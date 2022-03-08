using System;
using System.Threading;
using IO.Ably;
using IO.Ably.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Ably.Example
{
    public class AblyConsole : MonoBehaviour, IUiConsole
    {
        private AblyRealtime _ably;
        private ClientOptions _clientOptions;

        private Text _textContent;
        private InputField _clientId;
        private Button _connectButton;

        private static string _apiKey = "";

        private AblyChannelUiConsole _ablyChannelUiConsole;
        private AblyPresenceUiConsole _ablyPresenceUiConsole;

        void Start()
        {
            InitializeAbly();
            RegisterUiComponents();
            _ablyChannelUiConsole = AblyChannelUiConsole.CreateInstance(_ably, this);
            _ablyChannelUiConsole.RegisterUiComponents();
            _ablyPresenceUiConsole = AblyPresenceUiConsole.CreateInstance(_ably, this);
            _ablyPresenceUiConsole.RegisterUiComponents();
        }

        // Add components 
        private void RegisterUiComponents()
        {
            _textContent = GameObject.Find("TxtConsole").GetComponent<Text>();
            _clientId = GameObject.Find("ClientId").GetComponent<InputField>();
            _connectButton = GameObject.Find("ConnectBtn").GetComponent<Button>();
            _connectButton.onClick.AddListener(ConnectClickHandler);
        }

        private void InitializeAbly()
        {
            _clientOptions = new ClientOptions
            {
                Key = _apiKey,
                // this will disable the library trying to subscribe to network state notifications
                AutomaticNetworkStateMonitoring = false,
                AutoConnect = false,
                // this will make sure to post callbacks on UnitySynchronization Context Main Thread
                CustomContext = SynchronizationContext.Current
            };

            _ably = new AblyRealtime(_clientOptions);
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
                _ablyChannelUiConsole.EnableUiComponents(args.Current == ConnectionState.Connected);
                _ablyPresenceUiConsole.EnableUiComponents(args.Current == ConnectionState.Connected);
            });
        }


        private void ConnectClickHandler()
        {
            _clientOptions.ClientId = _clientId.text;
            _ably.Connect();
        }

        public void LogAndDisplay(string message)
        {
            Debug.Log(message);
            _textContent.text = $"{_textContent.text}\n{message}";
        }
    }

    internal interface IUiConsole
    {
        void LogAndDisplay(string message);
    }
}