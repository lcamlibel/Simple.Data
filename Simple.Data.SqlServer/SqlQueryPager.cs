using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Simple.Data.Ado;

namespace Simple.Data.SqlServer
{
    [Export(typeof (IQueryPager))]
    public class SqlQueryPager : IQueryPager
    {
        private static readonly Regex ColumnExtract = new Regex(@"SELECT\s*(.*)\s*(FROM.*)",
                                                                RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private static readonly Regex SelectMatch = new Regex(@"^SELECT\s*", RegexOptions.IgnoreCase);

        #region IQueryPager Members

        public IEnumerable<string> ApplyLimit(string sql, int take)
        {
            yield return SelectMatch.Replace(sql, match => match.Value + " TOP " + take + " ");
        }

        public IEnumerable<string> ApplyPaging(string sql, int skip, int take)
        {
            var builder = new StringBuilder("WITH __Data AS (SELECT ");

            Match match = ColumnExtract.Match(sql);
            string columns = match.Groups[1].Value.Trim();
            string fromEtc = match.Groups[2].Value.Trim();

            builder.Append(columns);

            string orderBy = ExtractOrderBy(columns, ref fromEtc);

            builder.AppendFormat(", ROW_NUMBER() OVER({0}) AS [_#_]", orderBy);
            builder.AppendLine();
            builder.Append(fromEtc);
            builder.AppendLine(")");
            builder.AppendFormat("SELECT {0} FROM __Data WHERE [_#_] BETWEEN {1} AND {2}", DequalifyColumns(columns),
                                 skip + 1, skip + take);

            yield return builder.ToString();
        }

        #endregion

        private static string DequalifyColumns(string original)
        {
            IEnumerable<string> q = from part in original.Split(',')
                                    select part.Substring(Math.Max(part.LastIndexOf('.') + 1, part.LastIndexOf('[')));
            return string.Join(",", q);
        }

        private static string ExtractOrderBy(string columns, ref string fromEtc)
        {
            string orderBy;
            int index = fromEtc.IndexOf("ORDER BY", StringComparison.InvariantCultureIgnoreCase);
            if (index > -1)
            {
                orderBy = fromEtc.Substring(index).Trim();
                fromEtc = fromEtc.Remove(index).Trim();
            }
            else
            {
                orderBy = "ORDER BY " + columns.Split(',').First().Trim();

                int aliasIndex = orderBy.IndexOf(" AS [", StringComparison.InvariantCultureIgnoreCase);

                if (aliasIndex > -1)
                {
                    orderBy = orderBy.Substring(0, aliasIndex);
                }
            }
            return orderBy;
        }
    }
}