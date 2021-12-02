using System;
using System.Text.RegularExpressions;
using Foundation;
using UIKit;
using UserNotifications;
using Xamarin.Essentials;

namespace IO.Ably.Push.iOS
{
    /// <inheritdoc />
    public class AppleMobileDevice : IMobileDevice
    {
        private const string TokenType = "apns";

        private readonly ILogger _logger;
        private static AblyRealtime _realtimeInstance;

        private AppleMobileDevice(PushCallbacks callbacks, ILogger logger)
        {
            Callbacks = callbacks;
            _logger = logger;
        }

        /// <summary>
        /// Initialises the Android MobileDevice implementation with the IoC dependency.
        /// </summary>
        /// <param name="ablyClientOptions">Ably client options used to initialise the AblyReltime client.</param>
        /// <param name="configureCallbacks">Action to configure callbacks.</param>
        public static IRealtimeClient Initialise(ClientOptions ablyClientOptions, Action<PushCallbacks> configureCallbacks = null)
        {
            var callbacks = new PushCallbacks();
            configureCallbacks?.Invoke(callbacks);
            return Initialise(ablyClientOptions, callbacks);
        }

        /// <summary>
        /// Initialises the Apple MobileDevice implementation and the AblyRealtime client that is used to subscribe to the Apple notification service.
        /// Use this method to initialise your AblyRealtime if you want to register the device for push notifications.
        /// </summary>
        /// <param name="ablyClientOptions">ClientOptions used to initialise the AblyRealtime instanced used to setup the ActivationStat.</param>
        /// <param name="callbacks">Optional callbacks class. It's especially useful to subscribe/unsubscribe to push channels when a device is activated / deactivated.</param>
        /// <returns>Initialised Ably instance which supports push notification registrations.</returns>
        public static IRealtimeClient Initialise(ClientOptions ablyClientOptions, PushCallbacks callbacks = null)
        {
            var mobileDevice = new AppleMobileDevice(callbacks, DefaultLogger.LoggerInstance);
            IoC.MobileDevice = mobileDevice;
            _realtimeInstance = new AblyRealtime(ablyClientOptions, mobileDevice);
            _realtimeInstance.Push.InitialiseStateMachine();
            return _realtimeInstance;
        }

        /// <summary>
        /// This method should be called by the application when a new token is generated on the device.
        /// </summary>
        /// <param name="tokenData">New device token data.</param>
        public static void OnNewRegistrationToken(NSData tokenData)
        {
            if (tokenData != null)
            {
                try
                {
                    var token = ConvertTokenToString(tokenData);

                    var realtimePush = _realtimeInstance.Push;
                    var tokenResult = Result.Ok(new RegistrationToken(TokenType, token));
                    realtimePush.StateMachine.UpdateRegistrationToken(tokenResult);
                }
                catch (Exception e)
                {
                    _realtimeInstance?.Logger.Error($"Error setting new token. Token: {tokenData}", e);
                }
            }

            string ConvertTokenToString(NSData deviceToken)
            {
                if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                {
                    return BitConverter.ToString(deviceToken.ToArray()).Replace("-", string.Empty);
                }

                return Regex.Replace(deviceToken.ToString(), "[^0-9a-zA-Z]+", string.Empty);
            }
        }

        /// <summary>
        /// Called by the device when the registration token fails registration.
        /// </summary>
        /// <param name="error">Error.</param>
        public static void OnRegistrationTokenFailed(ErrorInfo error)
        {
            var realtimePush = _realtimeInstance.Push;
            realtimePush.StateMachine.UpdateRegistrationToken(Result.Fail<RegistrationToken>(error));
        }

        /// <inheritdoc/>
        public void SetPreference(string key, string value, string groupName)
        {
            _logger.Debug($"Setting preferences: {groupName}:{key} with value {value}");
            Preferences.Set(key, value, groupName);
        }

        /// <inheritdoc/>
        public string GetPreference(string key, string groupName)
        {
            return Preferences.Get(key, string.Empty, groupName);
        }

        /// <inheritdoc/>
        public void RemovePreference(string key, string groupName)
        {
            _logger.Debug($"Removing preference: {groupName}:{key}");
            Preferences.Remove(key, groupName);
        }

        /// <inheritdoc/>
        public void ClearPreferences(string groupName)
        {
            _logger.Debug($"Clearing preferences group: {groupName}");
            Preferences.Clear(groupName);
        }

        /// <inheritdoc/>
        public void RequestRegistrationToken(Action<Result<RegistrationToken>> unusedAction) // For IOS integration the callback is not used
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                UNUserNotificationCenter.Current.RequestAuthorization(
                    UNAuthorizationOptions.Alert |
                          UNAuthorizationOptions.Sound |
                          UNAuthorizationOptions.Sound,
                    (granted, error) =>
                    {
                        if (granted)
                        {
                            MainThread.BeginInvokeOnMainThread(UIApplication.SharedApplication.RegisterForRemoteNotifications);
                        }
                        else
                        {
                            _logger.Error($"Error signing up for remote notifications: {error.LocalizedDescription}");
                        }
                    });
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                var pushSettings = UIUserNotificationSettings.GetSettingsForTypes(
                    UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound,
                    new NSSet());

                UIApplication.SharedApplication.RegisterUserNotificationSettings(pushSettings);
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            }
            else
            {
                UIRemoteNotificationType notificationTypes = UIRemoteNotificationType.Alert | UIRemoteNotificationType.Badge | UIRemoteNotificationType.Sound;
                UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(notificationTypes);
            }
        }

        /// <inheritdoc/>
        public string DevicePlatform => "ios";

        /// <inheritdoc/>
        public string FormFactor
        {
            get
            {
                var idiom = DeviceInfo.Idiom;
                if (idiom == DeviceIdiom.Watch)
                {
                    return DeviceFormFactor.Watch;
                }

                if (idiom == DeviceIdiom.TV)
                {
                    return DeviceFormFactor.Tv;
                }

                if (idiom == DeviceIdiom.Tablet)
                {
                    return DeviceFormFactor.Tablet;
                }

                if (idiom == DeviceIdiom.Phone)
                {
                    return DeviceFormFactor.Phone;
                }

                if (idiom == DeviceIdiom.Desktop)
                {
                    return DeviceFormFactor.Desktop;
                }

                return DeviceFormFactor.Other;
            }
        }

        public PushCallbacks Callbacks { get; }
    }
}
