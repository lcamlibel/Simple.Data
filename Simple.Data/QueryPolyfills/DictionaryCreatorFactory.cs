﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Simple.Data.QueryPolyfills
{
    internal class DictionaryCreatorFactory
    {
        public static Func<int, IDictionary<string, object>> CreateFunc(IDictionary<string, object> source)
        {
            var dictionary = source as Dictionary<string, object>;
            if (dictionary != null) return cap => new Dictionary<string, object>(cap, dictionary.Comparer);
            var sortedDictionary = source as SortedDictionary<string, object>;
            if (sortedDictionary != null) return cap => new SortedDictionary<string, object>(sortedDictionary.Comparer);
            if (source is ConcurrentDictionary<string, object>)
                return cap => new ConcurrentDictionary<string, object>();

            Type type = source.GetType();
            return cap => (IDictionary<string, object>) Activator.CreateInstance(type);
        }
    }
}