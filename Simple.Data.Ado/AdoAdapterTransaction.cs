using System.Data;

namespace Simple.Data.Ado
{
    internal class AdoAdapterTransaction : IAdapterTransaction
    {
        private readonly IDbConnection _dbConnection;
        private readonly IDbTransaction _dbTransaction;
        private readonly string _name;
        private readonly bool _sharedConnection;

        public AdoAdapterTransaction(IDbTransaction dbTransaction, bool sharedConnection = false)
            : this(dbTransaction, null, sharedConnection)
        {
        }

        public AdoAdapterTransaction(IDbTransaction dbTransaction, string name, bool sharedConnection = false)
        {
            _name = name;
            _dbTransaction = dbTransaction;
            _dbConnection = _dbTransaction.Connection;
            _sharedConnection = sharedConnection;
        }

        internal IDbTransaction DbTransaction
        {
            get { return _dbTransaction; }
        }

        #region IAdapterTransaction Members

        public void Dispose()
        {
            _dbTransaction.Dispose();
            if (!_sharedConnection)
                _dbConnection.Dispose();
        }

        public void Commit()
        {
            _dbTransaction.Commit();
        }

        public void Rollback()
        {
            _dbTransaction.Rollback();
        }

        public string Name
        {
            get { return _name; }
        }

        #endregion
    }
}