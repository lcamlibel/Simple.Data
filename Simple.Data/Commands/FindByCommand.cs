using System;
using System.Collections.Generic;
using System.Dynamic;
using Simple.Data.Extensions;

namespace Simple.Data.Commands
{
    internal class FindByCommand : ICommand, ICreateDelegate, IQueryCompatibleCommand
    {
        #region ICommand Members

        public bool IsCommandFor(string method)
        {
            return method.StartsWith("FindBy") ||
                   method.StartsWith("find_by_", StringComparison.InvariantCultureIgnoreCase);
        }

        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            SimpleExpression criteriaExpression =
                ExpressionHelper.CriteriaDictionaryToExpression(table.GetQualifiedName(),
                                                                CreateCriteriaDictionary(binder,
                                                                                         args));
            IDictionary<string, object> data = dataStrategy.Run.FindOne(table.GetQualifiedName(), criteriaExpression);
            return data != null ? new SimpleRecord(data, table.GetQualifiedName(), dataStrategy) : null;
        }

        #endregion

        #region ICreateDelegate Members

        public Func<object[], object> CreateDelegate(DataStrategy dataStrategy, DynamicTable table,
                                                     InvokeMemberBinder binder, object[] args)
        {
            if (dataStrategy is SimpleTransaction) return null;

            if (binder.Name.Equals("FindBy") || binder.Name.Equals("find_by"))
            {
                ArgumentHelper.CheckFindArgs(args, binder);
                if (args.Length == 1 && args[0].IsAnonymous()) return null;
            }

            IEnumerable<KeyValuePair<string, object>> criteriaDictionary =
                ArgumentHelper.CreateCriteriaDictionary(binder, args, "FindBy", "find_by");
            if (criteriaDictionary == null) return null;

            SimpleExpression criteriaExpression =
                ExpressionHelper.CriteriaDictionaryToExpression(table.GetQualifiedName(), criteriaDictionary);
            try
            {
                Adapter adapter = dataStrategy.GetAdapter();
                Func<object[], IDictionary<string, object>> func =
                    adapter.OptimizingDelegateFactory.CreateFindOneDelegate(adapter, table.GetQualifiedName(),
                                                                            criteriaExpression);
                return a =>
                           {
                               IDictionary<string, object> data = func(a);
                               return (data != null && data.Count > 0)
                                          ? new SimpleRecord(data, table.GetQualifiedName(), dataStrategy)
                                          : null;
                           };
            }
            catch (NotImplementedException)
            {
                return null;
            }
        }

        #endregion

        #region IQueryCompatibleCommand Members

        public object Execute(DataStrategy dataStrategy, SimpleQuery query, InvokeMemberBinder binder, object[] args)
        {
            SimpleExpression criteriaExpression = ExpressionHelper.CriteriaDictionaryToExpression(query.TableName,
                                                                                                  CreateCriteriaDictionary
                                                                                                      (binder,
                                                                                                       args));
            query = query.Where(criteriaExpression).Take(1);
            return query.FirstOrDefault();
        }

        #endregion

        private static IEnumerable<KeyValuePair<string, object>> CreateCriteriaDictionary(InvokeMemberBinder binder,
                                                                                          IList<object> args)
        {
            IDictionary<string, object> criteriaDictionary = null;
            if (binder.Name.Equals("FindBy") || binder.Name.Equals("find_by"))
            {
                if (args.Count == 0) throw new ArgumentException("FindBy requires arguments.");
                if (binder.CallInfo.ArgumentNames != null && binder.CallInfo.ArgumentNames.Count > 0)
                {
                    criteriaDictionary = binder.NamedArgumentsToDictionary(args);
                }
                else if (args.Count == 1)
                {
                    if (ReferenceEquals(args[0], null))
                        throw new ArgumentException("FindBy does not accept unnamed null argument.");
                    criteriaDictionary = args[0].ObjectToDictionary();
                }
            }
            else
            {
                criteriaDictionary = MethodNameParser.ParseFromBinder(binder, args);
            }
            return criteriaDictionary;
        }
    }
}