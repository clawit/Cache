using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cache.Fody
{
    public static class WeaveHelper
    {
        public const string CacheAttributeName = "CacheAttribute";
        public const string NoCacheAttributeName = "NoCacheAttribute";
        public const string CompilerGeneratedAttributeName = "CompilerGeneratedAttribute";

        private const string CacheGetterName = "Cache";
        private const string CacheTypeContainsMethodName = "Contains";
        private const string CacheTypeRemoveMethodName = "Remove";
        private const string CacheTypeRetrieveMethodName = "Retrieve";
        private const string CacheTypeStoreMethodName = "Store";

        public static MethodsForWeaving GetWeaveMethods(BaseModuleWeaver weaver)
        {
            
            weaver.LogInfo(string.Format("Searching for Methods and Properties in assembly ({0}).", weaver.ModuleDefinition.Name));

            MethodsForWeaving result = new MethodsForWeaving();

            foreach (TypeDefinition type in weaver.ModuleDefinition.Types)
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    if (ShouldWeaveMethod(method))
                    {
                        // Store Cache attribute, method attribute takes precedence over class attributes
                        CustomAttribute attribute =
                            method.CustomAttributes.SingleOrDefault(x => x.Constructor.DeclaringType.Name == CacheAttributeName) ??
                                method.DeclaringType.CustomAttributes.SingleOrDefault(
                                    x => x.Constructor.DeclaringType.Name == CacheAttributeName);

                        result.Add(method, attribute);
                    }

                    method.RemoveCacheAttribute(CacheAttributeName);
                    method.RemoveCacheAttribute(NoCacheAttributeName);
                }

                //TODO:for now we don't support property cache
                //foreach (PropertyDefinition property in type.Properties)
                //{
                //    if (ShouldWeaveProperty(property))
                //    {
                //        // Store Cache attribute, property attribute takes precedence over class attributes
                //        CustomAttribute attribute =
                //            property.CustomAttributes.SingleOrDefault(x => x.Constructor.DeclaringType.Name == CacheAttributeName) ??
                //                property.DeclaringType.CustomAttributes.SingleOrDefault(
                //                    x => x.Constructor.DeclaringType.Name == CacheAttributeName);

                //        result.Add(property, attribute);
                //    }

                //    property.RemoveCacheAttribute(CacheAttributeName);
                //    property.RemoveCacheAttribute(NoCacheAttributeName);
                //}

                type.RemoveCacheAttribute(CacheAttributeName);
                type.RemoveCacheAttribute(NoCacheAttributeName);
            }

            return result;
        }

        private static bool ShouldWeaveMethod(MethodDefinition method)
        {
            CustomAttribute classLevelCacheAttribute = method.DeclaringType.GetCacheAttribute(CacheAttributeName);

            bool hasClassLevelCache = classLevelCacheAttribute != null;
            bool hasMethodLevelCache = method.ContainsAttribute(CacheAttributeName);
            bool hasNoCacheAttribute = method.ContainsAttribute(NoCacheAttributeName);
            bool isSpecialName = method.IsSpecialName || method.IsGetter || method.IsSetter || method.IsConstructor;
            bool isCompilerGenerated = method.ContainsAttribute(CompilerGeneratedAttributeName);

            if (hasNoCacheAttribute || isSpecialName || isCompilerGenerated)
            {
                // Never weave property accessors, special methods and compiler generated methods
                return false;
            }

            if (hasMethodLevelCache)
            {
                // Always weave methods explicitly marked for cache
                return true;
            }

            if (hasClassLevelCache)
            {
                // Otherwise weave if marked at class level
                return true;
            }

            return false;
        }

        private static bool ShouldWeaveProperty(PropertyDefinition property)
        {
            CustomAttribute classLevelCacheAttribute = property.DeclaringType.GetCacheAttribute(CacheAttributeName);

            bool hasClassLevelCache = classLevelCacheAttribute != null;
            bool hasPropertyLevelCache = property.ContainsAttribute(CacheAttributeName);
            bool hasNoCacheAttribute = property.ContainsAttribute(NoCacheAttributeName);
            bool isCacheGetter = property.Name == CacheGetterName;
            bool hasGetAccessor = property.GetMethod != null;
            bool isAutoProperty = hasGetAccessor && property.GetMethod.ContainsAttribute(CompilerGeneratedAttributeName);

            if (hasNoCacheAttribute || isCacheGetter || isAutoProperty || !hasGetAccessor)
            {
                // Never weave Cache property, auto-properties, write-only properties and properties explicitly excluded
                return false;
            }

            if (hasPropertyLevelCache)
            {
                // Always weave properties explicitly marked for cache
                return true;
            }

            if (hasClassLevelCache)
            {
                // Otherwise weave if marked at class level
                return true;
            }

            return false;
        }

        public static void Weave(BaseModuleWeaver weaver, IEnumerable<Tuple<MethodDefinition, CustomAttribute>> methods )
        {
            foreach (Tuple<MethodDefinition, CustomAttribute> methodDefinition in methods)
            {
                WeaveMethod(weaver, methodDefinition.Item1, methodDefinition.Item2);
            }
        }

        public static void Weave(BaseModuleWeaver weaver, IEnumerable<Tuple<PropertyDefinition, CustomAttribute>> properties)
        {
            foreach (Tuple<PropertyDefinition, CustomAttribute> propertyTuple in properties)
            {
                PropertyDefinition property = propertyTuple.Item1;
                CustomAttribute attribute = propertyTuple.Item2;

                // Get-Only Property, weave like normal methods
                if (property.SetMethod == null)
                {
                    WeaveMethod(weaver, property.GetMethod, attribute);
                }
                else
                {
                    MethodDefinition propertyGet = GetCacheGetter(property.SetMethod);

                    if (!IsPropertySetterValidForWeaving(weaver, propertyGet, property.SetMethod))
                    {
                        continue;
                    }

                    weaver.LogInfo(string.Format("Weaving property {0}::{1}.", property.DeclaringType.Name, property.Name));

                    WeaveMethod(weaver, property.GetMethod, attribute, propertyGet);
                    WeavePropertySetter(weaver, property.SetMethod, propertyGet);
                }
            }
        }

        private static MethodDefinition GetCacheGetter(MethodDefinition methodDefinition)
        {
            MethodDefinition propertyGet = methodDefinition.DeclaringType.GetPropertyGet(CacheGetterName);

            propertyGet = propertyGet ??
                methodDefinition.DeclaringType.BaseType.Resolve().GetInheritedPropertyGet(CacheGetterName);

            return propertyGet;
        }

        private static bool IsPropertySetterValidForWeaving(BaseModuleWeaver weaver, MethodDefinition propertyGet, MethodDefinition methodDefinition)
        {
            if (!IsMethodValidForWeaving(weaver, propertyGet, methodDefinition))
            {
                return false;
            }

            if (CacheTypeGetRemoveMethod(propertyGet.ReturnType.Resolve(), CacheTypeRemoveMethodName) == null)
            {
                weaver.LogWarning(string.Format("Method {0} missing in {1}.", CacheTypeRemoveMethodName,
                    propertyGet.ReturnType.Resolve().FullName));

                weaver.LogWarning(
                    string.Format(
                        "ReturnType {0} of Getter {1} of Class {2} does not implement all methods. Skip weaving of method {3}.",
                        propertyGet.ReturnType.Name, CacheGetterName, methodDefinition.DeclaringType.Name, methodDefinition.Name));

                return false;
            }

            return true;
        }

        public static bool IsMethodValidForWeaving(BaseModuleWeaver weaver, MethodDefinition propertyGet, MethodDefinition methodDefinition)
        {
            //only non-static method need a getter
            if (propertyGet == null && !methodDefinition.IsStatic)
            {
                weaver.LogWarning(string.Format("Class {0} does not contain or inherit Getter {1}. Skip weaving of method {2}.",
                    methodDefinition.DeclaringType.Name, CacheGetterName, methodDefinition.Name));

                return false;
            }

            //not need anymore
            //if (methodDefinition.IsStatic && !propertyGet.IsStatic)
            //{
            //    weaver.LogWarning(string.Format("Method {2} of Class {0} is static, Getter {1} is not. Skip weaving of method {2}.",
            //        methodDefinition.DeclaringType.Name, CacheGetterName, methodDefinition.Name));

            //    return false;
            //}

            if (!methodDefinition.IsStatic && !CheckCacheTypeMethods(weaver, propertyGet.ReturnType.Resolve()))
            {
                weaver.LogWarning(
                    string.Format(
                        "ReturnType {0} of Getter {1} of Class {2} does not implement all methods. Skip weaving of method {3}.",
                        propertyGet.ReturnType.Name, CacheGetterName, methodDefinition.DeclaringType.Name, methodDefinition.Name));

                return false;
            }

            return true;
        }

        private static MethodDefinition CacheTypeGetRemoveMethod(TypeDefinition cacheType, string cacheTypeRemoveMethodName)
        {
            return cacheType.Method(cacheTypeRemoveMethodName);
        }

        private static bool CheckCacheTypeMethods(BaseModuleWeaver weaver, TypeDefinition cacheType)
        {
            weaver.LogInfo(string.Format("Checking CacheType methods ({0}, {1}, {2}).", CacheTypeContainsMethodName,
                CacheTypeStoreMethodName, CacheTypeRetrieveMethodName));

            if (CacheTypeGetContainsMethod(cacheType, CacheTypeContainsMethodName) == null)
            {
                weaver.LogWarning(string.Format("Method {0} missing in {1}.", CacheTypeContainsMethodName, cacheType.FullName));

                return false;
            }

            if (CacheTypeGetStoreMethod(cacheType, CacheTypeStoreMethodName) == null)
            {
                weaver.LogWarning(string.Format("Method {0} missing in {1}.", CacheTypeStoreMethodName, cacheType.FullName));

                return false;
            }

            if (CacheTypeGetRetrieveMethod(cacheType, CacheTypeRetrieveMethodName) == null)
            {
                weaver.LogWarning(string.Format("Method {0} missing in {1}.", CacheTypeRetrieveMethodName, cacheType.FullName));

                return false;
            }

            weaver.LogInfo(string.Format("CacheInterface methods found."));

            return true;
        }

        private static MethodDefinition CacheTypeGetContainsMethod(TypeDefinition cacheType, string cacheTypeContainsMethodName)
        {
            return cacheType.Method(cacheTypeContainsMethodName);
        }

        private static MethodDefinition CacheTypeGetStoreMethod(TypeDefinition cacheInterface, string cacheTypeStoreMethodName)
        {
            // Prioritize Store methods with parameters Dictionary
            MethodDefinition methodDefinition = cacheInterface.Method(cacheTypeStoreMethodName);

            if (methodDefinition != null)
            {
                return methodDefinition;
            }

            return cacheInterface.Method(cacheTypeStoreMethodName);
        }

        private static MethodDefinition CacheTypeGetRetrieveMethod(TypeDefinition cacheType, string cacheTypeRetrieveMethodName)
        {
            return cacheType.Method(cacheTypeRetrieveMethodName);
        }

        private static void WeavePropertySetter(BaseModuleWeaver weaver, MethodDefinition setter, MethodReference propertyGet)
        {
            setter.Body.InitLocals = true;
            setter.Body.SimplifyMacros();

            if (propertyGet.DeclaringType.HasGenericParameters)
            {
                propertyGet = propertyGet.MakeHostInstanceGeneric(propertyGet.DeclaringType.GenericParameters.Cast<TypeReference>().ToArray());
            }

            Instruction firstInstruction = setter.Body.Instructions.First();
            ILProcessor processor = setter.Body.GetILProcessor();

            // Add local variables
            int cacheKeyIndex = setter.AddVariable(weaver.ModuleDefinition.TypeSystem.String);

            // Generate CacheKeyTemplate
            string cacheKey = CreateCacheKeyString(setter);

            Instruction current = firstInstruction.Prepend(processor.Create(OpCodes.Ldstr, cacheKey), processor);

            // Create set cache key
            current = current.AppendStloc(processor, cacheKeyIndex);

            current = InjectCacheKeyCreatedCode(weaver, setter, current, processor, cacheKeyIndex);


            if (!propertyGet.Resolve().IsStatic)
            {
                current = current.AppendLdarg(processor, 0);
            }

            current
                .Append(processor.Create(OpCodes.Call, setter.Module.ImportReference(propertyGet)), processor)
                .AppendLdloc(processor, cacheKeyIndex)
                .Append(processor.Create(OpCodes.Callvirt, setter.Module.ImportReference(
                    CacheTypeGetRemoveMethod(propertyGet.ReturnType.Resolve(), CacheTypeRemoveMethodName))), processor);

            setter.Body.OptimizeMacros();
        }

        private static string CreateCacheKeyString(MethodDefinition methodDefinition)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(methodDefinition.DeclaringType.FullName);
            builder.Append(".");

            if (methodDefinition.IsSpecialName && (methodDefinition.IsSetter || methodDefinition.IsGetter))
            {
                builder.Append(Regex.Replace(methodDefinition.Name, "[gs]et_", string.Empty));
            }
            else
            {
                builder.Append(methodDefinition.Name);

                for (int i = 0; i < methodDefinition.Parameters.Count + methodDefinition.GenericParameters.Count; i++)
                {
                    builder.Append(string.Format("_{{{0}}}", i));
                }
            }

            return builder.ToString();
        }

        private static Instruction InjectCacheKeyCreatedCode(BaseModuleWeaver weaver, MethodDefinition methodDefinition, Instruction current, ILProcessor processor, int cacheKeyIndex)
        {
            // Call Debug.WriteLine with CacheKey
            current = current
                .AppendLdstr(processor, "CacheKey created: {0}")
                .AppendLdcI4(processor, 1)
                .Append(processor.Create(OpCodes.Newarr, weaver.ModuleDefinition.TypeSystem.Object), processor)
                .AppendDup(processor)
                .AppendLdcI4(processor, 0)
                .AppendLdloc(processor, cacheKeyIndex)
                .Append(processor.Create(OpCodes.Stelem_Ref), processor)
                .Append(processor.Create(OpCodes.Call, methodDefinition.Module.ImportMethod(ReferenceFinder.StringFormatMethod)),
                    processor)
                .Append(processor.Create(OpCodes.Call, methodDefinition.Module.ImportMethod(ReferenceFinder.DebugWriteLineMethod)),
                    processor);
            

            return current;
        }

        public static PropertyDefinition InjectProperty(BaseModuleWeaver weaver, MethodDefinition methodDefinition)
        {
            //Import Reference 
            TypeDefinition mdlClass = methodDefinition.DeclaringType;
            var refModule = ReferenceFinder.CacheAssembly.MainModule;
            var refInterface = refModule.GetType("Cache.ICacheProvider");

            //define the field we store the value in 
            FieldDefinition field = new FieldDefinition(
                "<Cache>k__BackingField", FieldAttributes.Private | FieldAttributes.Static, 
                mdlClass.Module.ImportReference(refInterface));
            mdlClass.Fields.Add(field);

            //Create the get method 
            MethodDefinition get = new MethodDefinition("get_Cache", 
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Static,
                mdlClass.Module.ImportReference(refInterface));
            ILProcessor processorGet = get.Body.GetILProcessor();
            get.Body.Instructions.Add(processorGet.Create(OpCodes.Ldsfld, field));
            get.Body.Instructions.Add(processorGet.Create(OpCodes.Ret));

            get.Body.InitLocals = true;
            get.SemanticsAttributes = MethodSemanticsAttributes.Getter;
            mdlClass.Methods.Add(get);

            //Create the set method 
            MethodDefinition set = new MethodDefinition("set_Cache",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Static,
                mdlClass.Module.TypeSystem.Void);
            ILProcessor processorSet = set.Body.GetILProcessor();
            set.Body.Instructions.Add(processorSet.Create(OpCodes.Ldarg_0));
            set.Body.Instructions.Add(processorSet.Create(OpCodes.Stsfld, field));
            set.Body.Instructions.Add(processorSet.Create(OpCodes.Ret));
            set.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, 
                mdlClass.Module.ImportReference(refInterface)));
            set.SemanticsAttributes = MethodSemanticsAttributes.Setter;
            mdlClass.Methods.Add(set);

            //create the property 
            PropertyDefinition propertyDefinition = new PropertyDefinition(
                "Cache", PropertyAttributes.None, 
                mdlClass.Module.ImportReference(refInterface))
            { GetMethod = get, SetMethod = set };

            //add the property to the type. 
            mdlClass.Properties.Add(propertyDefinition);

            return propertyDefinition;
        }

        public static void WeaveMethod(BaseModuleWeaver weaver, MethodDefinition methodDefinition, CustomAttribute attribute, MethodReference propertyGet)
        {
            //try to add propert 
            PropertyDefinition propertyInject = null;
            if (propertyGet == null && methodDefinition.IsStatic && !methodDefinition.DeclaringType.Properties.Any( p => p.Name == "Cache"))
            {
                propertyInject = InjectProperty(weaver, methodDefinition);
                propertyGet = propertyInject.GetMethod;
            }

            if (propertyInject == null )
            {
                propertyInject = methodDefinition.DeclaringType.Properties.FirstOrDefault(p => p.Name == "Cache");
            }

            //weave mthod 
            methodDefinition.Body.InitLocals = true;

            methodDefinition.Body.SimplifyMacros();

            if (propertyGet.DeclaringType.HasGenericParameters)
            {
                propertyGet = propertyGet.MakeHostInstanceGeneric(propertyGet.DeclaringType.GenericParameters.Cast<TypeReference>().ToArray());
            }

            Instruction firstInstruction = methodDefinition.Body.Instructions.First();

            ICollection<Instruction> returnInstructions =
                methodDefinition.Body.Instructions.ToList().Where(x => x.OpCode == OpCodes.Ret).ToList();

            if (returnInstructions.Count == 0)
            {
                weaver.LogWarning(string.Format("Method {0} does not contain any return statement. Skip weaving of method {0}.",
                    methodDefinition.Name));
                return;
            }

            // Add local variables
            int cacheKeyIndex = methodDefinition.AddVariable(weaver.ModuleDefinition.TypeSystem.String);
            int resultIndex = methodDefinition.AddVariable(methodDefinition.ReturnType);
            int objectArrayIndex = methodDefinition.AddVariable(weaver.ModuleDefinition.TypeSystem.Object.MakeArrayType());

            ILProcessor processor = methodDefinition.Body.GetILProcessor();

            // Generate CacheKeyTemplate
            string cacheKey = CreateCacheKeyString(methodDefinition);

            Instruction current = firstInstruction.Prepend(processor.Create(OpCodes.Ldstr, cacheKey), processor);

            current = SetCacheKeyLocalVariable(weaver, current, methodDefinition, processor, cacheKeyIndex, objectArrayIndex);

            //init CacheProvider when static method called
            if (methodDefinition.IsStatic)
            {
                var refModule = ReferenceFinder.CacheAssembly.MainModule;
                var refProvider = refModule.GetType("Cache.CacheProvider");
                var refProviderGet = refProvider.Method("get_Provider");
                var refSetter = propertyInject.SetMethod;
                current = current.Append(processor.Create(OpCodes.Call, methodDefinition.Module.ImportReference(refProviderGet)), processor);
                current = current.Append(processor.Create(OpCodes.Call, methodDefinition.Module.ImportReference(refSetter)), processor);
            }

            current = InjectCacheKeyCreatedCode(weaver, methodDefinition, current, processor, cacheKeyIndex);

            TypeDefinition propertyGetReturnTypeDefinition = propertyGet.ReturnType.Resolve();

            if (!propertyGet.Resolve().IsStatic)
            {
                current = current.AppendLdarg(processor, 0);
            }

            MethodReference methodReferenceContain =
                methodDefinition.Module.ImportMethod(CacheTypeGetContainsMethod(propertyGetReturnTypeDefinition,
                    CacheTypeContainsMethodName));

            current = current
                .Append(processor.Create(OpCodes.Call, methodDefinition.Module.ImportReference(propertyGet)), processor)
                .AppendLdloc(processor, cacheKeyIndex)
                .Append(processor.Create(OpCodes.Callvirt, methodReferenceContain), processor)
                .Append(processor.Create(OpCodes.Brfalse, firstInstruction), processor);

            // False branche (store value in cache of each return instruction)
            foreach (Instruction returnInstruction in returnInstructions)
            {
                returnInstruction.Previous.AppendStloc(processor, resultIndex);


                AppendDebugWrite(weaver, returnInstruction.Previous, processor, "Storing to cache.");
           

                if (!propertyGet.Resolve().IsStatic)
                {
                    returnInstruction.Previous.AppendLdarg(processor, 0);
                }

                MethodReference methodReferenceStore =
                    methodDefinition.Module.ImportMethod(CacheTypeGetStoreMethod(propertyGetReturnTypeDefinition, CacheTypeStoreMethodName));

                returnInstruction.Previous
                    .Append(processor.Create(OpCodes.Call, methodDefinition.Module.ImportReference(propertyGet)), processor)
                    .AppendLdloc(processor, cacheKeyIndex)
                    .AppendLdloc(processor, resultIndex)
                    .AppendBoxIfNecessary(processor, methodDefinition.ReturnType);

                // Pass parameters to Store method if supported
                if (methodReferenceStore.Parameters.Count == 3)
                {
                    returnInstruction.Previous
                        .Append(processor.Create(OpCodes.Newobj,
                            methodDefinition.Module.ImportReference(ReferenceFinder.DictionaryConstructor)), processor);

                    foreach (CustomAttributeNamedArgument property in attribute.Properties.Union(attribute.Fields))
                    {
                        returnInstruction.Previous
                            .AppendDup(processor)
                            .AppendLdstr(processor, property.Name)
                            .AppendLd(processor, property.Argument)
                            .AppendBoxIfNecessary(processor,
                                property.Argument.Type != weaver.ModuleDefinition.TypeSystem.Object
                                    ? property.Argument.Type : ((CustomAttributeArgument)property.Argument.Value).Type)
                            .Append(processor.Create(OpCodes.Callvirt, methodDefinition.Module.ImportReference(ReferenceFinder.DictionaryAddMethod)),
                                processor);
                    }
                }

                returnInstruction.Previous
                    .Append(processor.Create(OpCodes.Callvirt, methodReferenceStore), processor)
                    .AppendLdloc(processor, resultIndex);
            }


            current = AppendDebugWrite(weaver, current, processor, "Loading from cache.");


            if (!propertyGet.Resolve().IsStatic)
            {
                current = current.AppendLdarg(processor, 0);
            }

            // Start of branche true
            MethodReference methodReferenceRetrieve =
                methodDefinition.Module.ImportMethod(CacheTypeGetRetrieveMethod(propertyGetReturnTypeDefinition,
                    CacheTypeRetrieveMethodName)).MakeGeneric(new[] { methodDefinition.ReturnType });

            current.Append(processor.Create(OpCodes.Call, methodDefinition.Module.ImportReference(propertyGet)), processor)
                .AppendLdloc(processor, cacheKeyIndex)
                .Append(processor.Create(OpCodes.Callvirt, methodReferenceRetrieve), processor)
                .AppendStloc(processor, resultIndex)
                .Append(processor.Create(OpCodes.Br, returnInstructions.Last().Previous), processor);

            methodDefinition.Body.OptimizeMacros();
        }

        public static void WeaveMethod(BaseModuleWeaver weaver, MethodDefinition methodDefinition, CustomAttribute attribute)
        {
            MethodDefinition propertyGet = GetCacheGetter(methodDefinition);

            if (!IsMethodValidForWeaving(weaver, propertyGet, methodDefinition))
            {
                return;
            }

            if (methodDefinition.ReturnType == methodDefinition.Module.TypeSystem.Void)
            {
                weaver.LogWarning(string.Format("Method {0} returns void. Skip weaving of method {0}.", methodDefinition.Name));

                return;
            }

            weaver.LogInfo(string.Format("Weaving method {0}::{1}.", methodDefinition.DeclaringType.Name, methodDefinition.Name));

            WeaveMethod(weaver, methodDefinition, attribute, propertyGet);
        }
        private static Instruction AppendDebugWrite(BaseModuleWeaver weaver, Instruction instruction, ILProcessor processor, string message)
        {
            return instruction
                .AppendLdstr(processor, message)
                .Append(processor.Create(OpCodes.Call, weaver.ModuleDefinition.ImportMethod(ReferenceFinder.DebugWriteLineMethod)), processor);
        }

        private static Instruction SetCacheKeyLocalVariable(BaseModuleWeaver weaver, Instruction current, MethodDefinition methodDefinition,
            ILProcessor processor, int cacheKeyIndex, int objectArrayIndex)
        {
            if (methodDefinition.IsSetter || methodDefinition.IsGetter)
            {
                return current.AppendStloc(processor, cacheKeyIndex);
            }
            else
            {
                // Create object[] for string.format
                int parameterCount = methodDefinition.Parameters.Count + methodDefinition.GenericParameters.Count;

                current = current
                    .AppendLdcI4(processor, parameterCount)
                    .Append(processor.Create(OpCodes.Newarr, weaver.ModuleDefinition.TypeSystem.Object), processor)
                    .AppendStloc(processor, objectArrayIndex);


                // Set object[] values
                for (int i = 0; i < methodDefinition.GenericParameters.Count; i++)
                {
                    current = current
                        .AppendLdloc(processor, objectArrayIndex)
                        .AppendLdcI4(processor, i)
                        .Append(processor.Create(OpCodes.Ldtoken, methodDefinition.GenericParameters[i]), processor)
                        .Append(processor.Create(OpCodes.Call, methodDefinition.Module.ImportMethod(ReferenceFinder.SystemTypeGetTypeFromHandleMethod)),
                        processor)
                        .Append(processor.Create(OpCodes.Stelem_Ref), processor);
                }

                for (int i = 0; i < methodDefinition.Parameters.Count; i++)
                {
                    current = current
                        .AppendLdloc(processor, objectArrayIndex)
                        .AppendLdcI4(processor, methodDefinition.GenericParameters.Count + i)
                        .AppendLdarg(processor, !methodDefinition.IsStatic ? i + 1 : i)
                        .AppendBoxIfNecessary(processor, methodDefinition.Parameters[i].ParameterType)
                        .Append(processor.Create(OpCodes.Stelem_Ref), processor);
                }

                // Call string.format



                var ins = processor.Create(OpCodes.Call, methodDefinition.Module.ImportMethod(ReferenceFinder.StringFormatMethod));

                return current
                    .AppendLdloc(processor, objectArrayIndex)
                    .Append(ins, processor)
                    .AppendStloc(processor, cacheKeyIndex);
            }
        }

    }
}
