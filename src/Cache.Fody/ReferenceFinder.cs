namespace Cache.Fody
{
    using global::Fody;
    using Mono.Cecil;
    using System.Collections.Generic;

    public static class ReferenceFinder
    {
        //private static IAssemblyResolver AssemblyResolver { get; set; }

        //private static TypeDefinition CompilerGeneratedAttribute { get; set; }

        private static BaseModuleWeaver _weaver;
        public static BaseModuleWeaver Weaver { get { return _weaver; } }

        public static MethodDefinition DebugWriteLineMethod { get; set; }

        public static MethodReference DictionaryAddMethod { get; set; }

        public static MethodReference DictionaryConstructor { get; set; }

        public static MethodDefinition StringFormatMethod { get; set; }

        public static MethodDefinition SystemTypeGetTypeFromHandleMethod { get; set; }

        public static void LoadReferences(BaseModuleWeaver weaver)
        {
            _weaver = weaver;

            DebugWriteLineMethod = weaver.FindType("System.String").Method("WriteLine");
            StringFormatMethod = weaver.FindType("System.String").Method("Format");
            DictionaryConstructor = weaver.FindType("IDictionary`2").Method(".ctor");
            DictionaryAddMethod = weaver.FindType("IDictionary`2").Method("Add");
            SystemTypeGetTypeFromHandleMethod = weaver.FindType("Type").Method("GetTypeFromHandle");
        }

        //private static void AppendTypes(string name, List<TypeDefinition> coreTypes)
        //{
        //    AssemblyDefinition definition = AssemblyResolver.Resolve(AssemblyNameReference.Parse(name));
        //    if (definition != null)
        //    {
        //        coreTypes.AddRange(definition.MainModule.Types);
        //    }
        //}

    }
}