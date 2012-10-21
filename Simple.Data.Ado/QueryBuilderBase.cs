using System;
using System.Collections.Generic;
using System.Linq;
using Simple.Data.Ado.Schema;

namespace Simple.Data.Ado
{
    public abstract class QueryBuilderBase
    {
        protected readonly AdoAdapter AdoAdapter;
        protected readonly int BulkIndex;
        protected readonly DatabaseSchema Schema;
        protected readonly SimpleReferenceFormatter SimpleReferenceFormatter;

        protected IList<SimpleReference> Columns;
        protected CommandBuilder CommandBuilder;
        protected SimpleExpression HavingCriteria;
        protected SimpleQuery Query;
        protected Table Table;
        protected ObjectName TableName;
        protected SimpleExpression WhereCriteria;

        protected QueryBuilderBase(AdoAdapter adapter) : this(adapter, -1)
        {
        }

        protected QueryBuilderBase(AdoAdapter adapter, int bulkIndex) : this(adapter, bulkIndex, null)
        {
        }

        protected QueryBuilderBase(AdoAdapter adapter, int bulkIndex, IFunctionNameConverter functionNameConverter)
        {
            AdoAdapter = adapter;
            BulkIndex = bulkIndex;
            Schema = AdoAdapter.GetSchema();
            CommandBuilder = new CommandBuilder(Schema, BulkIndex);
            SimpleReferenceFormatter = new SimpleReferenceFormatter(Schema, CommandBuilder, functionNameConverter);
        }

        public abstract ICommandBuilder Build(SimpleQuery query, out IEnumerable<SimpleQueryClauseBase> unhandledClauses);

        protected virtual void SetQueryContext(SimpleQuery query)
        {
            Query = query;
            TableName = Schema.BuildObjectName(query.TableName);
            Table = Schema.FindTable(TableName);
            SelectClause selectClause = Query.Clauses.OfType<SelectClause>().SingleOrDefault();
            if (selectClause != null)
            {
                Columns = selectClause.Columns.OfType<AllColumnsSpecialReference>().Any() ? ExpandAllColumnsReferences(selectClause.Columns).ToArray() : selectClause.Columns.ToArray();
            }
            else
            {
                Columns =
                    Table.Columns.Select(
                        c => ObjectReference.FromStrings(Table.Schema, Table.ActualName, c.ActualName)).ToArray();
            }

            HandleWithClauses();

            WhereCriteria = Query.Clauses.OfType<WhereClause>().Aggregate(SimpleExpression.Empty,
                                                                            (seed, where) => seed && where.Criteria);
            HavingCriteria = Query.Clauses.OfType<HavingClause>().Aggregate(SimpleExpression.Empty,
                                                                              (seed, having) => seed && having.Criteria);

            CommandBuilder.SetText(GetSelectClause(TableName));
        }

        protected IEnumerable<SimpleReference> ExpandAllColumnsReferences(IEnumerable<SimpleReference> columns)
        {
            foreach (SimpleReference column in columns)
            {
                var allColumns = column as AllColumnsSpecialReference;
                if (ReferenceEquals(allColumns, null)) yield return column;
                else
                {
                    foreach (Column allColumn in Schema.FindTable(allColumns.Table.GetName()).Columns)
                    {
                        yield return new ObjectReference(allColumn.ActualName, allColumns.Table);
                    }
                }
            }
        }

        protected virtual void HandleWithClauses()
        {
            List<WithClause> withClauses = Query.Clauses.OfType<WithClause>().ToList();
            var relationTypeDict = new Dictionary<ObjectReference, RelationType>();
            if (withClauses.Count > 0)
            {
                for (int index = 0; index < withClauses.Count; index++)
                {
                    WithClause withClause = withClauses[index];
                    if (withClause.ObjectReference.GetOwner().IsNull())
                    {
                        HandleWithClauseUsingAssociatedJoinClause(relationTypeDict, withClause);
                    }
                    else
                    {
                        if (withClause.Type == WithType.NotSpecified)
                        {
                            InferWithType(withClause);
                        }
                        HandleWithClauseUsingNaturalJoin(withClause, relationTypeDict);
                    }
                }
                Columns =
                    Columns.OfType<ObjectReference>()
                        .Select(c => IsCoreTable(c.GetOwner()) ? c : AddWithAlias(c, relationTypeDict[c.GetOwner()]))
                        .ToArray();
            }
        }

