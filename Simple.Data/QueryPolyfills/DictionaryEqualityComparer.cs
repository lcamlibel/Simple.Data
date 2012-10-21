using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple.Data.QueryPolyfills
{
    public class DictionaryEqualityComparer : IEqualityComparer<IDictionary<string, object>>
    {
        #region IEqualityComparer<IDictionary<string,object>> Members

        public bool Equals(IDictionary<string, object> x, IDictionary<string, object> y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
            if (x.Count != y.Count) return false;
            object yvalue;
            return x.Keys.All(key => y.TryGetValue(key, out yvalue) && Equals(x[key], yvalue));
        }

        public int GetHashCode(IDictionary<string, object> obj)
        {
            return obj.Aggregate(0,
                                 (acc, kvp) =>
                                 (((acc*397) ^ kvp.Key.GetHashCode())*397) ^ (kvp.Value ?? DBNull.Value).GetHashCode());
        }

        #endregion

        public override int GetHashCode()
        {
            return 0;
        }
    }
}