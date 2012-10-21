﻿using System.Collections.Generic;
using System.Linq;
using Simple.Data.Extensions;

namespace Simple.Data.QueryPolyfills
{
    internal class GroupingHandler
    {
        private readonly HashSet<string> _groupColumns;

        public GroupingHandler(params string[] groupColumns)
        {
            _groupColumns = new HashSet<string>(groupColumns);
        }

        public IEnumerable<IGrouping<IDictionary<string, object>, IDictionary<string, object>>> Group(
            IEnumerable<IDictionary<string, object>> source)
        {
            return source.GroupBy(d => d.Where(kvp => _groupColumns.Contains(kvp.Key)).ToDictionary(),
                                  d => d.Where(kvp => !_groupColumns.Contains(kvp.Key)).ToDictionary(),
                                  new DictionaryEqualityComparer());
        }
    }
}