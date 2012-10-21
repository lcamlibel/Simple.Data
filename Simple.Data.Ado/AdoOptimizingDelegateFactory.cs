﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Simple.Data.Ado
{
    [Export(typeof (OptimizingDelegateFactory))]
    public class AdoOptimizingDelegateFactory : OptimizingDelegateFactory
    {
        public override Func<object[], IDictionary<string, object>> CreateFindOneDelegate(Adapter adapter,
                                                                                          string tableName,
                                                                                          SimpleExpression criteria)
        {
            return new AdoAdapterFinder((AdoAdapter) adapter).CreateFindOneDelegate(tableName, criteria);
        }

        public override Func<object[], IDictionary<string, object>> CreateGetDelegate(Adapter adapter, string tableName,
                                                                                      object[] keyValues)
        {
            return new AdoAdapterGetter((AdoAdapter) adapter).CreateGetDelegate(tableName, keyValues);
        }
    }
}