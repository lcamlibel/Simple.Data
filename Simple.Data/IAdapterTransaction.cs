using System;

namespace Simple.Data
{
    /// <summary>
    /// Represents a transaction for any adapter which supports those.
    /// </summary>
    public interface IAdapterTransaction : IDisposable
    {
        /// <summary>
        /// Gets the Name of the transaction, if named transactions are supported (e.g. SQL Server).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Commits the underlying transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rollbacks the underlying transaction.
        /// </summary>
        void Rollback();
    }
}