namespace Simple.Data
{
    public class ForUpdateClause : SimpleQueryClauseBase
    {
        public ForUpdateClause(bool skipLockedRows)
        {
            SkipLockedRows = skipLockedRows;
        }

        public bool SkipLockedRows { get; private set; }
    }
}