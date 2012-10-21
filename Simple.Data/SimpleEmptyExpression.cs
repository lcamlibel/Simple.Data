using System;

namespace Simple.Data
{
    public class SimpleEmptyExpression : SimpleExpression, IEquatable<SimpleEmptyExpression>
    {
        public SimpleEmptyExpression() : base(null, null, SimpleExpressionType.Empty)
        {
        }

        #region IEquatable<SimpleEmptyExpression> Members

        public bool Equals(SimpleEmptyExpression other)
        {
            return !ReferenceEquals(null, other);
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (SimpleEmptyExpression)) return false;
            return Equals((SimpleEmptyExpression) obj);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(SimpleEmptyExpression left, SimpleEmptyExpression right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SimpleEmptyExpression left, SimpleEmptyExpression right)
        {
            return !Equals(left, right);
        }
    }
}