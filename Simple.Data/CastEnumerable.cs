using System.Collections;
using System.Collections.Generic;

namespace Simple.Data
{
    internal class CastEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<dynamic> _source;

        public CastEnumerable(IEnumerable<dynamic> source)
        {
            _source = source;
        }

        #region Nested type: CastEnumerator

        private class CastEnumerator : IEnumerator<T>
        {
            private readonly IEnumerator<dynamic> _source;

            public CastEnumerator(IEnumerator<dynamic> source)
            {
                _source = source;
            }

            #region IEnumerator<T> Members

            public T Current
            {
                get { return _source.Current; }
            }

            public bool MoveNext()
            {
                return _source.MoveNext();
            }

            public void Reset()
            {
                _source.Reset();
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
                _source.Dispose();
            }

            #endregion
        }

        #endregion

        #region IEnumerable[T] implementation

        public IEnumerator<T> GetEnumerator()
        {
            return new CastEnumerator(_source.GetEnumerator());
        }

        #endregion

        #region IEnumerable implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}