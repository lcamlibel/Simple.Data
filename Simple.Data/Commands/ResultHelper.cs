using System.Collections.Generic;
using System.Linq;
using Simple.Data.Extensions;

namespace Simple.Data.Commands
{
    internal static class ResultHelper
    {
        public static object TypeResult(object result, DynamicTable table, DataStrategy dataStrategy)
        {
            var dictionary = result as IDictionary<string, object>;
            if (dictionary != null) return dictionary.ToDynamicRecord(table.GetQualifiedName(), dataStrategy);

            var list = result as IEnumerable<IDictionary<string, object>>;
            if (list != null)
                return new SimpleResultSet(list.Select(d => d.ToDynamicRecord(table.GetQualifiedName(), dataStrategy)));

            return null;
        }
    }
}