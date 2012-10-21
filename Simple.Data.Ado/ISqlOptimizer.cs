using System.Data;

namespace Simple.Data.Ado
{
    public class CommandOptimizer
    {
        public virtual IDbCommand OptimizeFindOne(IDbCommand command)
        {
            return command;
        }
    }
}