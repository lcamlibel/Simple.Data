using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using Simple.Data.Ado.Schema;

namespace Simple.Data.Ado
{
    public class CommandBuilder : ICommandBuilder
    {
        private readonly ProviderHelper _customInterfaceProvider;
        private readonly string _parameterSuffix;
        private readonly Dictionary<ParameterTemplate, object> _parameters = new Dictionary<ParameterTemplate, object>();
        private readonly DatabaseSchema _schema;
        private readonly ISchemaProvider _schemaProvider;
        private readonly StringBuilder _text;
        private Func<IDbCommand, IDbParameterFactory> _getParameterFactory;
        private int _number;

        public CommandBuilder(DatabaseSchema schema) : this(schema, -1)
        {
        }

        public CommandBuilder(DatabaseSchema schema, int bulkIndex)
        {
            _text = new StringBuilder();
            _schema = schema;
            _schemaProvider = schema.SchemaProvider;
            _customInterfaceProvider = schema.ProviderHelper;
            _parameterSuffix = (bulkIndex >= 0) ? "_c" + bulkIndex : string.Empty;
        }

        public CommandBuilder(string text, DatabaseSchema schema, int bulkIndex)
        {
            _text = new StringBuilder(text);
            _schema = schema;
            _schemaProvider = schema.SchemaProvider;
            _customInterfaceProvider = schema.ProviderHelper;
            _parameterSuffix = (bulkIndex >= 0) ? "_c" + bulkIndex : string.Empty;
        }

        public CommandBuilder(string text, DatabaseSchema schema,
                              IEnumerable<KeyValuePair<ParameterTemplate, Object>> parameters)
            : this(text, schema, -1)
        {
            foreach (var kvp in parameters)
            {
                _parameters.Add(kvp.Key, kvp.Value);
            }
        }

        public string Joins { get; set; }

        #region ICommandBuilder Members

        public ParameterTemplate AddParameter(object value)
        {
            string name = _schemaProvider.NameParameter("p" + Interlocked.Increment(ref _number) + _parameterSuffix);
            var parameterTemplate = new ParameterTemplate(name, value);
            _parameters.Add(parameterTemplate, value);
            return parameterTemplate;
        }

        public ParameterTemplate AddParameter(object value, Column column)
        {
            string name = _schemaProvider.NameParameter("p" + Interlocked.Increment(ref _number) + _parameterSuffix);
            var parameterTemplate = new ParameterTemplate(name, column);
            _parameters.Add(parameterTemplate, value);
            return parameterTemplate;
        }

        public void Append(string text)
        {
            _text.Append(text);
        }

        public IEnumerable<KeyValuePair<ParameterTemplate, object>> Parameters
        {
            get { return _parameters.AsEnumerable(); }
        }

        public string Text
        {
            get { return _text.ToString(); }
        }

        public IDbCommand GetCommand(IDbConnection connection, AdoOptions options)
        {
            IDbCommand command = connection.CreateCommand(options);
            command.CommandText = Text;
            SetParameters(command, string.Empty);
            if (options != null)
            {
                command.CommandTimeout = options.CommandTimeout;
            }
            return command;
        }

        public IDbCommand GetRepeatableCommand(IDbConnection connection, AdoOptions options)
        {
            IDbCommand command = connection.CreateCommand(options);
            command.CommandText = Text;

            IDbParameterFactory parameterFactory = CreateParameterFactory(command);

            foreach (ParameterTemplate parameter in _parameters.Keys)
            {
                command.Parameters.Add(parameterFactory.CreateParameter(parameter.Name, parameter.Column));
            }
            if (options != null)
            {
                command.CommandTimeout = options.CommandTimeout;
            }
            return command;
        }

        public CommandTemplate GetCommandTemplate(Table table)
        {
            Dictionary<string, int> index =
                table.Columns.Select((c, i) => Tuple.Create(c, i)).ToDictionary(t => t.Item1.ActualName, t => t.Item2);
            return new CommandTemplate(CreateGetParameterFactoryFunc(), _text.ToString(), _parameters.Keys.ToArray(),
                                       new Dictionary<string, int>(index, HomogenizedEqualityComparer.DefaultInstance));
        }

        #endregion

        public ParameterTemplate AddParameter(string name, DbType dbType, object value)
        {
            name = _schemaProvider.NameParameter(name + _parameterSuffix);
            var parameterTemplate = new ParameterTemplate(name, dbType, 0);
            _parameters.Add(parameterTemplate, value);
            return parameterTemplate;
        }

        public void SetText(string text)
        {
            _text.Clear();
            _text.Append(text);
        }

        public override string ToString()
        {
            return _text.ToString();
        }


        private IDbParameterFactory CreateParameterFactory(IDbCommand command)
        {
            CreateGetParameterFactoryFunc();

            return _getParameterFactory(command);
        }

        private Func<IDbCommand, IDbParameterFactory> CreateGetParameterFactoryFunc()
        {
            if (_getParameterFactory == null)
            {
                var customParameterFactory =
                    _customInterfaceProvider.GetCustomProvider<IDbParameterFactory>(_schemaProvider);
                if (customParameterFactory != null)
                {
                    _getParameterFactory = _ => customParameterFactory;
                }
                else
                {
                    _getParameterFactory = c => new GenericDbParameterFactory(c);
                }
            }

            return _getParameterFactory;
        }

        private void SetParameters(IDbCommand command, string suffix)
        {
            SetParameters(command, _parameters);
        }

        private void SetParameters(IDbCommand command, IEnumerable<KeyValuePair<ParameterTemplate, object>> parameters)
        {
            IDbParameterFactory parameterFactory = CreateParameterFactory(command);
            SetParameters(parameterFactory, command, parameters);
        }

