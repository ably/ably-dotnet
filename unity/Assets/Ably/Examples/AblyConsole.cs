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
        private static string _apiKey = "";

        void Start()
        {
            AddComponents();
            InitializeAbly();
        }

        // Add components 
        void AddComponents()
        {
            _textContent = GameObject.Find("TxtConsole").GetComponent<Text>();
            _connectButton = GameObject.Find("ConnectBtn").GetComponent<Button>();
            _connectButton.onClick.AddListener(OnConnectClickHandler);
        }

        void InitializeAbly()
        {
            LogAndDisplay("Initializing Ably Object");
            var options = new ClientOptions();
            options.Key = _apiKey;
            // this will disable the library trying to subscribe to network state notifications
            options.AutomaticNetworkStateMonitoring = false;
            options.CaptureCurrentSynchronizationContext = true;
            options.AutoConnect = false;
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
            });
            LogAndDisplay("Initialized Ably Object");
        }


        void OnConnectClickHandler()
        {
            _ably.Connect();
        }

        void LogAndDisplay(string message)
        {
            Debug.Log(message);
            _textContent.text = $"{_textContent.text}\n{message}";
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