using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Simple.Data.Ado
{
    internal class DataReaderMultipleEnumerator : IEnumerator<IEnumerable<IDictionary<string, object>>>
    {
        private readonly IDbCommand _command;
        private readonly IDbConnection _connection;
        private readonly IDisposable _connectionDisposable;
        private IDictionary<string, int> _index;
        private bool _lastRead;
        private IDataReader _reader;

        public DataReaderMultipleEnumerator(IDbCommand command, IDbConnection connection)
            : this(command, connection, null)
        {
        }

        public DataReaderMultipleEnumerator(IDbCommand command, IDbConnection connection, IDictionary<string, int> index)
        {
            _command = command;
            _connection = connection;
            _connectionDisposable = _connection.MaybeDisposable();
            _index = index;
        }

        #region IEnumerator<IEnumerable<IDictionary<string,object>>> Members

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
                _lastRead = true;
                return true;
            }
            _lastRead = _reader.NextResult();
            return _lastRead;
        }

        public void Reset()
        {
            if (_reader != null) _reader.Dispose();
            ExecuteReader();
        }

        public IEnumerable<IDictionary<string, object>> Current
        {
            get
            {
                if (!_lastRead) throw new InvalidOperationException();
                Dictionary<string, int> index = _reader.CreateDictionaryIndex();
                while (_reader.Read())
                {
                    yield return _reader.ToDictionary(index);
                }
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        #endregion

        private void ExecuteReader()
        {
            _connection.OpenIfClosed();
            _reader = _command.TryExecuteReader();
            _index = _index ?? _reader.CreateDictionaryIndex();
        }
    }
}