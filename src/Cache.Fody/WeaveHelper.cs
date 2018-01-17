using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Cache.Fody
{
    internal static class WeaveHelper
    {
        internal const string CacheAttributeName = "CacheAttribute";
        internal const string NoCacheAttributeName = "NoCacheAttribute";
        internal const string CompilerGeneratedAttributeName = "CompilerGeneratedAttribute";

        internal const string CacheGetterName = "Cache";
        internal const string CacheTypeContainsMethodName = "Contains";
        internal const string CacheTypeRemoveMethodName = "Remove";
        internal const string CacheTypeRetrieveMethodName = "Retrieve";
        internal const string CacheTypeStoreMethodName = "Store";

        internal static MethodsForWeaving GetWeaveMethods(this BaseModuleWeaver weaver)
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

                foreach (PropertyDefinition property in type.Properties)
                {
                    if (ShouldWeaveProperty(property))
                    {
                        // Store Cache attribute, property attribute takes precedence over class attributes
                        CustomAttribute attribute =
                            property.CustomAttributes.SingleOrDefault(x => x.Constructor.DeclaringType.Name == CacheAttributeName) ??
                                property.DeclaringType.CustomAttributes.SingleOrDefault(
                                    x => x.Constructor.DeclaringType.Name == CacheAttributeName);

                        result.Add(property, attribute);
                    }

                    property.RemoveCacheAttribute(CacheAttributeName);
                    property.RemoveCacheAttribute(NoCacheAttributeName);
                }

                type.RemoveCacheAttribute(CacheAttributeName);
                type.RemoveCacheAttribute(NoCacheAttributeName);
            }

            return result;
        }

        internal static bool ShouldWeaveMethod(MethodDefinition method)
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

        internal static bool ShouldWeaveProperty(PropertyDefinition property)
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

        internal static void Weave(BaseModuleWeaver weaver, IEnumerable<Tuple<MethodDefinition, CustomAttribute>> methods )
        {
            foreach (Tuple<MethodDefinition, CustomAttribute> methodDefinition in methods)
            {
                WeaveMethod(methodDefinition.Item1, methodDefinition.Item2);
            }
        }

        internal static void Weave(BaseModuleWeaver weaver, IEnumerable<Tuple<PropertyDefinition, CustomAttribute>> properties)
        {
            foreach (Tuple<PropertyDefinition, CustomAttribute> propertyTuple in properties)
            {
                PropertyDefinition property = propertyTuple.Item1;
                CustomAttribute attribute = propertyTuple.Item2;

                // Get-Only Property, weave like normal methods
                if (property.SetMethod == null)
                {
                    WeaveMethod(property.GetMethod, attribute);
                }
                else
                {
                    MethodDefinition propertyGet = GetCacheGetter(property.SetMethod);

                    if (!IsPropertySetterValidForWeaving(weaver, propertyGet, property.SetMethod))
                    {
                        continue;
                    }

                    weaver.LogInfo(string.Format("Weaving property {0}::{1}.", property.DeclaringType.Name, property.Name));

                    WeaveMethod(property.GetMethod, attribute, propertyGet);
                    WeavePropertySetter(property.SetMethod, propertyGet);
                }
            }
        }

        internal static MethodDefinition GetCacheGetter(MethodDefinition methodDefinition)
        {
            MethodDefinition propertyGet = methodDefinition.DeclaringType.GetPropertyGet(CacheGetterName);

            propertyGet = propertyGet ??
                methodDefinition.DeclaringType.BaseType.Resolve().GetInheritedPropertyGet(CacheGetterName);

            return propertyGet;
        }

        internal static bool IsPropertySetterValidForWeaving(BaseModuleWeaver weaver, MethodDefinition propertyGet, MethodDefinition methodDefinition)
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

        internal static bool IsMethodValidForWeaving(BaseModuleWeaver weaver, MethodDefinition propertyGet, MethodDefinition methodDefinition)
        {
            if (propertyGet == null)
            {
                weaver.LogWarning(string.Format("Class {0} does not contain or inherit Getter {1}. Skip weaving of method {2}.",
                    methodDefinition.DeclaringType.Name, CacheGetterName, methodDefinition.Name));

                return false;
            }

            if (methodDefinition.IsStatic && !propertyGet.IsStatic)
            {
                weaver.LogWarning(string.Format("Method {2} of Class {0} is static, Getter {1} is not. Skip weaving of method {2}.",
                    methodDefinition.DeclaringType.Name, CacheGetterName, methodDefinition.Name));

                return false;
            }

            if (!CheckCacheTypeMethods(weaver, propertyGet.ReturnType.Resolve()))
            {
                weaver.LogWarning(
                    string.Format(
                        "ReturnType {0} of Getter {1} of Class {2} does not implement all methods. Skip weaving of method {3}.",
                        propertyGet.ReturnType.Name, CacheGetterName, methodDefinition.DeclaringType.Name, methodDefinition.Name));

                return false;
            }

            return true;
        }

        internal static MethodDefinition CacheTypeGetRemoveMethod(TypeDefinition cacheType, string cacheTypeRemoveMethodName)
        {
            return cacheType.Method(cacheTypeRemoveMethodName);
        }

        internal static bool CheckCacheTypeMethods(BaseModuleWeaver weaver, TypeDefinition cacheType)
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

        internal static MethodDefinition CacheTypeGetContainsMethod(TypeDefinition cacheType, string cacheTypeContainsMethodName)
        {
            return cacheType.Method(cacheTypeContainsMethodName);
        }

        internal static MethodDefinition CacheTypeGetStoreMethod(TypeDefinition cacheInterface, string cacheTypeStoreMethodName)
        {
            // Prioritize Store methods with parameters Dictionary
            MethodDefinition methodDefinition = cacheInterface.Method(cacheTypeStoreMethodName);

            if (methodDefinition != null)
            {
                return methodDefinition;
            }

            return cacheInterface.Method(cacheTypeStoreMethodName);
        }

        internal static MethodDefinition CacheTypeGetRetrieveMethod(TypeDefinition cacheType, string cacheTypeRetrieveMethodName)
        {
            return cacheType.Method(cacheTypeRetrieveMethodName);
        }

        internal static void WeavePropertySetter(BaseModuleWeaver weaver, MethodDefinition setter, MethodReference propertyGet)
        {
            setter.Body.InitLocals = true;
            setter.Body.SimplifyMacros();

            //TODO:Add generic parameters support
            //if (propertyGet.DeclaringType.HasGenericParameters)
            //{
            //    propertyGet = propertyGet.MakeHostInstanceGeneric(propertyGet.DeclaringType.GenericParameters.Cast<TypeReference>().ToArray());
            //}

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
                .Append(processor.Create(OpCodes.Call, methodDefinition.Module.ImportMethod(References.StringFormatMethod)),
                    processor)
                .Append(processor.Create(OpCodes.Call, methodDefinition.Module.ImportMethod(References.DebugWriteLineMethod)),
                    processor);
            

            return current;
        }
    }
}
