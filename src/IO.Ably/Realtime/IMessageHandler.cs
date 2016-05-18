using System;

namespace IO.Ably.Realtime
{
    public interface IMessageHandler
    {
        void Handle(Message message);
    }

    /// <summary>Adapter to pass a delegate as IMessageHandler.</summary>
    public class MessageHandlerAction : IMessageHandler
    {
        private readonly Action<Message> action;

        public MessageHandlerAction(Action<Message> action)
        {
            if (null == action)
                throw new ArgumentNullException();
            this.action = action;
        }

        void IMessageHandler.Handle(Message message)
        {
            action(message);
        }

        protected bool Equals(MessageHandlerAction other)
        {
            return Equals(action, other.action);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MessageHandlerAction) obj);
        }

        public override int GetHashCode()
        {
            return (action != null ? action.GetHashCode() : 0);
        }
    }
}