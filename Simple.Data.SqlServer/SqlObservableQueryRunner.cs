using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.SqlClient;
using Simple.Data.Ado;

namespace Simple.Data.SqlServer
{
    [Export(typeof (IObservableQueryRunner))]
    public class SqlObservableQueryRunner : IObservableQueryRunner
    {
        #region IObservableQueryRunner Members

        public IObservable<IDictionary<string, object>> Run(IDbCommand command, IDbConnection connection,
                                                            IDictionary<string, int> index)
        {
            return new SqlObservable(connection as SqlConnection, command as SqlCommand, index);
        }

        #endregion

        #region Nested type: SqlObservable

        private class SqlObservable : IObservable<IDictionary<string, object>>
        {
            private readonly SqlCommand _command;
            private readonly SqlConnection _connection;
            private IDictionary<string, int> _index;

            public SqlObservable(SqlConnection connection, SqlCommand command, IDictionary<string, int> index)
            {
                if (connection == null) throw new ArgumentNullException("connection");
                if (command == null) throw new ArgumentNullException("command");
                _connection = connection;
                _command = command;
                _index = index;
            }

            #region IObservable<IDictionary<string,object>> Members

            public IDisposable Subscribe(IObserver<IDictionary<string, object>> observer)
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }

                _command.BeginExecuteReader(ExecuteReaderCompleted, observer);

                return new ActionDisposable(() =>
                                                {
                                                    using (_connection)
                                                    using (_command)
                                                    {
                                                    }
                                                });
            }

            #endregion

            private void ExecuteReaderCompleted(IAsyncResult ar)
            {
                var observer = ar.AsyncState as IObserver<IDictionary<string, object>>;
                if (observer == null) throw new InvalidOperationException();
                try
                {
                    using (SqlDataReader reader = _command.EndExecuteReader(ar))
                    {
                        if (_index == null) _index = reader.CreateDictionaryIndex();
                        while (reader.Read())
                        {
                            observer.OnNext(reader.ToDictionary(_index));
                        }
                    }
                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
            }
        }

        #endregion
    }
}