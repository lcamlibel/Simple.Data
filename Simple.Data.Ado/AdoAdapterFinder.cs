using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;

namespace Simple.Data.Ado
{
    internal class AdoAdapterFinder
    {
        private readonly AdoAdapter _adapter;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CommandTemplate>> _commandCaches =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, CommandTemplate>>();

        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;

        public AdoAdapterFinder(AdoAdapter adapter) : this(adapter, (IDbTransaction) null)
        {
        }

        public AdoAdapterFinder(AdoAdapter adapter, IDbConnection connection)
        {
            _adapter = adapter;
            _connection = connection;
        }

        public AdoAdapterFinder(AdoAdapter adapter, IDbTransaction transaction)
        {
            if (adapter == null) throw new ArgumentNullException("adapter");
            _adapter = adapter;

            if (transaction != null)
            {
                _transaction = transaction;
                _connection = transaction.Connection;
            }
        }

        private Func<IDbConnection> ConnectionCreator
        {
            get
            {
                if (_transaction != null)
                {
                    return () => _transaction.Connection;
                }
                return _adapter.CreateConnection;
            }
        }

        public IDictionary<string, object> FindOne(string tableName, SimpleExpression criteria)
        {
            if (criteria == null) return FindAll(_adapter.GetSchema().BuildObjectName(tableName)).FirstOrDefault();
            CommandTemplate commandTemplate = GetCommandTemplate(tableName, criteria);
            return ExecuteSingletonQuery(commandTemplate, criteria.GetValues());
        }

        public Func<object[], IDictionary<string, object>> CreateFindOneDelegate(string tableName,
                                                                                 SimpleExpression criteria)
        {
            if (criteria == null)
            {
                return _ => FindAll(_adapter.GetSchema().BuildObjectName(tableName)).FirstOrDefault();
            }
            ICommandBuilder commandBuilder = new FindHelper(_adapter.GetSchema())
                .GetFindByCommand(_adapter.GetSchema().BuildObjectName(tableName), criteria);

            IDbCommand command = commandBuilder.GetCommand(_adapter.CreateConnection(), _adapter.AdoOptions);
            command = _adapter.CommandOptimizer.OptimizeFindOne(command);

            CommandTemplate commandTemplate =
                commandBuilder.GetCommandTemplate(
                    _adapter.GetSchema().FindTable(_adapter.GetSchema().BuildObjectName(tableName)));

            var cloneable = command as ICloneable;
            if (cloneable != null)
            {
                return args => ExecuteSingletonQuery((IDbCommand) cloneable.Clone(), args, commandTemplate.Index);
            }
            return args => ExecuteSingletonQuery(commandTemplate, args);
        }

        private IDictionary<string, object> ExecuteSingletonQuery(IDbCommand command, object[] parameterValues,
                                                                  IDictionary<string, int> index)
        {
            for (int i = 0; i < command.Parameters.Count; i++)
            {
                ((IDbDataParameter) command.Parameters[i]).Value = FixObjectType(parameterValues[i]);
            }
            command.Connection = _adapter.CreateConnection();
            return TryExecuteSingletonQuery(command.Connection, command, index);
        }

        public IEnumerable<IDictionary<string, object>> Find(string tableName, SimpleExpression criteria)
        {
            if (criteria == null) return FindAll(_adapter.GetSchema().BuildObjectName(tableName));
            CommandTemplate commandTemplate = GetCommandTemplate(tableName, criteria);
            return ExecuteQuery(commandTemplate, criteria.GetValues());
        }

        private CommandTemplate GetCommandTemplate(string tableName, SimpleExpression criteria)
        {
            ConcurrentDictionary<string, CommandTemplate> tableCommandCache = _commandCaches.GetOrAdd(tableName,
                                                                                                      _ =>
                                                                                                      new ConcurrentDictionary
                                                                                                          <string,
                                                                                                          CommandTemplate
                                                                                                          >());

            string hash = new ExpressionHasher().Format(criteria);
            return tableCommandCache.GetOrAdd(hash,
                                              _ =>
                                              new FindHelper(_adapter.GetSchema())
                                                  .GetFindByCommand(_adapter.GetSchema().BuildObjectName(tableName),
                                                                    criteria)
                                                  .GetCommandTemplate(
                                                      _adapter.GetSchema().FindTable(
                                                          _adapter.GetSchema().BuildObjectName(tableName))));
        }

        private IEnumerable<IDictionary<string, object>> FindAll(ObjectName tableName)
        {
            return ExecuteQuery("select * from " + _adapter.GetSchema().FindTable(tableName).QualifiedName);
        }

        private IEnumerable<IDictionary<string, object>> ExecuteQuery(CommandTemplate commandTemplate,
                                                                      IEnumerable<object> parameterValues)
        {
            IDbConnection connection = _connection ?? _adapter.CreateConnection();
            IDbCommand command = commandTemplate.GetDbCommand(_adapter, connection, parameterValues);
            command.Transaction = _transaction;
            return TryExecuteQuery(connection, command, commandTemplate.Index);
        }

        private IDictionary<string, object> ExecuteSingletonQuery(CommandTemplate commandTemplate,
                                                                  IEnumerable<object> parameterValues)
        {
            IDbConnection connection = _connection ?? _adapter.CreateConnection();
            IDbCommand command = commandTemplate.GetDbCommand(_adapter, connection, parameterValues);
            command.Transaction = _transaction;
            return TryExecuteSingletonQuery(connection, command, commandTemplate.Index);
        }

        private IEnumerable<IDictionary<string, object>> ExecuteQuery(string sql, params object[] values)
        {
            IDbConnection connection = _connection ?? _adapter.CreateConnection();
            IDbCommand command = new CommandHelper(_adapter).Create(connection, sql, values);
            command.Transaction = _transaction;
            return TryExecuteQuery(connection, command);
        }

        private IEnumerable<IDictionary<string, object>> TryExecuteQuery(IDbConnection connection, IDbCommand command)
        {
            try
            {
                return command.ToEnumerable(ConnectionCreator);
            }
            catch (DbException ex)
            {
                throw new AdoAdapterException(ex.Message, command);
            }
        }

        private IEnumerable<IDictionary<string, object>> TryExecuteQuery(IDbConnection connection, IDbCommand command,
                                                                         IDictionary<string, int> index)
        {
            try
            {
                return command.ToEnumerable(ConnectionCreator);
            }
            catch (DbException ex)
            {
                throw new AdoAdapterException(ex.Message, command);
            }
        }

        private static IDictionary<string, object> TryExecuteSingletonQuery(IDbConnection connection, IDbCommand command,
                                                                            IDictionary<string, int> index)
        {
            using (connection.MaybeDisposable())
            using (command)
            {
                connection.OpenIfClosed();
                using (IDataReader reader = command.TryExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.ToDictionary(index);
                    }
                }
            }
            return null;
        }

        private static object FixObjectType(object value)
        {
            if (value == null) return DBNull.Value;
            if (TypeHelper.IsKnownType(value.GetType())) return value;
            var dynamicObject = value as DynamicObject;
            if (dynamicObject != null)
            {
                return dynamicObject.ToString();
            }
            return value;
        }
    }
}