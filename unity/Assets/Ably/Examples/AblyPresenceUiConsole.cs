using IO.Ably;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Ably.Examples
{
    public class AblyPresenceUiConsole
    {
        private AblyRealtime _ably;
        private readonly IUiConsole _uiConsole;

        private Button _presenceSubscribe;
        private Button _presenceUnsubscribe;
        private Button _getPresence;
        private Button _presenceMessageHistory;
        private Button _enterPresence;
        private Button _leavePresence;
        private InputField _channelName;
        private InputField _payload;

        private AblyPresenceUiConsole(AblyRealtime ably, IUiConsole uiConsole)
        {
            _ably = ably;
            _uiConsole = uiConsole;
        }

        internal static AblyPresenceUiConsole CreateInstance(AblyRealtime ably, IUiConsole uiConsole)
        {
            return new AblyPresenceUiConsole(ably, uiConsole);
        }

        internal void RegisterUiComponents()
        {
            _presenceSubscribe = GameObject.Find("SubscribePresence").GetComponent<Button>();
            _presenceSubscribe.onClick.AddListener(PresenceSubscribeClickHandler);

            _presenceUnsubscribe = GameObject.Find("UnsubscribePresence").GetComponent<Button>();
            _presenceUnsubscribe.onClick.AddListener(PresenceUnsubscribeClickHandler);

            _getPresence = GameObject.Find("GetPresence").GetComponent<Button>();
            _getPresence.onClick.AddListener(GetPresenceClickHandler);

            _presenceMessageHistory = GameObject.Find("PresenceHistory").GetComponent<Button>();
            _presenceMessageHistory.onClick.AddListener(PresenceMessageHistoryClickHandler);

            _enterPresence = GameObject.Find("EnterPresence").GetComponent<Button>();
            _enterPresence.onClick.AddListener(PresenceEnterClickHandler);

            _leavePresence = GameObject.Find("LeavePresence").GetComponent<Button>();
            _leavePresence.onClick.AddListener(PresenceLeaveClickHandler);

            _channelName = GameObject.Find("PresenceChannel").GetComponent<InputField>();
            _payload = GameObject.Find("PresencePayload").GetComponent<InputField>();
            EnableUiComponents(false);
        }

        internal void PresenceSubscribeClickHandler()
        {
            _uiConsole.LogAndDisplay("Presence subscribe clicked");
        }

        internal void PresenceUnsubscribeClickHandler()
        {
            _uiConsole.LogAndDisplay("Presence unsubscribe clicked");

        }

        internal void GetPresenceClickHandler()
        {
            _uiConsole.LogAndDisplay("Get presence clicked");

        }

        internal void PresenceMessageHistoryClickHandler()
        {
            _uiConsole.LogAndDisplay("Presence history clicked");

        }

        internal void PresenceEnterClickHandler()
        {
            _uiConsole.LogAndDisplay("Presence enter clicked");

        }
        internal void PresenceLeaveClickHandler()
        {
            _uiConsole.LogAndDisplay("Presence leave clicked");

        }

        internal void EnableUiComponents(bool isEnabled)
        {
            _channelName.interactable = isEnabled;
            _presenceSubscribe.interactable = isEnabled;
            _payload.interactable = isEnabled;
            _presenceSubscribe.interactable = isEnabled;
            _presenceUnsubscribe.interactable = isEnabled;
            _getPresence.interactable = isEnabled;
            _presenceMessageHistory.interactable = isEnabled;
            _enterPresence.interactable = isEnabled;
            _leavePresence.interactable = isEnabled;
        }
    }
}

