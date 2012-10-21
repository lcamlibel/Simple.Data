using System;
using System.Collections.Generic;

namespace Simple.Data
{
    public static class ColdObservable
    {
        public static readonly IDisposable EmptyDisposable = new _EmptyDisposable();

        public static IObservable<T> Create<T>(Func<IObserver<T>, IDisposable> func)
        {
            return new ColdObservable<T>(func);
        }

        public static IObservable<T> ToObservable<T>(this IEnumerable<T> source)
        {
            return Create<T>(o =>
                                 {
                                     try
                                     {
                                         foreach (T item in source)
                                         {
                                             o.OnNext(item);
                                         }
                                         o.OnCompleted();
                                     }
                                     catch (Exception ex)
                                     {
                                         o.OnError(ex);
                                     }
                                     return EmptyDisposable;
                                 });
        }

        #region Nested type: _EmptyDisposable

        private class _EmptyDisposable : IDisposable
        {
            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion
        }

        #endregion
    }

    internal class ColdObservable<T> : IObservable<T>
    {
        private readonly Func<IObserver<T>, IDisposable> _func;

        public ColdObservable(Func<IObserver<T>, IDisposable> func)
        {
            _func = func;
        }

        #region IObservable<T> Members

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _func(observer);
        }

        #endregion
    }

    internal static class MapObservable
    {
        public static IObservable<TOut> Map<TIn, TOut>(this IObservable<TIn> source, Func<TIn, TOut> mapFunc)
        {
            return new MapObservable<TIn, TOut>(source, mapFunc);
        }
    }

    internal class MapObservable<TIn, TOut> : IObservable<TOut>
    {
        private readonly Func<TIn, TOut> _map;
        private readonly IObservable<TIn> _source;

        public MapObservable(IObservable<TIn> source, Func<TIn, TOut> map)
        {
            _source = source;
            _map = map;
        }

        #region IObservable<TOut> Members

        public IDisposable Subscribe(IObserver<TOut> observer)
        {
            return _source.Subscribe(new MapObserver(observer, _map));
        }

        #endregion

        #region Nested type: MapObserver

        private class MapObserver : IObserver<TIn>
        {
            private readonly Func<TIn, TOut> _map;
            private readonly IObserver<TOut> _observer;

            public MapObserver(IObserver<TOut> observer, Func<TIn, TOut> map)
            {
                _observer = observer;
                _map = map;
            }

            #region IObserver<TIn> Members

            public void OnNext(TIn value)
            {
                _observer.OnNext(_map(value));
            }

            public void OnError(Exception error)
            {
                _observer.OnError(error);
            }

            public void OnCompleted()
            {
                _observer.OnCompleted();
            }

            #endregion
        }

        #endregion
    }
}