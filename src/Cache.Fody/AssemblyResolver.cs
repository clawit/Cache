﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cache.Fody
{
    public class AssemblyResolver : IAssemblyResolver
    {
        Dictionary<string, string> referenceDictionary;
        List<string> splitReferences;
        Dictionary<string, AssemblyDefinition> assemblyDefinitionCache = new Dictionary<string, AssemblyDefinition>(StringComparer.InvariantCultureIgnoreCase);

        public AssemblyResolver()
        {
        }

        public AssemblyResolver(List<string> splitReferences)
        {
            referenceDictionary = new Dictionary<string, string>();
            this.splitReferences = splitReferences;

            foreach (var filePath in splitReferences)
            {
                referenceDictionary[Path.GetFileNameWithoutExtension(filePath)] = filePath;
            }
        }

        AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
        {
            if (assemblyDefinitionCache.TryGetValue(file, out var assembly))
            {
                return assembly;
            }
            if (parameters.AssemblyResolver == null)
            {
                parameters.AssemblyResolver = this;
            }
            try
            {
                return assemblyDefinitionCache[file] = AssemblyDefinition.ReadAssembly(file, parameters);
            }
            catch (Exception exception)
            {
                throw new Exception($"Could not read '{file}'.", exception);
            }
        }

        public AssemblyDefinition Resolve(string assemblyName)
        {
            return Resolve(new AssemblyNameReference(assemblyName, null));
        }

        public AssemblyDefinition Resolve(AssemblyNameReference assemblyNameReference)
        {
            return Resolve(assemblyNameReference, new ReaderParameters());
        }

        public AssemblyDefinition Resolve(AssemblyNameReference assemblyNameReference, ReaderParameters parameters)
        {
            if (parameters == null)
            {
                parameters = new ReaderParameters();
            }

            if (referenceDictionary.TryGetValue(assemblyNameReference.Name, out var fileFromDerivedReferences))
            {
                return GetAssembly(fileFromDerivedReferences, parameters);
            }

            var joinedReferences = string.Join(Environment.NewLine, splitReferences.OrderBy(x => x));
            return null;
        }

        public virtual void Dispose()
        {
            foreach (var value in assemblyDefinitionCache.Values)
            {
                value?.Dispose();
            }
        }

    }
}
