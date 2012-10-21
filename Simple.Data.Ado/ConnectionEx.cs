using System;
using System.Data;

namespace Simple.Data.Ado
{
    public static class ConnectionEx
    {
        public static IDisposable MaybeDisposable(this IDbConnection connection)
        {
            if (connection == null || connection.State == ConnectionState.Open) return ActionDisposable.NoOp;
            return new ActionDisposable(connection.Dispose);
        }
    }
}