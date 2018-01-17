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

        internal ModuleDefinition ModuleDefinition { get; set; }

        internal MethodDefinition StringFormatMethod { get; set; }

        internal MethodDefinition SystemTypeGetTypeFromHandleMethod { get; set; }

        internal void LoadReferences()
        {
            DebugWriteLineMethod = Weaver.FindType("System.String").Method("WriteLine");
            StringFormatMethod = Weaver.FindType("System.String").Method("Format");
            DictionaryConstructor = Weaver.FindType("IDictionary`2").Method(".ctor");
            DictionaryAddMethod = Weaver.FindType("IDictionary`2").Method("Add");
            SystemTypeGetTypeFromHandleMethod = Weaver.FindType("Type").Method("GetTypeFromHandle");
        }

        internal void AppendTypes(string name, List<TypeDefinition> coreTypes)
        {
            AssemblyDefinition definition = AssemblyResolver.Resolve(AssemblyNameReference.Parse(name));
            if (definition != null)
            {
                coreTypes.AddRange(definition.MainModule.Types);
            }
        }

    }
}