using System;
using System.Dynamic;

namespace Simple.Data.Commands
{
    internal class QueryByCommand : ICommand
    {
        #region ICommand Members

        public bool IsCommandFor(string method)
        {
            return method.StartsWith("QueryBy") ||
                   method.StartsWith("query_by_", StringComparison.InvariantCultureIgnoreCase);
        }

        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            return CreateSimpleQuery(table, binder, args, dataStrategy);
        }

        #endregion

        private static object CreateSimpleQuery(DynamicTable table, InvokeMemberBinder binder, object[] args,
                                                DataStrategy dataStrategy)
        {
            SimpleExpression criteriaExpression =
                ExpressionHelper.CriteriaDictionaryToExpression(table.GetQualifiedName(),
                                                                MethodNameParser.ParseFromBinder(binder, args));
            return new SimpleQuery(dataStrategy, table.GetQualifiedName()).Where(criteriaExpression);
        }
    }
}