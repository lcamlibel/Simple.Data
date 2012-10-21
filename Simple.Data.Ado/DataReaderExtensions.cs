using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Simple.Data.Ado
{
    public static class DataReaderExtensions
    {
        public static IDictionary<string, object> ToDictionary(this IDataReader dataReader)
        {
            return dataReader.ToDictionary(dataReader.CreateDictionaryIndex());
        }

        public static IEnumerable<IDictionary<string, object>> ToDictionaries(this IDataReader reader)
        {
            using (reader)
            {
                return ToDictionariesImpl(reader).ToArray().AsEnumerable();
            }
        }

        public static IEnumerable<IEnumerable<IDictionary<string, object>>> ToMultipleDictionaries(
            this IDataReader reader)
        {
            using (reader)
            {
                return ToMultipleDictionariesImpl(reader).ToArray().AsEnumerable();
            }
        }

        private static IEnumerable<IEnumerable<IDictionary<string, object>>> ToMultipleDictionariesImpl(
            IDataReader reader)
        {
            do
            {
                yield return ToDictionariesImpl(reader).ToArray().AsEnumerable();
            } while (reader.NextResult());
        }

        private static IEnumerable<IDictionary<string, object>> ToDictionariesImpl(IDataReader reader)
        {
            Dictionary<string, int> index = reader.CreateDictionaryIndex();
            var values = new object[reader.FieldCount];
            while (reader.Read())
            {
                reader.GetValues(values);

                ReplaceDbNullsWithClrNulls(values);

                yield return OptimizedDictionary.Create(index, values);
            }
        }

        private static void ReplaceDbNullsWithClrNulls(object[] values)
        {
            int dbNullIndex;
            while ((dbNullIndex = Array.IndexOf(values, DBNull.Value)) > -1)
            {
                values[dbNullIndex] = null;
            }
        }
    }
}