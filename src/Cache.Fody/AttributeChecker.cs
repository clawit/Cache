namespace Cache.Fody
{
    using global::Fody;
    using Mono.Cecil;
    using System.Collections.Generic;
    using System.Linq;

    public static class AttributeChecker
    {
        public static CustomAttribute GetCacheAttribute(this ICustomAttributeProvider definition, string name)
        {
            var customAttributes = definition.CustomAttributes;

            return customAttributes.FirstOrDefault(x => x.AttributeType.Name == name);
        }

        public static bool ContainsAttribute(this ICustomAttributeProvider definition, string name)
        {
            return GetCacheAttribute(definition, name) != null;
        }

        public static bool IsCompilerGenerated(this ICustomAttributeProvider definition)
        {
            var customAttributes = definition.CustomAttributes;

            return customAttributes.Any(x => x.AttributeType.Name == WeaveHelper.CompilerGeneratedAttributeName);
        }

        public static void RemoveCacheAttribute(this ICustomAttributeProvider definition, string name)
        {
            var customAttributes = definition.CustomAttributes;

            var cacheAttribute = customAttributes.FirstOrDefault(x => x.AttributeType.Name == name);

            if (cacheAttribute != null)
            {
                customAttributes.Remove(cacheAttribute);
            }
        }

        public static void CheckForBadAttributes(BaseModuleWeaver Weaver, List<TypeDefinition> types)
        {
            foreach (var typeDefinition in types)
            {
                foreach (var method in typeDefinition.AbstractMethods())
                {
                    if (method.ContainsAttribute(WeaveHelper.CacheAttributeName))
                    {
                        Weaver.LogError($"Method '{method.FullName}' is abstract but has a [CacheAttribute]. Remove this attribute.");
                    }
                }
            }
        }

        public static void RemoveAttributes(BaseModuleWeaver Weaver, List<TypeDefinition> types)
        {
            Weaver.ModuleDefinition.RemoveCacheAttribute(WeaveHelper.CacheAttributeName);
            Weaver.ModuleDefinition.RemoveCacheAttribute(WeaveHelper.NoCacheAttributeName);
            Weaver.ModuleDefinition.Assembly.RemoveCacheAttribute(WeaveHelper.CacheAttributeName);
            Weaver.ModuleDefinition.Assembly.RemoveCacheAttribute(WeaveHelper.NoCacheAttributeName);
            foreach (var typeDefinition in types)
            {
                typeDefinition.RemoveCacheAttribute(WeaveHelper.CacheAttributeName);
                typeDefinition.RemoveCacheAttribute(WeaveHelper.NoCacheAttributeName);
                foreach (var method in typeDefinition.Methods)
                {
                    method.RemoveCacheAttribute(WeaveHelper.CacheAttributeName);
                    method.RemoveCacheAttribute(WeaveHelper.NoCacheAttributeName);
                }
            }
        }
    }
}
