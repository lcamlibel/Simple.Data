using System.Collections.Generic;
using System.Data;

namespace Simple.Data.Ado
{
    public interface IBulkUpdater
    {
        int Update(AdoAdapter adapter, string tableName, IList<IDictionary<string, object>> data,
                   IDbTransaction transaction);

        int Update(AdoAdapter adapter, string tableName, IList<IDictionary<string, object>> toList,
                   IEnumerable<string> criteriaFieldNames, IDbTransaction dbTransaction);
    }
}