using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Simple.Data.Extensions;

namespace Simple.Data.Commands
{
    internal class UpsertByCommand : ICommand
    {
        #region ICommand Members

        public bool IsCommandFor(string method)
        {
            return method.Homogenize().StartsWith("upsertby", StringComparison.InvariantCultureIgnoreCase);
        }

        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            object result;

            if (binder.HasSingleUnnamedArgument() || args.Length == 2 && args[1] is ErrorCallback)
            {
                result = UpsertByKeyFields(table.GetQualifiedName(), dataStrategy, args[0],
                                           MethodNameParser.ParseCriteriaNamesFromMethodName(binder.Name),
                                           !binder.IsResultDiscarded(),
                                           args.Length == 2 ? (ErrorCallback) args[1] : ((item, exception) => false));
            }
            else
            {
                IDictionary<string, object> criteria = MethodNameParser.ParseFromBinder(binder, args);
                SimpleExpression criteriaExpression =
                    ExpressionHelper.CriteriaDictionaryToExpression(table.GetQualifiedName(),
                                                                    criteria);
                IDictionary<string, object> data = binder.NamedArgumentsToDictionary(args);
                result = dataStrategy.Run.Upsert(table.GetQualifiedName(), data, criteriaExpression,
                                                 !binder.IsResultDiscarded());
            }

            return ResultHelper.TypeResult(result, table, dataStrategy);
        }

        #endregion

        internal static object UpsertByKeyFields(string tableName, DataStrategy dataStrategy, object entity,
                                                 IEnumerable<string> keyFieldNames, bool isResultRequired,
                                                 ErrorCallback errorCallback)
        {
            object record = UpdateCommand.ObjectToDictionary(entity);
            var list = record as IList<IDictionary<string, object>>;
            if (list != null)
                return dataStrategy.Run.UpsertMany(tableName, list, keyFieldNames, isResultRequired, errorCallback);

            var dict = record as IDictionary<string, object>;
            IEnumerable<KeyValuePair<string, object>> criteria = GetCriteria(keyFieldNames, dict);
            SimpleExpression criteriaExpression = ExpressionHelper.CriteriaDictionaryToExpression(tableName, criteria);
            return dataStrategy.Run.Upsert(tableName, dict, criteriaExpression, isResultRequired);
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
            }
            return criteria;
        }
    }
}