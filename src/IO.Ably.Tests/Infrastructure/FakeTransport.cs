using System;
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
                State = state.Value;
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

        public ProtocolMessage LastMessageSend => LastTransportData.Original;
        public RealtimeTransportData LastTransportData { get; set; }
        public string Host { get; set; }
        public TransportState State { get; set; }
        public ITransportListener Listener { get; set; }

        public bool OnConnectChangeStateToConnected { get; set; } = true;
        public void Connect()
        {
            ConnectCalled = true;
            if (OnConnectChangeStateToConnected)
            {
               Listener?.OnTransportConnected();
                State = TransportState.Connected;
            }
        }

        public void Close()
        {
            CloseCalled = true;

            //Listener?.OnTransportDataReceived(new ProtocolMessage(ProtocolMessage.MessageAction.Closed));
            Listener?.OnTransportDisconnected();
        }

        public void Send(RealtimeTransportData data)
        {
            SendAction(data);
            LastTransportData = data;
        }


        public void Abort(string reason)
        {
            AbortCalled = true;
        }

        public Action<RealtimeTransportData> SendAction = delegate { };
        private volatile bool _connectCalled;
    }
}