using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Simple.Data
{
    internal class MefHelper : Composer
    {
        public override T Compose<T>()
        {
            using (CompositionContainer container = CreateAppDomainContainer())
            {
                List<Lazy<T>> exports = container.GetExports<T>().ToList();
                if (exports.Count == 1)
                {
                    return exports.Single().Value;
                }
            }
            using (CompositionContainer container = CreateFolderContainer())
            {
                List<Lazy<T>> exports = container.GetExports<T>().ToList();
                if (exports.Count == 0) throw new SimpleDataException("No ADO Provider found.");
                if (exports.Count > 1)
                    throw new SimpleDataException(
                        "Multiple ADO Providers found; specify provider name or remove unwanted assemblies.");
                return exports.Single().Value;
            }
        }

        public override T Compose<T>(string contractName)
        {
            try
            {
                using (CompositionContainer container = CreateAppDomainContainer())
                {
                    List<Lazy<T>> exports = container.GetExports<T>(contractName).ToList();
                    if (exports.Count == 1)
                    {
                        return exports.Single().Value;
                    }
                }
                using (CompositionContainer container = CreateFolderContainer())
                {
                    List<Lazy<T>> exports = container.GetExports<T>(contractName).ToList();
                    if (exports.Count == 0)
                        throw new SimpleDataException(string.Format("No {0} Provider found.", contractName));
                    if (exports.Count > 1)
                        throw new SimpleDataException(
                            "Multiple ADO Providers found; specify provider name or remove unwanted assemblies.");
                    return exports.Single().Value;
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Trace.WriteLine(ex.Message);
                throw;
            }
        }

        public static T GetAdjacentComponent<T>(Type knownSiblingType)
        {
            using (var assemblyCatalog = new AssemblyCatalog(knownSiblingType.Assembly))
            {
                using (var container = new CompositionContainer(assemblyCatalog))
                {
                    return container.GetExportedValueOrDefault<T>();
                }
            }
        }

        private static CompositionContainer CreateFolderContainer()
        {
            string path = GetSimpleDataAssemblyPath();

            var assemblyCatalog = new AssemblyCatalog(ThisAssembly);
            var aggregateCatalog = new AggregateCatalog(assemblyCatalog);
            foreach (string file in Directory.GetFiles(path, "Simple.Data.*.dll"))
            {
                var catalog = new AssemblyCatalog(file);
                aggregateCatalog.Catalogs.Add(catalog);
            }
            return new CompositionContainer(aggregateCatalog);
        }

        private static CompositionContainer CreateAppDomainContainer()
        {
            var aggregateCatalog = new AggregateCatalog();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(IsSimpleDataAssembly))
            {
                aggregateCatalog.Catalogs.Add(new AssemblyCatalog(assembly));
            }
            return new CompositionContainer(aggregateCatalog);
        }

        private static bool IsSimpleDataAssembly(Assembly assembly)
        {
            return assembly.GetFullName().StartsWith("Simple.Data.", StringComparison.OrdinalIgnoreCase);
        }
    }
}