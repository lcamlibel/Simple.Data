using System.Collections.Generic;

namespace Simple.Data.Extensions
{
    internal static class DictionaryExtensions
    {
        public static SimpleRecord ToDynamicRecord(this IDictionary<string, object> dictionary, string tableName,
                                                   DataStrategy dataStrategy)
        {
            return dictionary == null ? null : new SimpleRecord(dictionary, tableName, dataStrategy);
        }
    }
}