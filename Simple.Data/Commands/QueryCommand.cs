using System;
using System.Dynamic;

namespace Simple.Data.Commands
{
    internal class QueryCommand : ICommand, ICreateDelegate
    {
        #region ICommand Members

        public bool IsCommandFor(string method)
        {
            return method.Equals("query", StringComparison.InvariantCultureIgnoreCase);
        }

        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            return new SimpleQuery(dataStrategy, table.GetQualifiedName());
        }

        #endregion

        #region ICreateDelegate Members

        public Func<object[], object> CreateDelegate(DataStrategy dataStrategy, DynamicTable table,
                                                     InvokeMemberBinder binder, object[] args)
        {
            return a => new SimpleQuery(dataStrategy, table.GetQualifiedName());
        }

        #endregion

        public object Execute(DataStrategy dataStrategy, SimpleQuery query, InvokeMemberBinder binder, object[] args)
        {
            throw new NotImplementedException();
        }
    }
}