using System;
using System.Data;

namespace Simple.Data.Ado
{
    internal class ConnectionScope : IDisposable
    {
        private readonly IDbConnection _connection;

        private readonly bool _dispose;

        private ConnectionScope(IDbConnection connection, bool dispose)
        {
            _connection = connection;
            _dispose = dispose;
        }

        public IDbConnection Connection
        {
            get { return _connection; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (!_dispose) return;
            _connection.Dispose();
        }

        #endregion

        public static ConnectionScope Create(IDbTransaction transaction, Func<IDbConnection> creator)
        {
            if (transaction != null)
            {
                return new ConnectionScope(transaction.Connection, false);
            }
            IDbConnection connection = creator();
            connection.OpenIfClosed();
            return new ConnectionScope(connection, true);
        }
    }
}