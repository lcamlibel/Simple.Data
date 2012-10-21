using System.ComponentModel.Composition;
using System.Data;
using System.Text.RegularExpressions;
using Simple.Data.Ado;

namespace Simple.Data.SqlServer
{
    [Export(typeof (CommandOptimizer))]
    public class SqlCommandOptimizer : CommandOptimizer
    {
        public override IDbCommand OptimizeFindOne(IDbCommand command)
        {
            command.CommandText = Regex.Replace(command.CommandText, "^SELECT ", "SELECT TOP 1 ",
                                                RegexOptions.IgnoreCase);
            return command;
        }
    }
}