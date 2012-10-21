using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Simple.Data.Extensions;

namespace Simple.Data.Commands
{
    internal class UpdateAllCommand : ICommand
    {
        #region ICommand Members

        public bool IsCommandFor(string method)
        {
            return method.Equals("updateall", StringComparison.InvariantCultureIgnoreCase);
        }

        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            SimpleExpression criteria = args.OfType<SimpleExpression>().SingleOrDefault() ?? new SimpleEmptyExpression();

            IDictionary<string, object> data =
                binder.NamedArgumentsToDictionary(args).Where(kv => !(kv.Value is SimpleExpression)).ToDictionary();

            if (data.Count == 0)
                data = args.OfType<IDictionary<string, object>>().SingleOrDefault();

            if (data == null)
            {
                throw new SimpleDataException("Could not resolve data.");
            }

            int updatedCount = dataStrategy.Run.Update(table.GetQualifiedName(), data, criteria);

            return updatedCount.ResultSetFromModifiedRowCount();
        }

        #endregion
    }
}