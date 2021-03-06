﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Simple.Data.Extensions;

namespace Simple.Data.Commands
{
    internal class InsertCommand : ICommand
    {
        #region ICommand Members

        public bool IsCommandFor(string method)
        {
            return method.Equals("insert", StringComparison.InvariantCultureIgnoreCase);
        }

        public object Execute(DataStrategy dataStrategy, DynamicTable table, InvokeMemberBinder binder, object[] args)
        {
            object result = DoInsert(binder, args, dataStrategy, table.GetQualifiedName());

            return ResultHelper.TypeResult(result, table, dataStrategy);
        }

        #endregion

        private static object DoInsert(InvokeMemberBinder binder, object[] args, DataStrategy dataStrategy,
                                       string tableName)
        {
            if (binder.HasSingleUnnamedArgument())
            {
                return InsertEntity(args[0], dataStrategy, tableName, (r, e) => false, !binder.IsResultDiscarded());
            }

            if (args.Length == 2)
            {
                var onError = args[1] as ErrorCallback;
                if (onError != null)
                {
                    return InsertEntity(args[0], dataStrategy, tableName, onError, !binder.IsResultDiscarded());
                }
            }
            return InsertDictionary(binder, args, dataStrategy, tableName);
        }

        private static object InsertDictionary(InvokeMemberBinder binder, IEnumerable<object> args,
                                               DataStrategy dataStrategy, string tableName)
        {
            return dataStrategy.Run.Insert(tableName, binder.NamedArgumentsToDictionary(args),
                                           !binder.IsResultDiscarded());
        }

        private static object InsertEntity(object entity, DataStrategy dataStrategy, string tableName,
                                           ErrorCallback onError, bool resultRequired)
        {
            var dictionary = entity as IDictionary<string, object>;
            if (dictionary != null)
                return dataStrategy.Run.Insert(tableName, dictionary, resultRequired);

            var list = entity as IEnumerable<IDictionary<string, object>>;
            if (list != null)
                return dataStrategy.Run.InsertMany(tableName, list, onError, resultRequired);

            var entityList = entity as IEnumerable;
            if (entityList != null)
            {
                object[] array = entityList.Cast<object>().ToArray();
                var rows = new List<IDictionary<string, object>>();
                for (int index = 0; index < array.Length; index++)
                {
                    object o = array[index];
                    dictionary = (o as IDictionary<string, object>) ?? o.ObjectToDictionary();
                    if (dictionary.Count == 0)
                    {
                        throw new SimpleDataException("Could not discover data in object.");
                    }
                    rows.Add(dictionary);
                }

                return dataStrategy.Run.InsertMany(tableName, rows, onError, resultRequired);
            }

            dictionary = entity.ObjectToDictionary();
            if (dictionary.Count == 0)
                throw new SimpleDataException("Could not discover data in object.");
            return dataStrategy.Run.Insert(tableName, dictionary, resultRequired);
        }
    }
}