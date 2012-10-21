﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Simple.Data
{
    internal class CachingAdapterFactory : AdapterFactory
    {
        private readonly ConcurrentDictionary<string, Adapter> _cache = new ConcurrentDictionary<string, Adapter>();

        public CachingAdapterFactory()
        {
        }

        public CachingAdapterFactory(Composer composer) : base(composer)
        {
        }

        public override Adapter Create(string adapterName, IEnumerable<KeyValuePair<string, object>> settings)
        {
            List<KeyValuePair<string, object>> mat;
            string hash;
            if (settings == null)
            {
                mat = new List<KeyValuePair<string, object>>();
                hash = adapterName;
            }
            else
            {
                mat = settings.ToList();
                hash = HashSettings(adapterName, mat);
            }
            //http://www.codethinked.com/blockingcollection-and-iproducerconsumercollection
            Adapter adapter = _cache.GetOrAdd(hash, _ => DoCreate(adapterName, mat));
            var cloneable = adapter as ICloneable;
            if (cloneable != null) return (Adapter) cloneable.Clone();
            return adapter;
        }

        private static string HashSettings(string adapterName, IEnumerable<KeyValuePair<string, object>> settings)
        {
            return adapterName +
                   string.Join("#", settings.Select(kvp => kvp.Key + "=" + kvp.Value));
        }

        public void Reset()
        {
            _cache.Clear();
        }
    }
}