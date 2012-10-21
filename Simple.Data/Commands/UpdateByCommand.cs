using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Simple.Data.Extensions;

namespace Simple.Data.Commands
{
    internal class UpdateByCommand : ICommand
    {
        #region ICommand Members

        public bool IsCommandFor(string method)
        {
            return method.Homogenize().StartsWith("updateby", StringComparison.InvariantCultureIgnoreCase);
        }

        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            if (binder.HasSingleUnnamedArgument())
            {
                return UpdateByKeyFields(table.GetQualifiedName(), dataStrategy, args[0],
                                         MethodNameParser.ParseCriteriaNamesFromMethodName(binder.Name));
            }

            IDictionary<string, object> criteria = MethodNameParser.ParseFromBinder(binder, args);
            SimpleExpression criteriaExpression =
                ExpressionHelper.CriteriaDictionaryToExpression(table.GetQualifiedName(), criteria);
            IDictionary<string, object> data = binder.NamedArgumentsToDictionary(args)
                .Where(kvp => !criteria.ContainsKey(kvp.Key))
                .ToDictionary();
            return dataStrategy.Run.Update(table.GetQualifiedName(), data, criteriaExpression);
        }

        #endregion

        internal static object UpdateByKeyFields(string tableName, DataStrategy dataStrategy, object entity,
                                                 IEnumerable<string> keyFieldNames)
        {
            object record = UpdateCommand.ObjectToDictionary(entity);
            var list = record as IList<IDictionary<string, object>>;
            if (list != null) return dataStrategy.Run.UpdateMany(tableName, list, keyFieldNames);

            var dict = record as IDictionary<string, object>;
            IEnumerable<KeyValuePair<string, object>> criteria = GetCriteria(keyFieldNames, dict);
            SimpleExpression criteriaExpression = ExpressionHelper.CriteriaDictionaryToExpression(tableName, criteria);
            return dataStrategy.Run.Update(tableName, dict, criteriaExpression);
        }

        private static IEnumerable<KeyValuePair<string, object>> GetCriteria(IEnumerable<string> keyFieldNames,
                                                                             IDictionary<string, object> record)
        {
            var criteria = new Dictionary<string, object>();

            foreach (string keyFieldName in keyFieldNames)
            {
                string name = keyFieldName;
                KeyValuePair<string, object> keyValuePair =
                    record.SingleOrDefault(kvp => kvp.Key.Homogenize().Equals(name.Homogenize()));
                if (string.IsNullOrWhiteSpace(keyValuePair.Key))
                {
                    throw new InvalidOperationException("Key field value not set.");
                }

                criteria.Add(keyFieldName, keyValuePair.Value);
                record.Remove(keyValuePair);
            }
            return criteria;
        }
    }
}