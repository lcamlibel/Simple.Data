﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Simple.Data.Extensions;

namespace Simple.Data.Ado
{
    internal class AdoAdapterUpserter
    {
        private readonly AdoAdapter _adapter;
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;

        public AdoAdapterUpserter(AdoAdapter adapter) : this(adapter, (IDbTransaction) null)
        {
        }

        public AdoAdapterUpserter(AdoAdapter adapter, IDbConnection connection)
        {
            _adapter = adapter;
            _connection = connection;
        }

        public AdoAdapterUpserter(AdoAdapter adapter, IDbTransaction transaction)
        {
            _adapter = adapter;
            _transaction = transaction;
            if (transaction != null) _connection = transaction.Connection;
        }

        public IDictionary<string, object> Upsert(string tableName, IDictionary<string, object> data,
                                                  SimpleExpression criteria, bool resultRequired)
        {
            IDbConnection connection = _connection ?? _adapter.CreateConnection();
            using (connection.MaybeDisposable())
            {
                connection.OpenIfClosed();
                return Upsert(tableName, data, criteria, resultRequired, connection);
            }
        }

        private IDictionary<string, object> Upsert(string tableName, IEnumerable<KeyValuePair<string, object>> data,
                                                   SimpleExpression criteria, bool resultRequired,
                                                   IDbConnection connection)
        {
            AdoAdapterFinder finder = _transaction == null
                                          ? new AdoAdapterFinder(_adapter, connection)
                                          : new AdoAdapterFinder(_adapter, _transaction);

            IDictionary<string, object> existing = finder.FindOne(tableName, criteria);
            if (existing != null)
            {
                // Don't update columns used as criteria
                IEnumerable<string> keys =
                    criteria.GetOperandsOfType<ObjectReference>().Select(o => o.GetName().Homogenize());
                IDictionary<string, object> updateData =
                    data.Where(kvp => keys.All(k => k != kvp.Key.Homogenize())).ToDictionary();
                if (updateData.Count == 0)
                {
                    return existing;
                }

                ICommandBuilder commandBuilder = new UpdateHelper(_adapter.GetSchema()).GetUpdateCommand(tableName,
                                                                                                         updateData,
                                                                                                         criteria);
                if (_transaction == null)
                {
                    _adapter.Execute(commandBuilder, connection);
                }
                else
                {
                    _adapter.Execute(commandBuilder, _transaction);
                }
                return resultRequired ? finder.FindOne(tableName, criteria) : null;
            }
            AdoAdapterInserter inserter = _transaction == null
                                              ? new AdoAdapterInserter(_adapter, connection)
                                              : new AdoAdapterInserter(_adapter, _transaction);
            return inserter.Insert(tableName, data, resultRequired);
        }


        public IEnumerable<IDictionary<string, object>> UpsertMany(string tableName,
                                                                   IList<IDictionary<string, object>> list,
                                                                   bool isResultRequired,
                                                                   Func<IDictionary<string, object>, Exception, bool>
                                                                       errorCallback)
        {
            for (int index = 0; index < list.Count; index++)
            {
                var row = list[index];
                IDictionary<string, object> result;
                try
                {
                    SimpleExpression criteria = ExpressionHelper.CriteriaDictionaryToExpression(tableName,
                                                                                                _adapter.GetKey(
                                                                                                    tableName, row));
                    result = Upsert(tableName, row, criteria, isResultRequired);
                }
                catch (Exception ex)
                {
                    if (errorCallback(row, ex)) continue;
                    throw;
                }

                yield return result;
            }
        }

        public IEnumerable<IDictionary<string, object>> UpsertMany(string tableName,
                                                                   IList<IDictionary<string, object>> list,
                                                                   IList<string> keyFieldNames, bool isResultRequired,
                                                                   Func<IDictionary<string, object>, Exception, bool>
                                                                       errorCallback)
        {
            for (int index = 0; index < list.Count; index++)
            {
                var row = list[index];
                IDictionary<string, object> result;
                try
                {
                    SimpleExpression criteria = GetCriteria(tableName, keyFieldNames, row);
                    result = Upsert(tableName, row, criteria, isResultRequired);
                }
                catch (Exception ex)
                {
                    if (errorCallback(row, ex)) continue;
                    throw;
                }

                yield return result;
            }
        }

        private static SimpleExpression GetCriteria(string tableName, IEnumerable<string> criteriaFieldNames,
                                                    IDictionary<string, object> record)
        {
            var criteria = new Dictionary<string, object>();

            foreach (string criteriaFieldName in criteriaFieldNames)
            {
                string name = criteriaFieldName;
                KeyValuePair<string, object> keyValuePair =
                    record.SingleOrDefault(kvp => kvp.Key.Homogenize().Equals(name.Homogenize()));
                if (string.IsNullOrWhiteSpace(keyValuePair.Key))
                {
                    throw new InvalidOperationException("Key field value not set.");
                }

                criteria.Add(criteriaFieldName, keyValuePair.Value);
                record.Remove(keyValuePair);
            }
            return ExpressionHelper.CriteriaDictionaryToExpression(tableName, criteria);
        }
    }
}