using System.Collections.Generic;
using System.Data;
using Simple.Data.Ado.Schema;

namespace Simple.Data.Ado
{
    public interface ICommandBuilder
    {
        IEnumerable<KeyValuePair<ParameterTemplate, object>> Parameters { get; }
        string Text { get; }
        ParameterTemplate AddParameter(object value);
        ParameterTemplate AddParameter(object value, Column column);
        void Append(string text);
        IDbCommand GetCommand(IDbConnection connection, AdoOptions options);
        IDbCommand GetRepeatableCommand(IDbConnection connection, AdoOptions options);
        CommandTemplate GetCommandTemplate(Table table);
    }
}