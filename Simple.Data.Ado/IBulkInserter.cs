using System;
using System.Collections.Generic;
using System.Data;

namespace Simple.Data.Ado
{
    public interface IBulkInserter
    {
        IEnumerable<IDictionary<string, object>> Insert(AdoAdapter adapter, string tableName,
                                                        IEnumerable<IDictionary<string, object>> data,
                                                        IDbTransaction transaction,
                                                        Func<IDictionary<string, object>, Exception, bool> onError,
                                                        bool resultRequired);
    }
}