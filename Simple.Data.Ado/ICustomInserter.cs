using System.Collections.Generic;
using System.Data;

namespace Simple.Data.Ado
{
    public interface ICustomInserter
    {
        IDictionary<string, object> Insert(AdoAdapter adapter, string tableName, IDictionary<string, object> data,
                                           IDbTransaction transaction = null, bool resultRequired = false);
    }
}