using System;
using System.Collections.Generic;
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
            private readonly Action<ProtocolMessage> _beforeMessage;
            private readonly Action<ProtocolMessage> _afterMessage;
            private readonly MessageHandler _handler;

            public List<ProtocolMessage> ProtocolMessagesReceived { get; set; } = new List<ProtocolMessage>();

            public TransportListenerWrapper(ITransportListener wrappedListener, Action<ProtocolMessage> beforeMessage, Action<ProtocolMessage> afterMessage, MessageHandler handler)
            {
                _wrappedListener = wrappedListener;
                _beforeMessage = beforeMessage;
                _afterMessage = afterMessage;
                _handler = handler;
            }

            public void OnTransportDataReceived(RealtimeTransportData data)
            {
                ProtocolMessage msg = null;
                try
                {
                    msg = _handler.ParseRealtimeData(data);
                    ProtocolMessagesReceived.Add(msg);
                    _beforeMessage?.Invoke(msg);
                    if (_beforeMessage != null)
                    {
                        data = _handler.GetTransportData(msg);
                    }
                }
                catch (Exception ex)
                {
                    DefaultLogger.Error("Error handling afterMessage helper.", ex);
                }

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
                    _afterMessage?.Invoke(msg);
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

        /// <summary>
        /// A list of all protocol messages that have been received from the ably service since the transport was created
        /// </summary>
        public List<ProtocolMessage> ProtocolMessagesReceived => (Listener as TransportListenerWrapper)?.ProtocolMessagesReceived;

        public Action<ProtocolMessage> BeforeDataProcessed = delegate { };

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
            get => WrappedTransport.Listener;
            set
            {
                var listener = new TransportListenerWrapper(value, BeforeDataProcessed, AfterDataReceived, _handler);
                WrappedTransport.Listener = listener;
            }
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
