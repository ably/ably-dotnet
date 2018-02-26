using System;

namespace IO.Ably
{
    /// <summary>Utility class that implements IDisposable bu calling the provided action.</summary>
    internal class ActionOnDispose : IDisposable
    {
        private readonly Action _action;

        public ActionOnDispose(Action act)
        {
            if (act == null)
            {
                throw new ArgumentNullException();
            }

            _action = act;
        }

        void IDisposable.Dispose()
        {
            _action();
        }
    }
}
