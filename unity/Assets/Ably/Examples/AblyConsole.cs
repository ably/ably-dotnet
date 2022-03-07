using System;
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
        void RegisterUiComponents()
        {
            _textContent = GameObject.Find("TxtConsole").GetComponent<Text>();

            _connectButton = GameObject.Find("ConnectBtn").GetComponent<Button>();
            _connectButton.onClick.AddListener(ConnectClickHandler);

            _subscribe = GameObject.Find("Subscribe").GetComponent<Button>();
            _subscribe.onClick.AddListener(SubscribeClickHandler);

            _publish = GameObject.Find("Publish").GetComponent<Button>();
            _publish.onClick.AddListener(PublishClickHandler);

            _channelName = GameObject.Find("ChannelName").GetComponent<InputField>();
            _eventName = GameObject.Find("EventName").GetComponent<InputField>();
            _payload = GameObject.Find("Payload").GetComponent<InputField>();
            SetUiComponentsActive(false);
        }

        void InitializeAbly()
        {
            var options = new ClientOptions();
            options.Key = _apiKey;
            // this will disable the library trying to subscribe to network state notifications
            options.AutomaticNetworkStateMonitoring = false;
            options.CaptureCurrentSynchronizationContext = true;
            options.AutoConnect = false;
            // this will make sure to post callbacks on UnitySynchronization Context Main Thread
            options.CustomContext = SynchronizationContext.Current;

            _ably = new AblyRealtime(options);
            _ably.Connection.On(args =>
            {
                LogAndDisplay($"Connection State is {args.Current}");
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
                SetUiComponentsActive(args.Current == ConnectionState.Connected);
            });
        }


        void ConnectClickHandler()
        {
            _ably.Connect();
        }

        void SubscribeClickHandler()
        {
            var channelName = _channelName.text;
            var eventName = _eventName.text;
            var payload = _payload.text;

            LogAndDisplay($"{channelName} {eventName} {payload}");
        }

        void PublishClickHandler()
        {
            var channelName = _channelName.text;
            var eventName = _eventName.text;
            var payload = _payload.text;

            LogAndDisplay($"{channelName} {eventName} {payload}");
        }

        void LogAndDisplay(string message)
        {
            Debug.Log(message);
            _textContent.text = $"{_textContent.text}\n{message}";
        }

        void SetUiComponentsActive(bool isActive)
        {
            _channelName.interactable = isActive;
            _eventName.interactable = isActive;
            _payload.interactable = isActive;
            _subscribe.interactable = isActive;
            _publish.interactable = isActive;
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