        protected void InferWithType(WithClause withClause)
        {
            ObjectReference objectReference = withClause.ObjectReference;
            while (!ReferenceEquals(objectReference.GetOwner(), null))
            {
                Table toTable = Schema.FindTable(objectReference.GetName());
                Table fromTable = Schema.FindTable(objectReference.GetOwner().GetName());
                if (Schema.GetRelationType(fromTable.ActualName, toTable.ActualName) == RelationType.OneToMany)
                {
                    withClause.Type = WithType.Many;
                    return;
                }
                objectReference = objectReference.GetOwner();
            }
        }

        protected void HandleWithClauseUsingAssociatedJoinClause(
            Dictionary<ObjectReference, RelationType> relationTypeDict, WithClause withClause)
        {
            JoinClause joinClause =
                Query.Clauses.OfType<JoinClause>().FirstOrDefault(
                    j => j.Table.GetAliasOrName() == withClause.ObjectReference.GetAliasOrName());
            if (joinClause != null)
            {
                Columns =
                    Columns.Concat(
                        Schema.FindTable(joinClause.Table.GetName()).Columns.Select(
                            c => new ObjectReference(c.ActualName, joinClause.Table)))
                        .ToArray();
                relationTypeDict[joinClause.Table] = WithTypeToRelationType(withClause.Type, RelationType.OneToMany);
            }
        }

        protected void HandleWithClauseUsingNaturalJoin(WithClause withClause,
                                                        Dictionary<ObjectReference, RelationType> relationTypeDict)
        {
            relationTypeDict[withClause.ObjectReference] = WithTypeToRelationType(withClause.Type, RelationType.None);
            Columns =
                Columns.Concat(
                    Schema.FindTable(withClause.ObjectReference.GetName()).Columns.Select(
                        c => new ObjectReference(c.ActualName, withClause.ObjectReference)))
                    .ToArray();
        }

        protected static RelationType WithTypeToRelationType(WithType withType, RelationType defaultRelationType)
        {
            switch (withType)
            {
                case WithType.One:
                    return RelationType.ManyToOne;
                case WithType.Many:
                    return RelationType.OneToMany;
                default:
                    return defaultRelationType;
            }
        }

        protected bool IsCoreTable(ObjectReference tableReference)
        {
            if (ReferenceEquals(tableReference, null)) throw new ArgumentNullException("tableReference");
            if (!string.IsNullOrWhiteSpace(tableReference.GetAlias())) return false;
            return Schema.FindTable(tableReference.GetName()) == Table;
        }

        protected ObjectReference AddWithAlias(ObjectReference c, RelationType relationType = RelationType.None)
        {
            if (relationType == RelationType.None)
                relationType = Schema.GetRelationType(c.GetOwner().GetOwner().GetName(), c.GetOwner().GetName());
            if (relationType == RelationType.None) throw new InvalidOperationException("No Join found");
            return c.As(string.Format("__with{0}__{1}__{2}",
                                      relationType == RelationType.OneToMany
                                          ? "n"
                                          : "1", c.GetOwner().GetAliasOrName(), c.GetName()));
        }

