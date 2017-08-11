using System;
using System.Reactive.Subjects;
using IO.Ably;

namespace App2.Droid
{
    public class AblyService : IAblyService, ILoggerSink
    {
        private AblyRealtime _ably;
        private readonly ISubject<LogMessage> _logSubject = new Subject<LogMessage>();
        private readonly ISubject<string> _connectionSubject = new Subject<string>();

        public void Init()
        {
            _ably = new AblyRealtime(new ClientOptions("lNj80Q.iGyVcQ:2QKX7FFASfX-7H9H")
            {
                LogHander = this,
                LogLevel = LogLevel.Debug, 
                AutoConnect = false,
                //TransportFactory = new WebSocketTransport.WebSocketTransportFactory()
            });
            _ably.Connection.On(change => _connectionSubject.OnNext(change.Current.ToString()));
        }

        public void Connect()
        {
            _ably.Connect();
        }

        public void SendMessage(string channel, string name, string value)
        {
            _ably.Channels.Get(channel).Publish(name, value);
        }

        public void LogEvent(LogLevel level, string message)
        {
            Android.Util.Log.Debug("ably", $"[{level}] {message}");
            _logSubject.OnNext(new LogMessage(message, level.ToString()));
        }

        public IDisposable Subscribe(IObserver<LogMessage> observer)
        {
            return _logSubject.Subscribe(observer);
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            return _connectionSubject.Subscribe(observer);
        }
    }

    

}