        private void SetParameters(IDbParameterFactory parameterFactory, IDbCommand command,
                                   IEnumerable<KeyValuePair<ParameterTemplate, object>> parameters)
        {
            List<KeyValuePair<ParameterTemplate, object>> parameterList = parameters.ToList();
            if (parameterList.Any(kvp => kvp.Value is IRange) ||
                parameterList.Any(kvp => kvp.Value is IEnumerable && !(kvp.Value is string)))
            {
                foreach (var pair in parameterList)
                {
                    foreach (
                        IDbDataParameter parameter in
                            CreateParameterComplex(parameterFactory, pair.Key, pair.Value, command))
                    {
                        command.Parameters.Add(parameter);
                    }
                }
            }
            else
            {
                foreach (var pair in parameterList)
                {
                    command.Parameters.Add(CreateSingleParameter(parameterFactory, pair.Value, pair.Key));
                }
            }
        }

        private IEnumerable<IDbDataParameter> CreateParameterComplex(IDbParameterFactory parameterFactory,
                                                                     ParameterTemplate template, object value,
                                                                     IDbCommand command)
        {
            if (template.Column != null && template.Column.IsBinary)
            {
                yield return CreateSingleParameter(parameterFactory, value, template.Name, template.Column);
            }
            else
            {
                var str = value as string;
                if (str != null)
                {
                    yield return CreateSingleParameter(parameterFactory, value, template.Name, template.Column);
                }
                else
                {
                    var range = value as IRange;
                    if (range != null)
                    {
                        yield return
                            CreateSingleParameter(parameterFactory, range.Start, template.Name + "_start",
                                                  template.Column);
                        yield return
                            CreateSingleParameter(parameterFactory, range.End, template.Name + "_end", template.Column);
                        SetBetweenInCommandText(command, template.Name);
                    }
                    else
                    {
                        var list = value as IEnumerable;
                        if (list != null)
                        {
                            var builder = new StringBuilder();
                            object[] array = list.Cast<object>().ToArray();
                            for (int i = 0; i < array.Length; i++)
                            {
                                builder.AppendFormat(",{0}_{1}", template.Name, i);
                                yield return
                                    CreateSingleParameter(parameterFactory, array[i], template.Name + "_" + i,
                                                          template.Column);
                            }
                            if (command.CommandText.Contains("!= " + template.Name))
                            {
                                command.CommandText = command.CommandText.Replace("!= " + template.Name,
                                                                                  _schema.Operators.NotIn + " (" +
                                                                                  builder.ToString().Substring(1) + ")");
                            }
                            else
                            {
                                command.CommandText = command.CommandText.Replace("= " + template.Name,
                                                                                  _schema.Operators.In + " (" +
                                                                                  builder.ToString().Substring(1) +
                                                                                  ")");
                            }
                        }
                        else
                        {
                            yield return CreateSingleParameter(parameterFactory, value, template);
                        }
                    }
                }
            }
        }

        public void SetBetweenInCommandText(IDbCommand command, string name)
        {
            if (command.CommandText.Contains("!= " + name))
            {
                command.CommandText = command.CommandText.Replace("!= " + name,
                                                                  string.Format("{0} {1}_start AND {1}_end",
                                                                                _schema.Operators.NotBetween, name));
            }
            else
            {
                command.CommandText = command.CommandText.Replace("= " + name,
                                                                  string.Format("{0} {1}_start AND {1}_end",
                                                                                _schema.Operators.Between, name));
            }
        }

        private static IDbDataParameter CreateSingleParameter(IDbParameterFactory parameterFactory, object value,
                                                              ParameterTemplate template)
        {
            if (template.Column != null)
            {
                return CreateSingleParameter(parameterFactory, value, template.Name, template.Column);
            }

            IDbDataParameter parameter = template.Type == ParameterType.NameOnly ? parameterFactory.CreateParameter(template.Name) : parameterFactory.CreateParameter(template.Name, template.DbType, template.MaxLength);
            parameter.Value = CommandHelper.FixObjectType(value);
            return parameter;
        }

        private static IDbDataParameter CreateSingleParameter(IDbParameterFactory parameterFactory, object value,
                                                              string name, Column column)
        {
            IDbDataParameter parameter = parameterFactory.CreateParameter(name, column);
            parameter.Value = CommandHelper.FixObjectType(value);
            return parameter;
        }

        internal IDbCommand CreateCommand(IDbParameterFactory parameterFactory, ICommandBuilder[] commandBuilders,
                                          IDbConnection connection, AdoOptions options)
        {
            IDbCommand command = connection.CreateCommand(options);
            parameterFactory = parameterFactory ?? new GenericDbParameterFactory(command);
            for (int i = 0; i < commandBuilders.Length; i++)
            {
                var commandBuilder = commandBuilders[i];
                if (!string.IsNullOrWhiteSpace(command.CommandText)) 
                    command.CommandText =String.Concat(command.CommandText , "; ");
                command.CommandText += commandBuilder.Text;
                SetParameters(parameterFactory, command, commandBuilder.Parameters);
            }
            return command;
        }

        internal IDbCommand CreateCommand(ICommandBuilder[] commandBuilders, IDbConnection connection,
                                          AdoOptions options)
        {
            IDbCommand command = connection.CreateCommand(options);
            for (int i = 0; i < commandBuilders.Length; i++)
            {
                var commandBuilder = commandBuilders[i];
                if (!string.IsNullOrWhiteSpace(command.CommandText))
                    command.CommandText = String.Concat(command.CommandText, "; ");
                command.CommandText += commandBuilder.Text;
                SetParameters(command, commandBuilder.Parameters);
            }
            return command;
        }
    }
}