        protected virtual void HandleJoins()
        {
            if (WhereCriteria == SimpleExpression.Empty && HavingCriteria == SimpleExpression.Empty
                && (!Query.Clauses.OfType<JoinClause>().Any())
                && (Columns.All(r => (r is CountSpecialReference)))) return;

            var joiner = new Joiner(JoinType.Inner, Schema);

            string dottedTables = RemoveSchemaFromQueryTableName();

            IEnumerable<string> fromTable = dottedTables.Contains('.')
                                                ? joiner.GetJoinClauses(TableName, dottedTables.Split('.').Reverse())
                                                : Enumerable.Empty<string>();

            JoinClause[] joinClauses = Query.Clauses.OfType<JoinClause>().ToArray();
            IEnumerable<string> fromJoins = joiner.GetJoinClauses(joinClauses, CommandBuilder);

            IEnumerable<string> fromCriteria = joiner.GetJoinClauses(TableName, WhereCriteria);

            IEnumerable<string> fromHavingCriteria = joiner.GetJoinClauses(TableName, HavingCriteria);

            IEnumerable<string> fromColumnList = Columns.Any(r => !(r is SpecialReference))
                                                     ? GetJoinClausesFromColumnList(joinClauses, joiner)
                                                     : Enumerable.Empty<string>();

            List<string> joinList =
                fromTable.Concat(fromJoins).Concat(fromCriteria).Concat(fromHavingCriteria).Concat(fromColumnList).
                    Select(s => s.Trim()).Distinct().ToList();

            List<string> leftJoinList =
                joinList.Where(s => s.StartsWith("LEFT ", StringComparison.OrdinalIgnoreCase)).ToList();

            for (int index = 0; index < leftJoinList.Count; index++)
            {
                string leftJoin = leftJoinList[index];
                if (joinList.Any(s => s.Equals(leftJoin.Substring(5), StringComparison.OrdinalIgnoreCase)))
                {
                    joinList.Remove(leftJoin);
                }
            }

            string joins = string.Join(" ", joinList);

            if (!string.IsNullOrWhiteSpace(joins))
            {
                CommandBuilder.Append(" " + joins);
            }
        }

        protected IEnumerable<string>
            GetJoinClausesFromColumnList(IEnumerable<JoinClause> joinClauses, Joiner joiner)
        {
            return joiner.GetJoinClauses(TableName, GetObjectReferences(Columns)
                                                         .Where(
                                                             o =>
                                                             !joinClauses.Any(j => ObjectReferenceIsInJoinClause(j, o))),
                                         JoinType.Outer);
        }

        protected static bool ObjectReferenceIsInJoinClause(JoinClause clause, ObjectReference reference)
        {
            return reference.GetOwner().GetAliasOrName().Equals(clause.Table.GetAliasOrName());
        }

        protected IEnumerable<ObjectReference> GetObjectReferences(IEnumerable<SimpleReference> source)
        {
            List<SimpleReference> list = source.ToList();
            foreach (ObjectReference objectReference in list.OfType<ObjectReference>())
            {
                yield return objectReference;
            }

            foreach (
                ObjectReference objectReference in
                    list.OfType<FunctionReference>().Select(fr => fr.Argument).OfType<ObjectReference>())
            {
                yield return objectReference;
            }
        }

        protected string RemoveSchemaFromQueryTableName()
        {
            return Query.TableName.StartsWith(Table.Schema + '.', StringComparison.InvariantCultureIgnoreCase)
                       ? Query.TableName.Substring(Query.TableName.IndexOf('.') + 1)
                       : Query.TableName;
        }

        protected virtual void HandleQueryCriteria()
        {
            if (WhereCriteria == SimpleExpression.Empty) return;
            CommandBuilder.Append(" WHERE " + new ExpressionFormatter(CommandBuilder, Schema).Format(WhereCriteria));
        }

        protected virtual void HandleHavingCriteria()
        {
            if (HavingCriteria == SimpleExpression.Empty) return;
            CommandBuilder.Append(" HAVING " +
                                   new ExpressionFormatter(CommandBuilder, Schema).Format(HavingCriteria));
        }

