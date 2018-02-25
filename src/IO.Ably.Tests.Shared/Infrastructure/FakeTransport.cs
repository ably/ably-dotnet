using System;
using System.Collections.Generic;
using System.Linq;
using IO.Ably.Realtime;
using IO.Ably.Transport;
using IO.Ably.Types;

namespace IO.Ably.Tests
{
    public class FakeTransport : ITransport
    {
        public TransportParams Parameters { get; }

        public FakeTransport(TransportState? state = null)
        {
            if (state.HasValue)
            {
                State = state.Value;
            }
        }

        public FakeTransport(TransportParams parameters)
        {
            Parameters = parameters;
        }

        public bool ConnectCalled
        {
            get { return _connectCalled; }
            set { _connectCalled = value; }
        }

        public bool CloseCalled { get; set; }

        public bool AbortCalled { get; set; }

        public ProtocolMessage LastMessageSend => LastTransportData?.Original;

        public List<RealtimeTransportData> SentMessages { get; set; } = new List<RealtimeTransportData>();

        public RealtimeTransportData LastTransportData => SentMessages.LastOrDefault();

        public TransportState State { get; set; }

        public ITransportListener Listener { get; set; }

        public bool OnConnectChangeStateToConnected { get; set; } = true;

        public void Connect()
        {
            DefaultLogger.Debug("Connecting using: " + Parameters.GetUri().ToString());

            ConnectCalled = true;
            if (OnConnectChangeStateToConnected)
            {
                Listener?.OnTransportEvent(TransportState.Connected);
                State = TransportState.Connected;
            }
        }

        public void Close(bool suppressClosedEvent = true)
        {
            CloseCalled = true;

            // Listener?.OnTransportDataReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));
            if (suppressClosedEvent == false)
            {
                Listener?.OnTransportEvent(TransportState.Closed);
            }
        }

        public void Send(RealtimeTransportData data)
        {
            SendAction(data);
            SentMessages.Add(data);
        }

        public void Abort(string reason)
        {
            AbortCalled = true;
        }

        public Action<RealtimeTransportData> SendAction = delegate { };

        private volatile bool _connectCalled;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}