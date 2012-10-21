using System;
using System.Collections.Generic;
using System.Linq;
using Simple.Data.Extensions;

namespace Simple.Data
{
    internal class AdapterFactory : IAdapterFactory
    {
        private readonly Composer _composer;

        protected AdapterFactory() : this(Composer.Default)
        {
        }

        protected AdapterFactory(Composer composer)
        {
            _composer = composer;
        }

        #region IAdapterFactory Members

        public Adapter Create(object settings)
        {
            return Create(settings.ObjectToDictionary());
        }

        public Adapter Create(string adapterName, object settings)
        {
            return Create(adapterName, settings.ObjectToDictionary());
        }

        public Adapter Create(IEnumerable<KeyValuePair<string, object>> settings)
        {
            var keyValuePairs = settings as KeyValuePair<string, object>[] ?? settings.ToArray();
            if (keyValuePairs.Any(kvp => kvp.Key.Equals("ConnectionString", StringComparison.OrdinalIgnoreCase)))
            {
                return Create("Ado", keyValuePairs);
            }

            throw new ArgumentException("Cannot infer adapter type from settings.");
        }

        public virtual Adapter Create(string adapterName, IEnumerable<KeyValuePair<string, object>> settings)
        {
            return DoCreate(adapterName, settings);
        }

        #endregion

        protected Adapter DoCreate(string adapterName, IEnumerable<KeyValuePair<string, object>> settings)
        {
            var adapter = _composer.Compose<Adapter>(adapterName);
            adapter.Setup(settings);
            return adapter;
        }
    }
}