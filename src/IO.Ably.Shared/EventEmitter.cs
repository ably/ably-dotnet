using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IO.Ably
{
    public interface IEventEmitter<TEvent, TArgs> where TEvent : struct where TArgs : EventArgs
    {
        void Off();
        void On(Action<TArgs> listener);
        void Once(Action<TArgs> listener);
        void Off(Action<TArgs> listener);
        void On(TEvent state, Action<TArgs> action);
        void Once(TEvent state, Action<TArgs> action);
        void Off(TEvent state, Action<TArgs> action);
    }

    public abstract class EventEmitter<TState, TArgs> : IEventEmitter<TState, TArgs> where TState : struct
        where TArgs : EventArgs
    {
        readonly List<Emitter<TState, TArgs>> _emitters = new List<Emitter<TState, TArgs>>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        private class Emitter<TState, TArgs> where TState : struct
        {
            public Action<TArgs> Action { get; }
            public bool Once { get; }
            public TState? State { get; }

            public Emitter(Action<TArgs> action, TState? state = null, bool once = false)
            {
                if (action == null)
                {
                    throw new ArgumentException("Cannot pass a null action to the EventEmitter");
                }

                Action = action;
                State = state;
                Once = once;
            }
        }

        public void Off()
        {
            _lock.EnterWriteLock();
            try
            {
                _emitters.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void On(Action<TArgs> listener)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_emitters.Any(x => x.Action == listener) == false)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _emitters.Add(new Emitter<TState, TArgs>(listener));
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public void Once(Action<TArgs> listener)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_emitters.Any(x => x.Action == listener) == false)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _emitters.Add(new Emitter<TState, TArgs>(listener, once: true));
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        public void Off(Action<TArgs> listener)
        {
            _lock.EnterWriteLock();
            try
            {
                _emitters.RemoveAll(x => x.Action == listener);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void On(TState state, Action<TArgs> action)
        {
            _lock.EnterWriteLock();
            try
            {
                _emitters.Add(new Emitter<TState, TArgs>(action, state));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Once(TState state, Action<TArgs> action)
        {
            _lock.EnterWriteLock();
            try
            {
                _emitters.Add(new Emitter<TState, TArgs>(action, state, true));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Off(TState state, Action<TArgs> action)
        {
            _lock.EnterWriteLock();
            try
            {
                _emitters.RemoveAll(x => x.Action == action && x.State.HasValue && x.State.Value.Equals(state));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Emit(TState state, TArgs data)
        {
            List<Emitter<TState, TArgs>> emitters;
            _lock.EnterUpgradeableReadLock();
            try
            {
                emitters = _emitters.Where(x => x.State == null
                                            || x.State.Value.Equals(state)).ToList();

                if (emitters.Any(x => x.Once))
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        foreach (var emitter in emitters.Where(x => x.Once))
                        {
                            _emitters.Remove(emitter);
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }

            foreach (var emitter in emitters)
            {
                try
                {
                    emitter.Action(data);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error executing on event: {state}", ex);
                }
            }
        }

    }
}
