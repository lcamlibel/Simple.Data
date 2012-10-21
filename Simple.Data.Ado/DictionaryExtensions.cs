using System.Collections;
using System.Collections.Generic;

namespace Simple.Data.Ado
{
    public static class DictionaryExtensions
    {
        public static object GetLockObject<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            var collection = dictionary as ICollection;
            if (collection != null)
            {
                return collection.SyncRoot;
            }
            return dictionary;
        }
    }
}