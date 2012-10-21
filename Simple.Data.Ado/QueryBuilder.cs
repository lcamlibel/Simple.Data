using System.Collections.Generic;

namespace Simple.Data.Ado
{
    public class QueryBuilder : QueryBuilderBase
    {
        private List<SimpleQueryClauseBase> _unhandledClauses;

        public QueryBuilder(AdoAdapter adoAdapter)
            : base(adoAdapter)
        {
        }

        public QueryBuilder(AdoAdapter adoAdapter, int bulkIndex) : base(adoAdapter, bulkIndex)
        {
        }

        public override ICommandBuilder Build(SimpleQuery query, out IEnumerable<SimpleQueryClauseBase> unhandledClauses)
        {
            var customBuilder =
                AdoAdapter.ProviderHelper.GetCustomProvider<ICustomQueryBuilder>(AdoAdapter.ConnectionProvider);
            if (customBuilder != null)
            {
                return customBuilder.Build(AdoAdapter, BulkIndex, query, out unhandledClauses);
            }

            _unhandledClauses = new List<SimpleQueryClauseBase>();
            SetQueryContext(query);

            HandleJoins();
            HandleQueryCriteria();
            HandleGrouping();
            HandleHavingCriteria();
            HandleOrderBy();

            unhandledClauses = _unhandledClauses;
            return CommandBuilder;
        }
    }
}