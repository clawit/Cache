namespace Cache.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Fody;
    using Mono.Cecil;
    using Mono.Cecil.Rocks;

    internal class ReferenceFinder
    {
        internal BaseModuleWeaver Weaver { get; set; }
        internal IAssemblyResolver AssemblyResolver { get; set; }

        //internal TypeDefinition CompilerGeneratedAttribute { get; set; }

        internal MethodDefinition DebugWriteLineMethod { get; set; }

        internal MethodReference DictionaryAddMethod { get; set; }

        internal MethodReference DictionaryConstructor { get; set; }

        //internal GenericInstanceType DictionaryGenericInstanceType { get; set; }

        //internal TypeDefinition DictionaryInterface { get; set; }

        //internal TypeDefinition DictionaryType { get; set; }

        internal ModuleDefinition ModuleDefinition { get; set; }

        internal MethodDefinition StringFormatMethod { get; set; }

        internal MethodDefinition SystemTypeGetTypeFromHandleMethod { get; set; }

        //internal TypeDefinition SystemTypeType { get; set; }

        internal void LoadReferences()
        {
            DebugWriteLineMethod = Weaver.FindType("System.String").Method("WriteLine");
            StringFormatMethod = Weaver.FindType("System.String").Method("Format");
            DictionaryConstructor = Weaver.FindType("IDictionary`2").Method(".ctor");
            DictionaryAddMethod = Weaver.FindType("IDictionary`2").Method("Add");
            SystemTypeGetTypeFromHandleMethod = Weaver.FindType("Type").Method("GetTypeFromHandle");




            //List<TypeDefinition> coreTypes = new List<TypeDefinition>();
            //AppendTypes("System.Runtime.Extensions", coreTypes);
            //AppendTypes("System", coreTypes);
            //AppendTypes("mscorlib", coreTypes);
            //AppendTypes("System.Runtime", coreTypes);
            //AppendTypes("System.Reflection", coreTypes);

            //TypeDefinition debugType = GetDebugType(coreTypes);

            //DebugWriteLineMethod =
            //    debugType.Methods.First(
            //        method =>
            //            method.Matches("WriteLine", ModuleDefinition.TypeSystem.Void, new[] { ModuleDefinition.TypeSystem.String }));

            //StringFormatMethod =
            //    ModuleDefinition.TypeSystem.String.Resolve()
            //        .Methods.First(
            //            method =>
            //                method.Matches("Format", ModuleDefinition.TypeSystem.String,
            //                    new[] { ModuleDefinition.TypeSystem.String, ModuleDefinition.TypeSystem.Object.MakeArrayType() }));

            //CompilerGeneratedAttribute = coreTypes.First(t => t.Name == "CompilerGeneratedAttribute");

            //DictionaryInterface = GetDictionaryInterface(coreTypes);

            //DictionaryType = GetDictionaryType(coreTypes);
            //DictionaryGenericInstanceType = DictionaryType.MakeGenericInstanceType(ModuleDefinition.TypeSystem.String,
            //    ModuleDefinition.TypeSystem.Object);

            //DictionaryConstructor =
            //    DictionaryGenericInstanceType.Resolve()
            //        .GetConstructors()
            //        .First(x => !x.Parameters.Any())
            //        .MakeHostInstanceGeneric(ModuleDefinition.TypeSystem.String, ModuleDefinition.TypeSystem.Object);

            //DictionaryAddMethod =
            //    DictionaryGenericInstanceType.Resolve()
            //        .Methods.First(
            //            method =>
            //                method.Matches("Add", ModuleDefinition.TypeSystem.Void,
            //                    new TypeReference[] { DictionaryType.GenericParameters[0], DictionaryType.GenericParameters[1] }))
            //        .MakeHostInstanceGeneric(ModuleDefinition.TypeSystem.String, ModuleDefinition.TypeSystem.Object);

            //SystemTypeType = GetSystemTypeType(coreTypes);

            //SystemTypeGetTypeFromHandleMethod =
            //    SystemTypeType.Resolve()
            //        .Methods.First(
            //            method =>
            //                method.Matches("GetTypeFromHandle", SystemTypeType,
            //                    new TypeReference[] { GetSystemRuntimeTypeHandleType(coreTypes) }));
        }

        internal void AppendTypes(string name, List<TypeDefinition> coreTypes)
        {
            AssemblyDefinition definition = AssemblyResolver.Resolve(AssemblyNameReference.Parse(name));
            if (definition != null)
            {
                coreTypes.AddRange(definition.MainModule.Types);
            }
        }

        //internal TypeDefinition GetDebugType(List<TypeDefinition> coreTypes)
        //{
        //    TypeDefinition debugType = coreTypes.FirstOrDefault(x => x.Name == "Debug");

        //    if (debugType != null)
        //    {
        //        return debugType;
        //    }

        //    AssemblyDefinition systemDiagnosticsDebug = AssemblyResolver.Resolve(AssemblyNameReference.Parse("System.Diagnostics.Debug"));

        //    if (systemDiagnosticsDebug != null)
        //    {
        //        debugType = systemDiagnosticsDebug.MainModule.Types.FirstOrDefault(x => x.Name == "Debug");

        //        if (debugType != null)
        //        {
        //            return debugType;
        //        }
        //    }

        //    throw new Exception("Could not find the 'Debug' type.");
        //}

        //internal TypeDefinition GetDictionaryInterface(List<TypeDefinition> coreTypes)
        //{
        //    TypeDefinition dictionaryType = coreTypes.FirstOrDefault(x => x.Name == "IDictionary`2");

        //    if (dictionaryType != null)
        //    {
        //        return dictionaryType;
        //    }

        //    throw new Exception("Could not find the 'IDictionary' interface.");
        //}

        //internal TypeDefinition GetDictionaryType(List<TypeDefinition> coreTypes)
        //{
        //    TypeDefinition dictionaryType = coreTypes.FirstOrDefault(x => x.Name == "Dictionary`2");

        //    if (dictionaryType != null)
        //    {
        //        return dictionaryType;
        //    }

        //    throw new Exception("Could not find the 'Dictionary' type.");
        //}

        //internal TypeDefinition GetSystemRuntimeTypeHandleType(List<TypeDefinition> coreTypes)
        //{
        //    TypeDefinition runtimeTypeHandle = coreTypes.FirstOrDefault(x => x.Name == "RuntimeTypeHandle");

        //    if (runtimeTypeHandle != null)
        //    {
        //        return runtimeTypeHandle;
        //    }

        //    throw new Exception("Could not find the 'RuntimeHandle' type.");
        //}

        //internal TypeDefinition GetSystemTypeType(List<TypeDefinition> coreTypes)
        //{
        //    TypeDefinition systemType = coreTypes.FirstOrDefault(x => x.Name == "Type");

        //    if (systemType != null)
        //    {
        //        return systemType;
        //    }

        //    throw new Exception("Could not find the 'SystemType' type.");
        //}
    }
}