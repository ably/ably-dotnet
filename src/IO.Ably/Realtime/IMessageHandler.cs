using System;

namespace IO.Ably.Realtime
{
    /// <summary>Interface to actually handle those messages as they arrive.</summary>
    /// <remarks>
    ///     <para>
    ///         <b>NB!</b> Channel doesn't retain the handlers, internally, it uses weak references. This mean you must
    ///         retain the handlers yourself.
    ///     </para>
    ///     <para>You can handle them in your class, or you can use <see cref="MessageHandlerAction" />.</para>
    /// </remarks>
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
    }
}