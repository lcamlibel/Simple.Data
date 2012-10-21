using System;

namespace Simple.Data
{
    public class WithCountClause : SimpleQueryClauseBase
    {
        private readonly Action<int> _setCount;

        public WithCountClause(Action<int> setCount)
        {
            _setCount = setCount;
        }

        public void SetCount(int count)
        {
            _setCount(count);
        }
    }
}