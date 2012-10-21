using System.Collections.Generic;
using System.Linq;

namespace Simple.Data
{
    internal class TransactionRunner : RunStrategy
    {
        private readonly IAdapterWithTransactions _adapter;
        private readonly IAdapterTransaction _adapterTransaction;

        public TransactionRunner(IAdapterWithTransactions adapter, IAdapterTransaction adapterTransaction)
        {
            _adapter = adapter;
            _adapterTransaction = adapterTransaction;
        }

        protected override Adapter Adapter
        {
            get { return (Adapter) _adapter; }
        }

        internal override IDictionary<string, object> FindOne(string tableName, SimpleExpression criteria)
        {
            return Find(tableName, criteria).FirstOrDefault();
        }

        internal override int UpdateMany(string tableName, IList<IDictionary<string, object>> dataList)
        {
            return _adapter.UpdateMany(tableName, dataList, _adapterTransaction);
        }

        internal override int UpdateMany(string tableName, IList<IDictionary<string, object>> dataList,
                                         IEnumerable<string> criteriaFieldNames)
        {
            return _adapter.UpdateMany(tableName, dataList, criteriaFieldNames, _adapterTransaction);
        }

        internal override IEnumerable<IDictionary<string, object>> Find(string tableName, SimpleExpression criteria)
        {
            return _adapter.Find(tableName, criteria, _adapterTransaction);
        }

        internal override IDictionary<string, object> Insert(string tableName, IDictionary<string, object> data,
                                                             bool resultRequired)
        {
            return _adapter.Insert(tableName, data, _adapterTransaction, resultRequired);
        }

        /// <summary>
        ///  Inserts a record into the specified "table".
        ///  </summary><param name="tableName">Name of the table.</param>
        /// <param name="data">The values to insert.</param>
        /// <param name="resultRequired"></param> 
        /// <param name="onError"></param> 
        /// <returns>If possible, return the newly inserted row, including any automatically-set values such as primary keys or timestamps.</returns>
        internal override IEnumerable<IDictionary<string, object>> InsertMany(string tableName,
                                                                              IEnumerable<IDictionary<string, object>>
                                                                                  data, ErrorCallback onError,
                                                                              bool resultRequired)
        {
            return _adapter.InsertMany(tableName, data, _adapterTransaction,
                                       (dict, exception) => onError(new SimpleRecord(dict), exception), resultRequired);
        }

        internal override int Update(string tableName, IDictionary<string, object> data, SimpleExpression criteria)
        {
            return _adapter.Update(tableName, data, criteria, _adapterTransaction);
        }

        public override IDictionary<string, object> Upsert(string tableName, IDictionary<string, object> dict,
                                                           SimpleExpression criteriaExpression, bool isResultRequired)
        {
            return _adapter.Upsert(tableName, dict, criteriaExpression, isResultRequired, _adapterTransaction);
        }

        public override IEnumerable<IDictionary<string, object>> UpsertMany(string tableName,
                                                                            IList<IDictionary<string, object>> list,
                                                                            bool isResultRequired,
                                                                            ErrorCallback errorCallback)
        {
            return _adapter.UpsertMany(tableName, list, _adapterTransaction, isResultRequired,
                                       (dict, exception) => errorCallback(new SimpleRecord(dict), exception));
        }

        public override IDictionary<string, object> Get(string tableName, object[] args)
        {
            return _adapter.Get(tableName, _adapterTransaction, args);
        }

        public override IEnumerable<IDictionary<string, object>> RunQuery(SimpleQuery query,
                                                                          out IEnumerable<SimpleQueryClauseBase>
                                                                              unhandledClauses)
        {
            return _adapter.RunQuery(query, _adapterTransaction, out unhandledClauses);
        }

        public override IEnumerable<IDictionary<string, object>> UpsertMany(string tableName,
                                                                            IList<IDictionary<string, object>> list,
                                                                            IEnumerable<string> keyFieldNames,
                                                                            bool isResultRequired,
                                                                            ErrorCallback errorCallback)
        {
            return _adapter.UpsertMany(tableName, list, keyFieldNames, _adapterTransaction, isResultRequired,
                                       (dict, exception) => errorCallback(new SimpleRecord(dict), exception));
        }

        internal override int UpdateMany(string tableName, IList<IDictionary<string, object>> newValuesList,
                                         IList<IDictionary<string, object>> originalValuesList)
        {
            return newValuesList.Select((t, i) => Update(tableName, t, originalValuesList[i])).Sum();
        }

        internal override int Update(string tableName, IDictionary<string, object> newValuesDict,
                                     IDictionary<string, object> originalValuesDict)
        {
            SimpleExpression criteria = CreateCriteriaFromOriginalValues(tableName, newValuesDict, originalValuesDict);
            Dictionary<string, object> changedValuesDict = CreateChangedValuesDict(newValuesDict, originalValuesDict);
            return _adapter.Update(tableName, changedValuesDict, criteria, _adapterTransaction);
        }

        internal override int Delete(string tableName, SimpleExpression criteria)
        {
            return _adapter.Delete(tableName, criteria, _adapterTransaction);
        }
    }
}