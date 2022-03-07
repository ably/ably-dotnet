using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using IO.Ably;
using IO.Ably.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Ably.Examples
{
    public class AblyConsole : MonoBehaviour
    {
        private AblyRealtime _ably;
        private Text _textContent;
        private Button _connectButton;
        private Button _subscribe;
        private Button _unsubscribe;
        private Button _listChannels;
        private Button _messageHistory;
        private Button _publish;
        private InputField _channelName;
        private InputField _eventName;
        private InputField _payload;

        private static string _apiKey = "";

        void Start()
        {
            RegisterUiComponents();
            InitializeAbly();
        }

        // Add components 
        private void RegisterUiComponents()
        {
            _textContent = GameObject.Find("TxtConsole").GetComponent<Text>();

            _connectButton = GameObject.Find("ConnectBtn").GetComponent<Button>();
            _connectButton.onClick.AddListener(ConnectClickHandler);

            _subscribe = GameObject.Find("Subscribe").GetComponent<Button>();
            _subscribe.onClick.AddListener(SubscribeChannelClickHandler);

            _unsubscribe = GameObject.Find("Unsubscribe").GetComponent<Button>();
            _unsubscribe.onClick.AddListener(UnsubscribeChannelHandler);

            _listChannels = GameObject.Find("ListChannels").GetComponent<Button>();
            _listChannels.onClick.AddListener(ListChannelsHandler);

            _messageHistory = GameObject.Find("MessageHistory").GetComponent<Button>();
            _messageHistory.onClick.AddListener(ChannelMessageHistoryHandler);

            _publish = GameObject.Find("Publish").GetComponent<Button>();
            _publish.onClick.AddListener(PublishClickHandler);

            _channelName = GameObject.Find("ChannelName").GetComponent<InputField>();
            _eventName = GameObject.Find("EventName").GetComponent<InputField>();
            _payload = GameObject.Find("Payload").GetComponent<InputField>();
            EnableUiComponents(false);
        }

        private void InitializeAbly()
        {
            var options = new ClientOptions();
            options.Key = _apiKey;
            // this will disable the library trying to subscribe to network state notifications
            options.AutomaticNetworkStateMonitoring = false;
            options.AutoConnect = false;
            // this will make sure to post callbacks on UnitySynchronization Context Main Thread
            options.CustomContext = SynchronizationContext.Current;

            _ably = new AblyRealtime(options);
            _ably.Connection.On(args =>
            {
                LogAndDisplay($"Connection State is <b>{args.Current}</b>");
                _connectButton.GetComponentInChildren<Text>().text = args.Current.ToString();
                var connectBtnImage = _connectButton.GetComponent<Image>();
                switch (args.Current)
                {
                    case ConnectionState.Initialized:
                        connectBtnImage.color = Color.white;
                        break;
                    case ConnectionState.Connecting:
                        connectBtnImage.color = Color.gray;
                        break;
                    case ConnectionState.Connected:
                        connectBtnImage.color = Color.green;
                        break;
                    case ConnectionState.Disconnected:
                        connectBtnImage.color = Color.yellow;
                        break;
                    case ConnectionState.Suspended:
                    case ConnectionState.Closing:
                    case ConnectionState.Closed:
                    case ConnectionState.Failed:
                        connectBtnImage.color = Color.red;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                EnableUiComponents(args.Current == ConnectionState.Connected);
            });
        }


        private void ConnectClickHandler()
        {
            _ably.Connect();
        }

        private void SubscribeChannelClickHandler()
        {
            var channelName = _channelName.text;
            var eventName = _eventName.text;
            _ably.Channels.Get(channelName).Subscribe(eventName, message =>
            {
                LogAndDisplay($"Received message <b>{message.Data}</b> from channel <b>{channelName}</b>");
            });
            LogAndDisplay($"Successfully subscribed to channel <b>{channelName}</b>");
        }

        private void UnsubscribeChannelHandler()
        {
            var channelName = _channelName.text;
            _ably.Channels.Get(channelName).Unsubscribe();
            LogAndDisplay($"Successfully unsubscribed to channel <b>{channelName}</b>");
        }

        private void ListChannelsHandler()
        {
            var channelNames = string.Join(", ", _ably.Channels.Select(channel => channel.Name));
            LogAndDisplay($"Channel Names - <b>{channelNames}</b>");
        }

        private async void ChannelMessageHistoryHandler()
        {
            var channelName = _channelName.text;
            LogAndDisplay($"#### <b>{channelName}</b> ####");
            var historyPage = await _ably.Channels.Get(channelName).HistoryAsync();
            while (true)
            {
                foreach (var message in historyPage.Items)
                {
                    LogAndDisplay(message.Data.ToString());
                }
                if (historyPage.IsLast)
                {
                    break;
                }
                historyPage = await historyPage.NextAsync();
            };
            LogAndDisplay($"#### <b>{channelName}</b> ####");
        }

        private async void PublishClickHandler()
        {
            var channelName = _channelName.text;
            var eventName = _eventName.text;
            var payload = _payload.text;
            // async-await makes sure call is executed in the background and then result is posted on UnitySynchronizationContext/Main thread
            var result = await _ably.Channels.Get(channelName).PublishAsync(eventName, payload);
            LogAndDisplay(result.IsSuccess
                ? $"Successfully published message <b>{payload}</b> to channel <b>{channelName}</b>"
                : $"Error publishing message <b>{payload}</b> to channel <b>{channelName}</b>, failed with error <b>{result.Error.Message}</b>");
        }

        private void LogAndDisplay(string message)
        {
            Debug.Log(message);
            _textContent.text = $"{_textContent.text}\n{message}";
        }

        private void EnableUiComponents(bool isEnabled)
        {
            _channelName.interactable = isEnabled;
            _eventName.interactable = isEnabled;
            _payload.interactable = isEnabled;
            _subscribe.interactable = isEnabled;
            _unsubscribe.interactable = isEnabled;
            _listChannels.interactable = isEnabled;
            _messageHistory.interactable = isEnabled;
            _publish.interactable = isEnabled;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                LogAndDisplay("Clicked left arrow");
            }
        }
    }
}