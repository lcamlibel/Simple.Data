using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple.Data.QueryPolyfills
{
    internal class FunctionHandlers
    {
        private static readonly Dictionary<string, Func<IEnumerable<object>, object>> Funcs
            = new Dictionary<string, Func<IEnumerable<object>, object>>(StringComparer.OrdinalIgnoreCase)
                  {
                      {"min", o => o.Min()},
                      {"max", o => o.Max()},
                      {"sum", o => o.Aggregate(ObjectMaths.Add)},
                      {"avg", Average},
                      {"average", Average},
                  };

        public static bool Exists(string function)
        {
            return Funcs.ContainsKey(function);
        }

        public static Func<IEnumerable<object>, object> Get(string function)
        {
            return Funcs[function];
        }

        private static object Average(IEnumerable<object> source)
        {
            List<object> list = source.ToList();
            if (list.Count == 0) return 0;
            object total = list.Aggregate(ObjectMaths.Add);
            return ObjectMaths.Divide(total, list.Count);
        }
    }
}