        protected virtual void HandleGrouping()
        {
            if (HavingCriteria == SimpleExpression.Empty &&
                !Columns.OfType<FunctionReference>().Any(f => f.IsAggregate)) return;

            List<SimpleReference> groupColumns =
                GetColumnsToSelect(Table).Where(
                    c => (!(c is FunctionReference)) || !((FunctionReference) c).IsAggregate).ToList();

            if (groupColumns.Count == 0) return;

            CommandBuilder.Append(" GROUP BY " +
                                   string.Join(",",
                                               groupColumns.Select(
                                                   SimpleReferenceFormatter.FormatColumnClauseWithoutAlias)));
        }

        protected virtual void HandleOrderBy()
        {
            if (!Query.Clauses.OfType<OrderByClause>().Any()) return;

            IEnumerable<string> orderNames = Query.Clauses.OfType<OrderByClause>().Select(ToOrderByDirective);
            CommandBuilder.Append(" ORDER BY " + string.Join(", ", orderNames));
        }

        protected string ToOrderByDirective(OrderByClause item)
        {
            string name;
            if (!string.IsNullOrWhiteSpace(item.Reference.GetOwner().GetAlias()))
            {
                name = string.Format("{0}.{1}", Schema.QuoteObjectName(item.Reference.GetOwner().GetAlias()),
                                     Schema.QuoteObjectName(item.Reference.GetName()));
            }
            else if (
                Columns.Any(
                    r => (!string.IsNullOrWhiteSpace(r.GetAlias())) && r.GetAlias().Equals(item.Reference.GetName())))
            {
                name = item.Reference.GetName();
            }
            else
            {
                Table table = Schema.FindTable(item.Reference.GetOwner().GetName());
                name = table.FindColumn(item.Reference.GetName()).QualifiedName;
            }

            string direction = item.Direction == OrderByDirection.Descending ? " DESC" : string.Empty;
            return name + direction;
        }

        protected virtual string GetSelectClause(ObjectName tableName)
        {
            Table table = Schema.FindTable(tableName);
            string template = Query.Clauses.OfType<DistinctClause>().Any()
                                  ? "select distinct {0} from {1}"
                                  : "select {0} from {1}";
            return string.Format(template,
                                 GetColumnsClause(table),
                                 table.QualifiedName);
        }

        protected virtual string GetColumnsClause(Table table)
        {
            if (Columns != null && Columns.Count == 1 && Columns[0] is SpecialReference)
            {
                return FormatSpecialReference((SpecialReference) Columns[0]);
            }

            return string.Join(",", GetColumnsToSelect(table).Select(SimpleReferenceFormatter.FormatColumnClause));
        }

        protected static string FormatSpecialReference(SpecialReference reference)
        {
            if (reference.GetType() == typeof (CountSpecialReference)) return "COUNT(*)";
            if (reference.GetType() == typeof (ExistsSpecialReference)) return "DISTINCT 1";
            throw new InvalidOperationException("SpecialReference type not recognised.");
        }

        protected IEnumerable<SimpleReference> GetColumnsToSelect(Table table)
        {
            if (Columns != null && Columns.Count > 0)
            {
                return Columns;
            }
            return
                table.Columns.Select(c => ObjectReference.FromStrings(table.Schema, table.ActualName, c.ActualName));
        }

        protected string FormatGroupByColumnClause(SimpleReference reference)
        {
            var objectReference = reference as ObjectReference;
            if (!ReferenceEquals(objectReference, null))
            {
                Table table = Schema.FindTable(objectReference.GetOwner().GetName());
                Column column = table.FindColumn(objectReference.GetName());
                return string.Format("{0}.{1}", table.QualifiedName, column.QuotedName);
            }

            var functionReference = reference as FunctionReference;
            if (!ReferenceEquals(functionReference, null))
            {
                return FormatGroupByColumnClause(functionReference.Argument);
            }

            throw new InvalidOperationException("SimpleReference type not supported.");
        }
    }
}