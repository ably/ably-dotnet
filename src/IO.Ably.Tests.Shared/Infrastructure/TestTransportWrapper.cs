using System;
using IO.Ably.MessageEncoders;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Tests.Infrastructure
{
    internal class TestTransportWrapper : ITransport
    {
        private class TransportListenerWrapper : ITransportListener
        {
            private readonly ITransportListener _wrappedListener;
            private readonly Action<ProtocolMessage> _afterMessage;
            private readonly MessageHandler _handler;

            public TransportListenerWrapper(ITransportListener wrappedListener, Action<ProtocolMessage> afterMessage, MessageHandler handler)
            {
                _wrappedListener = wrappedListener;
                _afterMessage = afterMessage;
                _handler = handler;
            }

            public void OnTransportDataReceived(RealtimeTransportData data)
            {
                try
                {
                    _wrappedListener.OnTransportDataReceived(data);
                }
                catch (Exception e)
                {
                    DefaultLogger.Error("Test transport factor on receive error ", e);
                }

                try
                {
                    _afterMessage(_handler.ParseRealtimeData(data));
                }
                catch (Exception ex)
                {
                    DefaultLogger.Error("Error handling afterMessage helper.", ex);
                }
            }

            public void OnTransportEvent(TransportState state, Exception exception = null)
            {
                _wrappedListener?.OnTransportEvent(state, exception);
            }
        }

        internal ITransport WrappedTransport { get; }

        private readonly MessageHandler _handler;

        public Action<ProtocolMessage> AfterDataReceived = delegate { };

        public Action<ProtocolMessage> MessageSent = delegate { };

        public TestTransportWrapper(ITransport wrappedTransport, Protocol protocol)
        {
            WrappedTransport = wrappedTransport;
            _handler = new MessageHandler(protocol);
        }

        public TransportState State => WrappedTransport.State;

        public ITransportListener Listener
        {
            get { return WrappedTransport.Listener; }
            set { WrappedTransport.Listener = new TransportListenerWrapper(value, x => AfterDataReceived(x), _handler); }
        }

        public void FakeTransportState(TransportState state, Exception ex = null)
        {
            Listener?.OnTransportEvent(state, ex);
        }

        public void FakeReceivedMessage(ProtocolMessage message)
        {
            var data = _handler.GetTransportData(message);
            Listener?.OnTransportDataReceived(data);
        }

        public void Connect()
        {
            WrappedTransport.Connect();
        }

        public void Close(bool suppressClosedEvent = true)
        {
            DefaultLogger.Debug("Closing test transport!");
            WrappedTransport.Close(suppressClosedEvent);
        }

        public void Send(RealtimeTransportData data)
        {
            MessageSent(data.Original);
            WrappedTransport.Send(data);
        }

        public void Dispose()
        {
        }
    }
}