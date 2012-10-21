using System;
using System.Data;
using Simple.Data.Ado.Schema;

namespace Simple.Data.Ado
{
    internal class GenericDbParameterFactory : IDbParameterFactory
    {
        private readonly IDbCommand _command;

        public GenericDbParameterFactory(IDbCommand command)
        {
            if (command == null) throw new ArgumentNullException("command");
            _command = command;
        }

        #region IDbParameterFactory Members

        public IDbDataParameter CreateParameter(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            IDbDataParameter parameter = _command.CreateParameter();
            parameter.ParameterName = name;
            return parameter;
        }

        public IDbDataParameter CreateParameter(string name, Column column)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (column == null) throw new ArgumentNullException("column");
            IDbDataParameter parameter = _command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = column.DbType;
            parameter.Size = column.DbType == DbType.StringFixedLength || column.DbType == DbType.AnsiStringFixedLength
                                 ? 0
                                 : column.MaxLength;
            parameter.SourceColumn = column.ActualName;
            return parameter;
        }

        public IDbDataParameter CreateParameter(string name, DbType dbType, int size)
        {
            if (name == null) throw new ArgumentNullException("name");
            IDbDataParameter parameter = _command.CreateParameter();
            parameter.ParameterName = name;
            parameter.DbType = dbType;
            parameter.Size = size;
            return parameter;
        }

        #endregion
    }
}