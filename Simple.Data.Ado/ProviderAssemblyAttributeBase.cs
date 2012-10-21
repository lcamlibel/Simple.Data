using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Simple.Data.Ado
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public abstract class ProviderAssemblyAttributeBase : Attribute
    {
        private readonly HashSet<string> _adoProviderNames;

        protected ProviderAssemblyAttributeBase(string providerName, params string[] additionalProviderNames)
        {
            _adoProviderNames = new HashSet<string>(additionalProviderNames, StringComparer.InvariantCultureIgnoreCase)
                                    {providerName};
        }

        private static Assembly CurrentDomainOnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.Load(args.Name);
        }

        public bool IsForProviderName(string adoProviderName)
        {
            return _adoProviderNames.Contains(adoProviderName);
        }

        public abstract bool TryGetProvider(string connectionString, out IConnectionProvider provider,
                                            out Exception exception);

        public static IEnumerable<ProviderAssemblyAttributeBase> Get(Assembly assembly)
        {
            if (assembly.ReflectionOnly)
            {
                foreach (AssemblyName referencedAssembly in assembly.GetReferencedAssemblies())
                {
                    if (
                        AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().All(
                            a => a.GetFullName() != referencedAssembly.FullName))
                    {
                        try
                        {
                            Assembly.ReflectionOnlyLoad(referencedAssembly.FullName);
                        }
                        catch (FileNotFoundException)
                        {
                            return Enumerable.Empty<ProviderAssemblyAttributeBase>();
                        }
                    }
                }
                bool hasAttribute = assembly.GetCustomAttributesData().Any(
                    cad => typeof (ProviderAssemblyAttributeBase).IsAssignableFrom(cad.Constructor.DeclaringType));
                if (hasAttribute)
                {
                    assembly = Assembly.Load(assembly.GetFullName());
                }
                else
                {
                    return Enumerable.Empty<ProviderAssemblyAttributeBase>();
                }
            }
            return assembly.GetCustomAttributes(typeof (ProviderAssemblyAttributeBase), false)
                .Cast<ProviderAssemblyAttributeBase>();
        }
    }
}