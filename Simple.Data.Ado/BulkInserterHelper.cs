using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Simple.Data.Ado.Schema;

namespace Simple.Data.Ado
{
    internal class BulkInserterHelper
    {
        protected readonly AdoAdapter Adapter;
        protected readonly IEnumerable<IDictionary<string, object>> Data;
        private readonly List<Column> _columns;
        private readonly Table _table;
        private Action<IDictionary<string, object>, IDbCommand> _parameterSetter;

        public BulkInserterHelper(AdoAdapter adapter, IEnumerable<IDictionary<string, object>> data, Table table,
                                  List<Column> columns)
        {
            Adapter = adapter;
            Data = data;
            _table = table;
            _columns = columns;
        }

        public virtual void InsertRowsWithoutFetchBack(string insertSql,
                                                       Func<IDictionary<string, object>, Exception, bool> onError)
        {
            IDbConnection connection = Adapter.CreateConnection();
            using (connection.MaybeDisposable())
            {
                using (
                    IDbCommand insertCommand = new CommandHelper(Adapter).CreateInsert(connection, insertSql, _columns))
                {
                    connection.OpenIfClosed();
                    TryPrepare(insertCommand);
                    foreach (var row in Data)
                    {
                        InsertRow(row, insertCommand, onError);
                    }
                }
            }
        }

        public virtual IEnumerable<IDictionary<string, object>> InsertRowsWithSeparateStatements(string insertSql,
                                                                                                 string selectSql,
                                                                                                 Func
                                                                                                     <
                                                                                                     IDictionary
                                                                                                     <string, object>,
                                                                                                     Exception, bool>
                                                                                                     onError)
        {
            IDbConnection connection = Adapter.CreateConnection();
            using (connection.MaybeDisposable())
            {
                using (
                    IDbCommand insertCommand = new CommandHelper(Adapter).CreateInsert(connection, insertSql, _columns))
                using (IDbCommand selectCommand = connection.CreateCommand(Adapter.AdoOptions))
                {
                    selectCommand.CommandText = selectSql;
                    connection.OpenIfClosed();
                    TryPrepare(insertCommand, selectCommand);
                    return
                        Data.Select(row => InsertRow(row, insertCommand, selectCommand, onError)).Where(r => r != null).
                            ToList();
                }
            }
        }

        public virtual IEnumerable<IDictionary<string, object>> InsertRowsWithCompoundStatement(string insertSql,
                                                                                                string selectSql,
                                                                                                Func
                                                                                                    <
                                                                                                    IDictionary
                                                                                                    <string, object>,
                                                                                                    Exception, bool>
                                                                                                    onError)
        {
            insertSql += "; " + selectSql;

            IDbConnection connection = Adapter.CreateConnection();
            using (connection.MaybeDisposable())
            {
                using (IDbCommand command = new CommandHelper(Adapter).CreateInsert(connection, insertSql, _columns))
                {
                    connection.OpenIfClosed();
                    TryPrepare(command);
                    return Data.Select(row => InsertRowAndSelect(row, command, onError)).Where(r => r != null).ToList();
                }
            }
        }

        protected IDictionary<string, object> InsertRowAndSelect(IDictionary<string, object> row, IDbCommand command,
                                                                 Func<IDictionary<string, object>, Exception, bool>
                                                                     onError)
        {
            if (_parameterSetter == null) _parameterSetter = BuildParameterSettingAction(row);
            _parameterSetter(row, command);

            try
            {
                IDictionary<string, object> insertedRow = TryExecuteSingletonQuery(command);
                return insertedRow;
            }
            catch (Exception ex)
            {
                if (onError(row, ex)) return null;
                throw;
            }
        }

        protected int InsertRow(IDictionary<string, object> row, IDbCommand command,
                                Func<IDictionary<string, object>, Exception, bool> onError)
        {
            if (_parameterSetter == null) _parameterSetter = BuildParameterSettingAction(row);
            _parameterSetter(row, command);

            try
            {
                return command.TryExecuteNonQuery();
            }
            catch (Exception ex)
            {
                if (onError(row, ex)) return 0;
                throw;
            }
        }

        protected IDictionary<string, object> InsertRow(IDictionary<string, object> row, IDbCommand insertCommand,
                                                        IDbCommand selectCommand,
                                                        Func<IDictionary<string, object>, Exception, bool> onError)
        {
            if (_parameterSetter == null) _parameterSetter = BuildParameterSettingAction(row);
            _parameterSetter(row, insertCommand);

            try
            {
                if (insertCommand.TryExecuteNonQuery() == 1)
                    return TryExecuteSingletonQuery(selectCommand);
            }
            catch (Exception ex)
            {
                if (onError(row, ex)) return null;
                throw;
            }
            return null;
        }

        private static IDictionary<string, object> TryExecuteSingletonQuery(IDbCommand command)
        {
            using (IDataReader reader = command.TryExecuteReader())
            {
                if (reader.Read())
                {
                    return reader.ToDictionary();
                }
            }

            return null;
        }

        private static void TryPrepare(params IDbCommand[] commands)
        {
            for (int index = 0; index < commands.Length; index++)
            {
                IDbCommand command = commands[index];
                try
                {
                    command.Prepare();
                }
                catch (InvalidOperationException)
                {
                    Trace.TraceWarning("Could not prepare command.");
                }
            }
        }

        private Action<IDictionary<string, object>, IDbCommand> BuildParameterSettingAction(
            IDictionary<string, object> sample)
        {
            Action<IDictionary<string, object>, IDbCommand>[] actions =
                _columns.Select<Column, Action<IDictionary<string, object>, IDbCommand>>(
                    (c, i) => (row, cmd) => cmd.SetParameterValue(i, null)).ToArray();

            string[] usedColumnNames =
                sample.Keys.Where(
                    k => _columns.Any(c => String.Equals(c.ActualName, k, StringComparison.InvariantCultureIgnoreCase)))
                    .ToArray();

            for (int i = 0; i < usedColumnNames.Length; i++)
            {
                string columnName = usedColumnNames[i];
                int index = _columns.IndexOf(_table.FindColumn(columnName));
                if (index >= 0)
                    actions[index] = BuildIndividualFunction(columnName, index);

                ++index;
            }

            return actions.Aggregate((working, next) => working + next) ?? ((row, cmd) => { });
        }

        private Action<IDictionary<string, object>, IDbCommand> BuildIndividualFunction(string key, int index)
        {
            return (dict, command) => command.SetParameterValue(index, dict[key]);
        }
    }
}