using System.Collections.Generic;
using Simple.Data.Extensions;

namespace Simple.Data
{
    public class AdoCompatibleComparer : IEqualityComparer<string>
    {
        public static readonly HomogenizedEqualityComparer DefaultInstance = new HomogenizedEqualityComparer();

        #region IEqualityComparer<string> Members

        public bool Equals(string x, string y)
        {
            return ReferenceEquals(x, y.Homogenize())
                   || x.Homogenize() == y.Homogenize()
                   || x.Homogenize().Singularize() == y.Homogenize().Singularize();
        }

        public int GetHashCode(string obj)
        {
            return obj.Homogenize().Singularize().GetHashCode();
        }

        #endregion
    }
}