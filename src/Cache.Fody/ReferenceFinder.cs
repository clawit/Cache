namespace Cache.Fody
{
    using global::Fody;
    using Mono.Cecil;
    using Mono.Cecil.Rocks;
    using System.Collections.Generic;
    using System.Linq;

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

            DebugWriteLineMethod = weaver.FindType("System.Diagnostics.Debug").Method("WriteLine");
            StringFormatMethod = weaver.FindType("System.String").Method("Format", new string[] { "String", "Object[]" });
            DictionaryConstructor = weaver.FindType("Dictionary`2").Resolve().GetConstructors().FirstOrDefault();
            DictionaryAddMethod = weaver.FindType("Dictionary`2").Method("Add");
            SystemTypeGetTypeFromHandleMethod = weaver.FindType("Type").Method("GetTypeFromHandle");
        }

    }
}