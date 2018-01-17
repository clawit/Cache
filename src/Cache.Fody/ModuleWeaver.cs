using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using Fody;

namespace Cache.Fody
{
    public class ModuleWeaver : BaseModuleWeaver
    {
        internal ReferenceFinder _references;
        internal List<TypeDefinition> _types;

        public override void Execute()
        {
            //
            _types = ModuleDefinition.GetTypes().ToList();

            //check for attribute which class is an abstract one.
            AttributeChecker.CheckForBadAttributes(this, _types);

            //
            _references = new ReferenceFinder() { Weaver = this, AssemblyResolver = ModuleDefinition.AssemblyResolver, ModuleDefinition = ModuleDefinition };
            _references.LoadReferences();

            //
            var methods = this.GetWeaveMethods();

            //weaving
            WeaveHelper.Weave(this, methods.Methods);
            WeaveHelper.Weave(this, methods.Properties);

            //
            AttributeChecker.RemoveAttributes(this, _types);
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "netstandard";
            yield return "mscorlib";
        }

        public override bool ShouldCleanReference => true;

    }
}
