using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace Simple.Data.Ado
{
    internal class DataReaderEnumerable : IEnumerable<IDictionary<string, object>>
    {
        private readonly IDbCommand _command;
        private readonly Func<IDbConnection> _createConnection;
        private readonly IDbTransaction _transaction;
        private IEnumerable<IDictionary<string, object>> _cache;
        private IDictionary<string, int> _index;

        public DataReaderEnumerable(IDbCommand command, Func<IDbConnection> createConnection)
            : this(command, createConnection, null)
        {
        }

        public DataReaderEnumerable(IDbCommand command, Func<IDbConnection> createConnection,
                                    IDictionary<string, int> index)
        {
            _command = command;
            _createConnection = createConnection;
            _index = index;
        }

        public DataReaderEnumerable(IDbCommand command, IDbTransaction transaction, IDictionary<string, int> index)
            : this(command, () => transaction.Connection, index)
        {
            _transaction = transaction;
        }

        #region IEnumerable<IDictionary<string,object>> Members

        public IEnumerator<IDictionary<string, object>> GetEnumerator()
        {
            if (_cache != null) return _cache.GetEnumerator();

            IDbCommand command;

            var clonable = _command as ICloneable;
            if (clonable != null)
            {
                command = (IDbCommand) clonable.Clone();
                command.Connection = _createConnection();
                if (_transaction != null)
                {
                    command.Transaction = _transaction;
                }
            }
            else
            {
                command = _command;
            }

            return new DataReaderEnumerator(command, _index, Cache, CacheIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private void Cache(IEnumerable<IDictionary<string, object>> cache)
        {
            Interlocked.CompareExchange(ref _cache, cache, null);
        }

        private void CacheIndex(IDictionary<string, int> index)
        {
            Interlocked.CompareExchange(ref _index, index, null);
        }

        #region Nested type: DataReaderEnumerator

        private class DataReaderEnumerator : IEnumerator<IDictionary<string, object>>
        {
            private readonly Action<IEnumerable<IDictionary<string, object>>> _cacheAction;
            private readonly Action<IDictionary<string, int>> _cacheIndexAction;
            private readonly IDbCommand _command;
            private readonly IDisposable _connectionDisposable;
            private IList<IDictionary<string, object>> _cache = new List<IDictionary<string, object>>();
            private IDictionary<string, object> _current;
            private IDictionary<string, int> _index;
            private IDataReader _reader;

            public DataReaderEnumerator(IDbCommand command, IDictionary<string, int> index,
                                        Action<IEnumerable<IDictionary<string, object>>> cacheAction,
                                        Action<IDictionary<string, int>> cacheIndexAction)
            {
                _command = command;
                _cacheAction = cacheAction;
                _cacheIndexAction = cacheIndexAction;
                _connectionDisposable = _command.Connection.MaybeDisposable();
                _index = index;
            }

            #region IEnumerator<IDictionary<string,object>> Members

            public void Dispose()
            {
                using (_connectionDisposable)
                using (_command)
                using (_reader)
                {
                    /* NO-OP */
                }
            }

            public bool MoveNext()
            {
                if (_reader == null)
                {
                    ExecuteReader();
                    if (_reader == null) return false;
                }

                return _reader.Read() ? SetCurrent() : EndRead();
            }

            public void Reset()
            {
                if (_reader != null) _reader.Dispose();
                if (_cache != null)
                    _cache.Clear();
                ExecuteReader();
            }

            public IDictionary<string, object> Current
            {
                get
                {
                    if (_current == null) throw new InvalidOperationException();
                    return _current;
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            #endregion

            private bool SetCurrent()
            {
                _current = _reader.ToDictionary(_index);

                // We don't want to cache more than 100 rows, too much memory would be used.
                if (_cache != null && _cache.Count < 100)
                {
                    _cache.Add(_current);
                }
                else
                {
                    _cache = null;
                }

                return true;
            }

            private bool EndRead()
            {
                _current = null;

                // When reader is done, cache the results to the DataReaderEnumerable.
                if (_cache != null)
                {
                    _cacheAction(_cache);
                }

                return false;
            }

            private void ExecuteReader()
            {
                _command.Connection.OpenIfClosed();
                _reader = _command.TryExecuteReader();
                CreateIndexIfNecessary();
            }

            private void CreateIndexIfNecessary()
            {
                if (_reader != null && _index == null)
                {
                    _index = _reader.CreateDictionaryIndex();
                    _cacheIndexAction(_index);
                }
            }
        }

        #endregion
    }
}