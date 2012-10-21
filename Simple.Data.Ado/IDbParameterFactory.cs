using System.Data;
using Simple.Data.Ado.Schema;

namespace Simple.Data.Ado
{
    public interface IDbParameterFactory
    {
        IDbDataParameter CreateParameter(string name);
        IDbDataParameter CreateParameter(string name, Column column);
        IDbDataParameter CreateParameter(string name, DbType dbType, int maxLength);
    }
}