using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Simple.Data.Commands
{
    public class GetCommand : ICommand, ICreateDelegate, IQueryCompatibleCommand
    {
        #region ICommand Members

        public bool IsCommandFor(string method)
        {
            return method.Equals("get", StringComparison.OrdinalIgnoreCase) ||
                   method.Equals("getscalar", StringComparison.OrdinalIgnoreCase);
        }

        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            IDictionary<string, object> result = dataStrategy.Run.Get(table.GetName(), args);
            if (result == null || result.Count == 0) return null;
            return binder.Name.Equals("get", StringComparison.OrdinalIgnoreCase)
                       ? new SimpleRecord(result, table.GetQualifiedName(), dataStrategy)
                       : result.First().Value;
        }

        #endregion

        #region ICreateDelegate Members

        public Func<object[], object> CreateDelegate(DataStrategy dataStrategy, DynamicTable table,
                                                     InvokeMemberBinder binder, object[] args)
        {
            if (dataStrategy is SimpleTransaction) return null;

            Func<object[], IDictionary<string, object>> func =
                dataStrategy.GetAdapter().OptimizingDelegateFactory.CreateGetDelegate(dataStrategy.GetAdapter(),
                                                                                      table.GetQualifiedName(), args);
            return a =>
                       {
                           IDictionary<string, object> data = func(a);
                           return (data != null && data.Count > 0)
                                      ? new SimpleRecord(data, table.GetQualifiedName(), dataStrategy)
                                      : null;
                       };
        }

        #endregion

        #region IQueryCompatibleCommand Members

        public object Execute(DataStrategy dataStrategy, SimpleQuery query, InvokeMemberBinder binder, object[] args)
        {
            IList<string> keyNames = dataStrategy.GetAdapter().GetKeyNames(query.TableName);
            IEnumerable<KeyValuePair<string, object>> dict =
                keyNames.Select((k, i) => new KeyValuePair<string, object>(k, args[i]));
            query = query.Where(ExpressionHelper.CriteriaDictionaryToExpression(query.TableName, dict)).Take(1);
            dynamic result = query.FirstOrDefault();
            if (result == null) return null;
            return binder.Name.Equals("get", StringComparison.OrdinalIgnoreCase)
                       ? result
                       : ((IDictionary<string, object>) result).First().Value;
        }

        #endregion
    }
}