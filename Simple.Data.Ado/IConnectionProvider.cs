using System.Data;
using Simple.Data.Ado.Schema;

namespace Simple.Data.Ado
{
    public interface IConnectionProvider
    {
        string ConnectionString { get; }
        bool SupportsCompoundStatements { get; }
        bool SupportsStoredProcedures { get; }
        void SetConnectionString(string connectionString);
        IDbConnection CreateConnection();
        ISchemaProvider GetSchemaProvider();
        string GetIdentityFunction();
        IProcedureExecutor GetProcedureExecutor(AdoAdapter adapter, ObjectName procedureName);
    }
}