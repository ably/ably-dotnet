using System;
using System.Text.RegularExpressions;
using Foundation;
using UIKit;
using UserNotifications;
using Xamarin.Essentials;

namespace IO.Ably.Push.iOS
{
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

        public static IRealtimeClient Initialise(ClientOptions ablyClientOptions, PushCallbacks callbacks)
        {
            var mobileDevice = new AppleMobileDevice(callbacks, DefaultLogger.LoggerInstance);
            IoC.MobileDevice = mobileDevice;
            _realtimeInstance = new AblyRealtime(ablyClientOptions, mobileDevice);
            _realtimeInstance.Push.InitialiseStateMachine();
            return _realtimeInstance;
        }

        public static void OnNewRegistrationToken(NSData tokenData)
        {
            if (tokenData != null)
            {
                try
                {
                    var token = ConvertTokenToString(tokenData);
                    // Call the state machine to register the new token
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

        public static void OnRegistrationTokenFailed(ErrorInfo error)
        {
            // Call the state machine to register the new token
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

        public void RequestRegistrationToken(Action<Result<RegistrationToken>> _) // For IOS integration the callback is not used
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert |
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