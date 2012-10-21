using System.Collections.Concurrent;
using System.Collections.Generic;
using ResultSet =
    System.Collections.Generic.IEnumerable
        <System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>>;

namespace Simple.Data.Ado
{
    public partial class AdoAdapter : IAdapterWithFunctions
    {
        private readonly ConcurrentDictionary<string, IProcedureExecutor> _executors =
            new ConcurrentDictionary<string, IProcedureExecutor>();

        #region IAdapterWithFunctions Members

        public bool IsValidFunction(string functionName)
        {
            return _connectionProvider.SupportsStoredProcedures && _schema.IsProcedure(functionName);
        }

        public IEnumerable<ResultSet> Execute(string functionName, IDictionary<string, object> parameters)
        {
            IProcedureExecutor executor = _executors.GetOrAdd(functionName,
                                                              f =>
                                                              _connectionProvider.GetProcedureExecutor(this,
                                                                                                       _schema.
                                                                                                           BuildObjectName
                                                                                                           (f)));
            return executor.Execute(parameters);
        }

        public IEnumerable<ResultSet> Execute(string functionName, IDictionary<string, object> parameters,
                                              IAdapterTransaction transaction)
        {
            IProcedureExecutor executor = _executors.GetOrAdd(functionName,
                                                              f =>
                                                              _connectionProvider.GetProcedureExecutor(this,
                                                                                                       _schema.
                                                                                                           BuildObjectName
                                                                                                           (f)));
            return executor.Execute(parameters, ((AdoAdapterTransaction) transaction).DbTransaction);
        }

        #endregion
    }
}