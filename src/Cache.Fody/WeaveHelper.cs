using System;
using System.Linq;
using Fody;
using Mono.Cecil;

namespace Cache.Fody
{
    internal static class WeaveHelper
    {
        internal const string CacheAttributeName = "CacheAttribute";
        internal const string NoCacheAttributeName = "NoCacheAttribute";

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

                    method.RemoveAttribute(CacheAttributeName);
                    method.RemoveAttribute(NoCacheAttributeName);
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

                    property.RemoveAttribute(CacheAttributeName);
                    property.RemoveAttribute(NoCacheAttributeName);
                }

                type.RemoveAttribute(CacheAttributeName);
            }

            return result;
        }  

        internal static bool ShouldWeaveMethod(MethodDefinition method)
        {
            CustomAttribute classLevelCacheAttribute =
                method.DeclaringType.CustomAttributes.SingleOrDefault(x => x.Constructor.DeclaringType.Name == CacheAttributeName);

            bool hasClassLevelCache = classLevelCacheAttribute != null &&
                !CacheAttributeExcludesMethods(classLevelCacheAttribute);
            bool hasMethodLevelCache = method.ContainsAttribute(CacheAttributeName);
            bool hasNoCacheAttribute = method.ContainsAttribute(NoCacheAttributeName);
            bool isSpecialName = method.IsSpecialName || method.IsGetter || method.IsSetter || method.IsConstructor;
            bool isCompilerGenerated = method.ContainsAttribute(References.CompilerGeneratedAttribute);

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

            if (hasClassLevelCache && !CacheAttributeExcludesMethods(classLevelCacheAttribute))
            {
                // Otherwise weave if marked at class level
                return MethodCacheEnabledByDefault || CacheAttributeMembersExplicitly(classLevelCacheAttribute, Members.Methods);
            }

            return false;
        }

        internal static bool ShouldWeaveProperty(PropertyDefinition property)
        {
            CustomAttribute classLevelCacheAttribute =
                property.DeclaringType.CustomAttributes.SingleOrDefault(x => x.Constructor.DeclaringType.Name == CacheAttributeName);

            bool hasClassLevelCache = classLevelCacheAttribute != null;
            bool hasPropertyLevelCache = property.ContainsAttribute(CacheAttributeName);
            bool hasNoCacheAttribute = property.ContainsAttribute(NoCacheAttributeName);
            bool isCacheGetter = property.Name == CacheGetterName;
            bool hasGetAccessor = property.GetMethod != null;
            bool isAutoProperty = hasGetAccessor && property.GetMethod.ContainsAttribute(References.CompilerGeneratedAttribute);

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

            if (hasClassLevelCache && !CacheAttributeExcludesProperties(classLevelCacheAttribute))
            {
                // Otherwise weave if marked at class level
                return PropertyCacheEnabledByDefault ||
                    CacheAttributeMembersExplicitly(classLevelCacheAttribute, Members.Properties);
            }

            return false;
        }
    }
}
