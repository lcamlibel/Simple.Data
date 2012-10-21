﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Simple.Data.Ado.Schema;

namespace Simple.Data.Ado
{
    public class BulkInserter : IBulkInserter
    {
        #region IBulkInserter Members

        public IEnumerable<IDictionary<string, object>> Insert(AdoAdapter adapter, string tableName,
                                                               IEnumerable<IDictionary<string, object>> data,
                                                               IDbTransaction transaction,
                                                               Func<IDictionary<string, object>, Exception, bool>
                                                                   onError, bool resultRequired)
        {
            Table table = adapter.GetSchema().FindTable(tableName);
            List<Column> columns = table.Columns.Where(c => c.IsWriteable).ToList();

            string columnList = string.Join(",", columns.Select(c => c.QuotedName));
            string valueList = string.Join(",", columns.Select(c => "?"));

            string insertSql = "insert into " + table.QualifiedName + " (" + columnList + ") values (" + valueList + ")";

            BulkInserterHelper helper = transaction == null
                                            ? new BulkInserterHelper(adapter, data, table, columns)
                                            : new BulkInserterTransactionHelper(adapter, data, table, columns,
                                                                                transaction);

            if (resultRequired)
            {
                Column identityColumn = table.Columns.FirstOrDefault(col => col.IsIdentity);
                if (identityColumn != null)
                {
                    string identityFunction = adapter.GetIdentityFunction();
                    if (!string.IsNullOrWhiteSpace(identityFunction))
                    {
                        return InsertRowsAndReturn(adapter, identityFunction, helper, insertSql, table, onError);
                    }
                }
            }

            helper.InsertRowsWithoutFetchBack(insertSql, onError);

            return null;
        }

        #endregion

        private static IEnumerable<IDictionary<string, object>> InsertRowsAndReturn(AdoAdapter adapter,
                                                                                    string identityFunction,
                                                                                    BulkInserterHelper helper,
                                                                                    string insertSql, Table table,
                                                                                    Func
                                                                                        <IDictionary<string, object>,
                                                                                        Exception, bool> onError)
        {
            Column identityColumn = table.Columns.FirstOrDefault(col => col.IsIdentity);

            if (identityColumn != null)
            {
                string selectSql = "select * from " + table.QualifiedName + " where " + identityColumn.QuotedName +
                                   " = " + identityFunction;
                if (adapter.ProviderSupportsCompoundStatements)
                {
                    return helper.InsertRowsWithCompoundStatement(insertSql, selectSql, onError);
                }
                return helper.InsertRowsWithSeparateStatements(insertSql, selectSql, onError);
            }

            return null;
        }
    }
}