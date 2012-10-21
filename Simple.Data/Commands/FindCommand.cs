using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Simple.Data.Commands
{
    internal class FindCommand : ICommand, IQueryCompatibleCommand
    {
        #region ICommand Members

        /// <summary>
        /// Determines whether the instance is able to handle the specified method.
        /// </summary>
        /// <param name="method">The method name.</param>
        /// <returns>
        /// 	<c>true</c> if the instance is able to handle the specified method; otherwise, <c>false</c>.
        /// </returns>
        public bool IsCommandFor(string method)
        {
            return method.Equals("find", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="dataStrategy">The database (or transaction)</param>
        /// <param name="table"></param>
        /// <param name="binder">The binder from the <see cref="DynamicTable"/> method invocation.</param>
        /// <param name="args">The arguments from the <see cref="DynamicTable"/> method invocation.</param>
        /// <returns></returns>
        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            if (args.Length == 1 && args[0] is SimpleExpression)
            {
                IDictionary<string, object> data = dataStrategy.Run.FindOne(table.GetQualifiedName(),
                                                                            (SimpleExpression) args[0]);
                return data != null ? new SimpleRecord(data, table.GetQualifiedName(), dataStrategy) : null;
            }

            throw new BadExpressionException("Find only accepts a criteria expression.");
        }

        #endregion

        #region IQueryCompatibleCommand Members

        public object Execute(DataStrategy dataStrategy, SimpleQuery query, InvokeMemberBinder binder, object[] args)
        {
            return query.Where((SimpleExpression) args[0]).Take(1).FirstOrDefault();
        }

        #endregion
    }
}