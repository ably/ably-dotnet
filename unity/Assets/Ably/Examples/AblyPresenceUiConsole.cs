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
            var channelName = _channelName.text;
            _ably.Channels.Get(channelName).Presence.Subscribe(message =>
            {
                _uiConsole.LogAndDisplay($"Received presence message <b>{message.Data}</b> from channel <b>{channelName}</b>");
            });
            _uiConsole.LogAndDisplay($"Successfully subscribed to channel <b>{channelName}</b> for <b>Presence</b> messages");
        }

        internal void PresenceUnsubscribeClickHandler()
        {
            var channelName = _channelName.text;
            _ably.Channels.Get(channelName).Presence.Unsubscribe();
            _uiConsole.LogAndDisplay($"Successfully unsubscribed to channel <b>{channelName}</b> for <b>Presence</b> messages");
        }

        internal async void GetPresenceClickHandler()
        {
            var channelName = _channelName.text;
            var presenceMessages = await _ably.Channels.Get(channelName).Presence.GetAsync();
            _uiConsole.LogAndDisplay($"#### <b>{channelName}</b> ####");
            foreach (var presenceMessage in presenceMessages)
            {
                _uiConsole.LogAndDisplay(presenceMessage.Data.ToString());
            }
            _uiConsole.LogAndDisplay($"#### <b>{channelName}</b> ####");
        }

        internal async void PresenceMessageHistoryClickHandler()
        {
            var channelName = _channelName.text;
            _uiConsole.LogAndDisplay($"#### <b>{channelName}</b> ####");
            var presenceHistoryPage = await _ably.Channels.Get(channelName).Presence.HistoryAsync();
            while (true)
            {
                foreach (var presenceMessage in presenceHistoryPage.Items)
                {
                    _uiConsole.LogAndDisplay(presenceMessage.Data.ToString());
                }
                if (presenceHistoryPage.IsLast)
                {
                    break;
                }
                presenceHistoryPage = await presenceHistoryPage.NextAsync();
            };
            _uiConsole.LogAndDisplay($"#### <b>{channelName}</b> ####");
        }

        internal async void PresenceEnterClickHandler()
        {
            var channelName = _channelName.text;

            // async-await makes sure call is executed in the background and then result is posted on UnitySynchronizationContext/Main thread
            var result = await _ably.Channels.Get(channelName).Presence.EnterAsync(_payload.text);

            _uiConsole.LogAndDisplay(result.IsSuccess
                ? $"Successfully entered presence to channel <b>{channelName}</b>"
                : $"Error entering presence to channel <b>{channelName}</b>, failed with error <b>{result.Error.Message}</b>");
        }

        internal async void PresenceLeaveClickHandler()
        {
            var channelName = _channelName.text;

            // async-await makes sure call is executed in the background and then result is posted on UnitySynchronizationContext/Main thread
            var result = await _ably.Channels.Get(channelName).Presence.LeaveAsync(_payload.text);

            _uiConsole.LogAndDisplay(result.IsSuccess
                ? $"Successfully left presence to channel <b>{channelName}</b>"
                : $"Error leaving presence to channel <b>{channelName}</b>, failed with error <b>{result.Error.Message}</b>");
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

