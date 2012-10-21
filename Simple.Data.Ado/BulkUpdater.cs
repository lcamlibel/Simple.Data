﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Simple.Data.Ado.Schema;

namespace Simple.Data.Ado
{
    internal class BulkUpdater : IBulkUpdater
    {
        #region IBulkUpdater Members

        public int Update(AdoAdapter adapter, string tableName, IList<IDictionary<string, object>> data,
                          IDbTransaction transaction)
        {
            return Update(adapter, tableName, data, adapter.GetKeyNames(tableName).ToList(), transaction);
        }

        public int Update(AdoAdapter adapter, string tableName, IList<IDictionary<string, object>> data,
                          IEnumerable<string> criteriaFieldNames, IDbTransaction transaction)
        {
            int count = 0;
            if (data == null || !data.Any())
                return count;

            List<string> criteriaFieldNameList = criteriaFieldNames.ToList();
            if (criteriaFieldNameList.Count == 0)
                throw new NotSupportedException("Adapter does not support key-based update for this object.");

            if (!AllRowsHaveSameKeys(data))
                throw new SimpleDataException(
                    "Records have different structures. Bulk updates are only valid on consistent records.");
            Table table = adapter.GetSchema().FindTable(tableName);

            var exampleRow = new Dictionary<string, object>(data.First(), HomogenizedEqualityComparer.DefaultInstance);

            ICommandBuilder commandBuilder = new UpdateHelper(adapter.GetSchema()).GetUpdateCommand(tableName,
                                                                                                    exampleRow,
                                                                                                    ExpressionHelper.
                                                                                                        CriteriaDictionaryToExpression
                                                                                                        (
                                                                                                            tableName,
                                                                                                            GetCriteria(
                                                                                                                criteriaFieldNameList,
                                                                                                                exampleRow)));

            IDbConnection connection = adapter.CreateConnection();
            using (connection.MaybeDisposable())
            using (IDbCommand command = commandBuilder.GetRepeatableCommand(connection, adapter.Options as AdoOptions))
            {
                if (transaction != null)
                {
                    command.Transaction = transaction;
                }
                connection.OpenIfClosed();
                Dictionary<string, IDbDataParameter> propertyToParameterMap = CreatePropertyToParameterMap(data, table,
                                                                                                           command);

                foreach (var row in data)
                {
                    foreach (var kvp in row)
                    {
                        if (propertyToParameterMap.ContainsKey(kvp.Key))
                        {
                            propertyToParameterMap[kvp.Key].Value = kvp.Value ?? DBNull.Value;
                        }
                    }
                    count += command.TryExecuteNonQuery();
                }
            }

            return count;
        }

        #endregion

        private static Dictionary<string, IDbDataParameter> CreatePropertyToParameterMap(
            IEnumerable<IDictionary<string, object>> data, Table table, IDbCommand command)
        {
            return data.First().Select(kvp => new
                                                  {
                                                      kvp.Key,
                                                      Value = GetDbDataParameter(table, command, kvp)
                                                  })
                .Where(t => t.Value != null)
                .ToDictionary(t => t.Key, t => t.Value);
        }

        private static IDbDataParameter GetDbDataParameter(Table table, IDbCommand command,
                                                           KeyValuePair<string, object> kvp)
        {
            try
            {
                return command.Parameters.Cast<IDbDataParameter>().
                    FirstOrDefault
                    (p =>
                     p.SourceColumn ==
                     table.FindColumn(kvp.Key).ActualName);
            }
            catch (UnresolvableObjectException)
            {
                return null;
            }
        }

        private static bool AllRowsHaveSameKeys(IList<IDictionary<string, object>> data)
        {
            var exemplar = new HashSet<string>(data.First().Keys);

            return data.Skip(1).All(d => exemplar.SetEquals(d.Keys));
        }

        private static Dictionary<string, object> GetCriteria(IEnumerable<string> keyFieldNames,
                                                              IDictionary<string, object> record)
        {
            var criteria = new Dictionary<string, object>();

            foreach (string keyFieldName in keyFieldNames)
            {
                if (!record.ContainsKey(keyFieldName))
                {
                    throw new InvalidOperationException("Key field value not set.");
                }

                criteria.Add(keyFieldName, record[keyFieldName]);
                record.Remove(keyFieldName);
            }
            return criteria;
        }
    }
}