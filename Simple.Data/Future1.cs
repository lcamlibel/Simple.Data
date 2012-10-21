using System;
using System.Threading;

namespace Simple.Data
{
    public class Future<T>
    {
        private bool _hasValue;
        private T _value;

        private Future()
        {
        }

        public T Value
        {
            get
            {
                SpinWait.SpinUntil(() => _hasValue);
                return _value;
            }
        }

        public bool HasValue
        {
            get { return _hasValue; }
        }

        private void Set(T value)
        {
            _value = value;
            _hasValue = true;
        }

        public static Future<T> Create(out Action<T> setAction)
        {
            var future = new Future<T>();
            setAction = future.Set;
            return future;
        }

        public static implicit operator T(Future<T> future)
        {
            return future.Value;
        }
    }
}