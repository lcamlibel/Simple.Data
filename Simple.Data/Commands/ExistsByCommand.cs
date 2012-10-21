﻿using System;
using System.Dynamic;

namespace Simple.Data.Commands
{
    internal class ExistsByCommand : ICommand
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
            return method.StartsWith("existsby", StringComparison.InvariantCultureIgnoreCase)
                   || method.StartsWith("exists_by", StringComparison.InvariantCultureIgnoreCase)
                   || method.StartsWith("anyby", StringComparison.InvariantCultureIgnoreCase)
                   || method.StartsWith("any_by", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="dataStrategy">The data strategy.</param>
        /// <param name="table"></param>
        /// <param name="binder">The binder from the <see cref="DynamicTable"/> method invocation.</param>
        /// <param name="args">The arguments from the <see cref="DynamicTable"/> method invocation.</param>
        /// <returns></returns>
        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            SimpleExpression criteria = ExpressionHelper.CriteriaDictionaryToExpression(table.GetQualifiedName(),
                                                                                        MethodNameParser.ParseFromBinder
                                                                                            (binder, args));
            return new SimpleQuery(dataStrategy, table.GetQualifiedName()).Where(criteria).Exists();
        }

        #endregion
    }
}