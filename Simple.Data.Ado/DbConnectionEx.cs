﻿using System.Data;

namespace Simple.Data.Ado
{
    public static class DbConnectionEx
    {
        public static void OpenIfClosed(this IDbConnection connection)
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
        }

        public static IDbCommand CreateCommand(this IDbConnection connection, AdoOptions options)
        {
            if (options == null || options.CommandTimeout < 0) return connection.CreateCommand();

            IDbCommand command = connection.CreateCommand();
            command.CommandTimeout = options.CommandTimeout;
            return command;
        }
    }
}