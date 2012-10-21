﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Simple.Data.Ado;
using Simple.Data.Ado.Schema;
using Simple.Data.SqlServer.Properties;

namespace Simple.Data.SqlServer
{
    internal class SqlSchemaProvider : ISchemaProvider
    {
        private readonly IConnectionProvider _connectionProvider;

        public SqlSchemaProvider(IConnectionProvider connectionProvider)
        {
            if (connectionProvider == null) throw new ArgumentNullException("connectionProvider");
            _connectionProvider = connectionProvider;
        }

        public IConnectionProvider ConnectionProvider
        {
            get { return _connectionProvider; }
        }

        #region ISchemaProvider Members

        public IEnumerable<Table> GetTables()
        {
            return GetSchema("TABLES").Select(SchemaRowToTable);
        }

        public IEnumerable<Column> GetColumns(Table table)
        {
            if (table == null) throw new ArgumentNullException("table");
            DataTable cols = GetColumnsDataTable(table);
            return cols.AsEnumerable().Select(row => SchemaRowToColumn(table, row));
        }

        public IEnumerable<Procedure> GetStoredProcedures()
        {
            return GetSchema("Procedures").Select(SchemaRowToStoredProcedure);
        }

        public IEnumerable<Parameter> GetParameters(Procedure storedProcedure)
        {
            // GetSchema does not return the return value of e.g. a stored proc correctly,
            // i.e. there isn't sufficient information to correctly set up a stored proc.
            using (var connection = (SqlConnection) ConnectionProvider.CreateConnection())
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = storedProcedure.QualifiedName;

                    connection.Open();
                    SqlCommandBuilder.DeriveParameters(command);

                    //Tim Cartwright: I added size and dbtype so inout/out params would function properly.
                    for (int index = 0; index < command.Parameters.Count; index++)
                    {
                        SqlParameter p = command.Parameters[index];
                        yield return
                            new Parameter(p.ParameterName, SqlTypeResolver.GetClrType(p.DbType.ToString()), p.Direction,
                                          p.DbType, p.Size);
                    }
                }
            }
        }

        public Key GetPrimaryKey(Table table)
        {
            if (table == null) throw new ArgumentNullException("table");
            return new Key(GetPrimaryKeys(table.ActualName).AsEnumerable()
                               .Where(
                                   row =>
                                   row["TABLE_SCHEMA"].ToString() == table.Schema &&
                                   row["TABLE_NAME"].ToString() == table.ActualName)
                               .OrderBy(row => (int) row["ORDINAL_POSITION"])
                               .Select(row => row["COLUMN_NAME"].ToString()));
        }

        public IEnumerable<ForeignKey> GetForeignKeys(Table table)
        {
            if (table == null) throw new ArgumentNullException("table");
            List<IGrouping<string, DataRow>> groups = GetForeignKeys(table.ActualName)
                .Where(row =>
                       row["TABLE_SCHEMA"].ToString() == table.Schema &&
                       row["TABLE_NAME"].ToString() == table.ActualName)
                .GroupBy(row => row["CONSTRAINT_NAME"].ToString())
                .ToList();

            return from @group in groups select new ForeignKey(
                                             new ObjectName(@group.First()["TABLE_SCHEMA"].ToString(), @group.First()["TABLE_NAME"].ToString()),
                                             @group.Select(row => row["COLUMN_NAME"].ToString()),
                                             new ObjectName(@group.First()["UNIQUE_TABLE_SCHEMA"].ToString(),
                                                            @group.First()["UNIQUE_TABLE_NAME"].ToString()),
                                             @group.Select(row => row["UNIQUE_COLUMN_NAME"].ToString()));
        }

        public string QuoteObjectName(string unquotedName)
        {
            if (unquotedName == null) throw new ArgumentNullException("unquotedName");
            if (unquotedName.StartsWith("[")) return unquotedName;
            return string.Concat("[", unquotedName, "]");
        }

        public string NameParameter(string baseName)
        {
            if (baseName == null) throw new ArgumentNullException("baseName");
            if (baseName.Length == 0) throw new ArgumentException("Base name must be provided");
            return (baseName.StartsWith("@")) ? baseName : "@" + baseName;
        }

        public String GetDefaultSchema()
        {
            return "dbo";
        }

        #endregion

        private static Table SchemaRowToTable(DataRow row)
        {
            return new Table(row["TABLE_NAME"].ToString(), row["TABLE_SCHEMA"].ToString(),
                             row["TABLE_TYPE"].ToString() == "BASE TABLE" ? TableType.Table : TableType.View);
        }

        private static Column SchemaRowToColumn(Table table, DataRow row)
        {
            var sqlDbType = SqlDbType.Udt;

            if (!row.IsNull("type_name"))
                sqlDbType = DbTypeFromInformationSchemaTypeName((string) row["type_name"]);

            var size = (short) row["max_length"];
            switch (sqlDbType)
            {
                case SqlDbType.Image:
                case SqlDbType.NText:
                case SqlDbType.Text:
                    size = -1;
                    break;
                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                    size = (short) (size/2);
                    break;
            }

            return new SqlColumn(row["name"].ToString(), table, (bool) row["is_identity"], sqlDbType, size);
        }

        private IEnumerable<DataRow> GetSchema(string collectionName, params string[] constraints)
        {
            using (IDbConnection cn = ConnectionProvider.CreateConnection())
            {
                cn.Open();

                return cn.GetSchema(collectionName, constraints).AsEnumerable();
            }
        }

        private static Procedure SchemaRowToStoredProcedure(DataRow row)
        {
            return new Procedure(row["ROUTINE_NAME"].ToString(), row["SPECIFIC_NAME"].ToString(),
                                 row["ROUTINE_SCHEMA"].ToString());
        }

        public Type DataTypeToClrType(string dataType)
        {
            return SqlTypeResolver.GetClrType(dataType);
        }

        private DataTable GetColumnsDataTable(Table table)
        {
            const string columnSelect =
                @"SELECT name, is_identity, type_name(system_type_id) as type_name, max_length from sys.columns 
where object_id = object_id(@tableName, 'TABLE') or object_id = object_id(@tableName, 'VIEW') order by column_id";
            var @tableName = new SqlParameter("@tableName", SqlDbType.NVarChar, 128);
            @tableName.Value = string.Format("[{0}].[{1}]", table.Schema, table.ActualName);
            return SelectToDataTable(columnSelect, @tableName);
        }

        private DataTable GetPrimaryKeys()
        {
            return SelectToDataTable(Resources.PrimaryKeySql);
        }

        private DataTable GetForeignKeys()
        {
            return SelectToDataTable(Resources.ForeignKeysSql);
        }

        private DataTable GetPrimaryKeys(string tableName)
        {
            DataTable primaryKeys = GetPrimaryKeys();
            DataTable dataTable =
                primaryKeys.AsEnumerable().Where(
                    row => row["TABLE_NAME"].ToString().Equals(tableName, StringComparison.InvariantCultureIgnoreCase)).
                    CopyToDataTable();
            return dataTable;
        }

        private EnumerableRowCollection<DataRow> GetForeignKeys(string tableName)
        {
            return GetForeignKeys().AsEnumerable()
                .Where(
                    row => row["TABLE_NAME"].ToString().Equals(tableName, StringComparison.InvariantCultureIgnoreCase));
        }

        private DataTable SelectToDataTable(string sql, params SqlParameter[] parameters)
        {
            var dataTable = new DataTable();
            using (var cn = ConnectionProvider.CreateConnection() as SqlConnection)
            {
                using (var adapter = new SqlDataAdapter(sql, cn))
                {
                    adapter.SelectCommand.Parameters.AddRange(parameters);
                    adapter.Fill(dataTable);
                }
            }

            return dataTable;
        }

        private static SqlDbType DbTypeFromInformationSchemaTypeName(string informationSchemaTypeName)
        {
            return DbTypeLookup.GetSqlDbType(informationSchemaTypeName);
        }
    }
}