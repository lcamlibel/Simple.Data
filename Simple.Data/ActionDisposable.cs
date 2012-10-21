using System;

namespace Simple.Data
{
    public sealed class ActionDisposable : IDisposable
    {
        public static readonly IDisposable NoOp = new ActionDisposable();
        private readonly Action _action;

        public ActionDisposable() : this(null)
        {
        }

        public ActionDisposable(Action action)
        {
            _action = action ?? (() => { });
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        ~ActionDisposable()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                _action();
        }
    }
}