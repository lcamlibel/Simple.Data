using System.Collections.Generic;

namespace Simple.Data
{
    public interface IRange
    {
        object Start { get; }
        object End { get; }
        IEnumerable<object> AsEnumerable();
    }
}