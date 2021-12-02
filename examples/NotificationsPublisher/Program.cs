﻿using System;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using IO.Ably;
using IO.Ably.Types;
using Newtonsoft.Json.Linq;
using Terminal.Gui;

namespace NotificationsPublisher
{
    static class Publisher
    {
        private const string AblyKeyFileName = "key.secret";
        static readonly AppLogger Logger = new ();

        static Window CreateLogsWindow()
        {
            var win = new Window("Logs")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            Application.Top.GetCurrentWidth(out int currentWidth);
            Application.Top.GetCurrentHeight(out int currentHeight);

            ListView listView = new ListView(Logger.Messages)
            {
                X = 1,
                Y = 1,
                Width = currentWidth,
                Height = currentHeight,
                ColorScheme = Colors.TopLevel
            };

            win.Add(listView);
            Logger.OnMessageAdded = msg =>
            {
                if (Logger.Messages.Count > currentHeight)
                {
                    listView.ScrollDown(1);
                }
            };

            return win;
        }

        static Window CreateMessagesWindow()
        {
            var win = new Window("Messages")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            var channelLabel = new Label("Channel: ") { X = 1, Y = 1, Width = 10 };
            var channelMessage = new TextField(string.Empty) { X = Pos.Right(channelLabel), Y = 1, Width = Dim.Percent(80) };

            var messageFrame = new FrameView("Message") { X = 1, Y = Pos.Bottom(channelLabel), Height = Dim.Fill(1) - 2, Width = Dim.Percent(90) };

            var messageNameLabel = new Label("Name:") { X = 1, Y = 1, Width = 10};
            var messageNameText = new TextField(string.Empty) { X = Pos.Right(messageNameLabel), Y = Pos.Top(messageNameLabel), Width = Dim.Percent(90) };

            var messageDataLabel = new Label("Data:") { X = 1, Y = Pos.Bottom(messageNameText) + 1, Width = 10};
            var messageDataText = new TextView() { X = Pos.Right(messageDataLabel), Y = Pos.Top(messageDataLabel), Width = Dim.Percent(90),
                Multiline = true, Height = 5, ColorScheme = Colors.Dialog,
                Border = new Border() { BorderStyle = BorderStyle.Single, BorderBrush = Color.Black, BorderThickness = new Thickness(1)}};

            var messageExtrasLabel = new Label("Extras:") { X = 1, Y = Pos.Bottom(messageDataText) + 1, Width = 10};

            var messageExtrasText = new TextView() { X = Pos.Right(messageExtrasLabel), Y = Pos.Top(messageExtrasLabel), Width = Dim.Percent(90),
                Multiline = true, Height = 10, ColorScheme = Colors.Dialog,
                Border = new Border() { BorderStyle = BorderStyle.Single, BorderBrush = Color.Black, BorderThickness = new Thickness(1)}};

            var pushNotificationExtraButton = new Button("Push notification")
            {
                X = Pos.Left(messageExtrasText),
                Y = Pos.Bottom(messageExtrasText) + 1,
            };

            pushNotificationExtraButton.Clicked += PushNotificationExtraButtonOnClicked;
            void PushNotificationExtraButtonOnClicked()
            {
                messageExtrasText.Text = @"{
    ""push"": {
      ""notification"": {
        ""title"": ""title goes here"",
        ""body"": ""Body"",
        ""sound"": ""default"",
    },
  },
}
";
            }


            var pushNotificationWithDataExtraButton = new Button("Push with data")
            {
                X = Pos.Right(pushNotificationExtraButton) + 1,
                Y = Pos.Bottom(messageExtrasText) + 1,
            };
            pushNotificationWithDataExtraButton.Clicked += PushNotificationExtraWithDataButtonClicked;

            void PushNotificationExtraWithDataButtonClicked()
            {
                messageExtrasText.Text = @"{
    ""push"": {
      ""data"": {""foo"": ""bar"", ""baz"": ""quz""},
      ""apns"": {
        ""apns-headers"": {
          ""apns-push-type"": ""background"",
          ""apns-priority"": ""5"",
        },
        ""aps"": {
          ""content-available"": 1
        }
    }
  },
}
";
            }

            messageFrame.Add(messageNameLabel, messageNameText, messageDataLabel, messageDataText, messageExtrasLabel, messageExtrasText, pushNotificationExtraButton, pushNotificationWithDataExtraButton);

            var sendButton = new Button("Send")
            {
                X = 1,
                Y = Pos.Bottom(messageFrame) + 1,
                IsDefault = true,
            };

            var errorLabel = new Label()
            {
                X = Pos.Right(sendButton) + 1,
                Y = Pos.Top(sendButton),
                ColorScheme = Colors.Error,
                Visible = false
            };

            sendButton.Clicked += Send;

            void Send()
            {
                errorLabel.Visible = false;
                if (channelMessage.Text.IsEmpty)
                {
                    errorLabel.Text = "Please enter channel name";
                    errorLabel.Visible = true;
                    return;
                }

                if (messageDataText.Text.IsEmpty)
                {
                    errorLabel.Text = "Please enter message data";
                    errorLabel.Visible = true;
                    return;
                }

                var extrasResult = ParseExtras();
                MessageExtras messageExtras = null;
                if (extrasResult.IsFailure)
                {
                    errorLabel.Text = $"Error parsing Extras: {extrasResult.Error.Message}";
                    errorLabel.Visible = true;
                    return;
                }

                if (extrasResult.IsSuccess && extrasResult.Value is not null)
                {
                    messageExtras = new MessageExtras(extrasResult.Value);
                }

                var channelName = channelMessage.Text.ToString();
                var messageName = messageNameText.Text.ToString();
                var data = messageDataText.Text.ToString();

                var message = new Message(messageName, data, extras: messageExtras);
                Ably.Channels.Get(channelName).Publish(message);
            }

            Result<JToken> ParseExtras()
            {
                try
                {
                    var extras = messageExtrasText.Text.ToString();
                    if (string.IsNullOrWhiteSpace(extras))
                    {
                        return Result.Ok<JToken>(null);
                    }

                    return Result.Ok(JToken.Parse(messageExtrasText.Text.ToString()));
                }
                catch (Exception e)
                {
                    return Result.Fail<JToken>(new ErrorInfo(e.Message));
                }
            }

            win.Add(channelLabel, channelMessage, messageFrame, sendButton, errorLabel);

            return win;

            // Button 1 (Push notification)
            // Button 2 (Push with background data)
        }


        static Window CreateMainWindow()
        {
            var win = new Window("Hello")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            var keyLabel = new Label("Ably Key: ") { X = 1, Y = 1 };
            var keyText = new Label() { X = Pos.Right(keyLabel), Y = Pos.Top(keyLabel) };

            var connectionStatusLabel = new Label("Connection: ") { X = Pos.Left(keyLabel), Y = Pos.Bottom(keyLabel) + 1 };
            var connectionStatusText = new Label() { X = Pos.Right(connectionStatusLabel), Y = Pos.Top(connectionStatusLabel) };

            Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(500), CheckAblyInitialized);

            win.Add(
                keyLabel,
                keyText,
                connectionStatusLabel,
                connectionStatusText
            );

            return win;

            bool CheckAblyInitialized(MainLoop arg)
            {
                keyText.Text = _ablyKey;
                if (Ably is not null)
                {
                    connectionStatusText.Text = Ably.Connection.State.ToString();
                }

                return true;
            }
        }

        private static string _ablyKey = string.Empty;

        static string GetCurrentKey()
        {
            if (File.Exists(AblyKeyFileName))
            {
                return File.ReadAllText(AblyKeyFileName);
            }

            return null;
        }

        static void SaveKey(string key)
        {
            File.WriteAllText(AblyKeyFileName, key, Encoding.UTF8);
        }

        static void Configure()
        {
            void InitialiseAbly(string key)
            {
                _ablyKey = key;
                var options = new ClientOptions(key)
                {
                    LogHandler = Logger,
                    LogLevel = LogLevel.Debug,
                    AutoConnect = false
                };
                Ably = new AblyRealtime(options);
            }

            var key = GetCurrentKey();
            if (string.IsNullOrWhiteSpace(key) == false)
            {
                InitialiseAbly(key);
                return;
            }

            var okButton = new Button("Ok", is_default: true);
            var cancelButton = new Button("Cancel");

            var d = new Dialog(
                "New File", 50, 20,
                okButton,
                cancelButton);

            var keyLabel = new Label("Ably Key: ") { X = 1, Y = 2 };
            var keyText = new TextField(_ablyKey)
            {
                X = Pos.Left(keyLabel),
                Y = Pos.Bottom(keyLabel) + 1,
                Width = 30
            };

            d.Add(keyLabel);
            d.Add(keyText);

            okButton.Clicked += OkButton;
            cancelButton.Clicked += () => { Application.RequestStop(); };

            Application.Run(d);

            void OkButton()
            {
                _ablyKey = keyText.Text.ToString();
                SaveKey(_ablyKey);
                InitialiseAbly(_ablyKey);
                Application.RequestStop();
            }

        }

        static Window HelloWindow()
        {
            var win = new Window("Hello")
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1
            };

            return win;
        }

        static void Quit()
        {
            var n = MessageBox.Query(50, 7, "Quit Demo", "Are you sure you want to quit this demo?", "Yes", "No");
            if (n == 0)
            {
                running = null;
                Application.Top.Running = false;
            }
        }

        static void Close()
        {
            MessageBox.ErrorQuery(50, 7, "Error", "There is nothing to close", "Ok");
        }

        public static IRealtimeClient Ably;

        private static MenuBar _menu = null;

        static MenuBar CreateMenu()
        {
            if (_menu is not null)
                return _menu;

            _menu = new MenuBar(new MenuBarItem[]
            {
                new MenuBarItem("Commands", new MenuItem[]
                {
                    new MenuItem("_Connect", "Connect Ably", () =>
                    {
                        Ably?.Connect();
                    }),
                    new MenuItem("_Disconnect", string.Empty, () =>
                    {
                        Ably?.Connection.Close();
                    }),
                    new MenuItem("_Logs", string.Empty, () =>
                    {
                        running = ShowLogsWindow;
                        Application.RequestStop();
                    }),
                    new MenuItem("_Quit", string.Empty, Quit)
                }),
                new MenuBarItem("_Messages", new MenuItem[]
                {
                    new MenuItem("_Send", string.Empty, () =>
                    {
                        HideWindows();
                        messageWindow.Visible = true;
                    }),
                })
            });

            return _menu;
        }

        static MenuBar CreateLogsMenu()
        {
            return new MenuBar(new MenuBarItem[]
            {
                new MenuBarItem("Commands", new MenuItem[]
                {
                    new MenuItem("_Back", string.Empty, () =>
                    {
                        running = InitApp;
                        Application.RequestStop();
                    }),
                    new MenuItem("_Quit", string.Empty, Quit)
                })
            });
        }

        public static Action running = InitApp;

        public static void ShowLogsWindow()
        {
            var top = Application.Top;
            logsWindow = CreateLogsWindow();
            top.Add(logsWindow, CreateLogsMenu());

            Application.Run();
        }

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.Default;

            Application.Init();
            Application.HeightAsBuffer = true;

            while (running != null)
            {
                running.Invoke();
            }

            Application.Shutdown();
        }

        private static Window mainWindow;
        private static Window helloWindow;
        private static Window logsWindow;
        private static Window messageWindow;

        private static Window[] windows;

        private static void HideWindows()
        {
            foreach (var window in windows)
            {
                window.Visible = false;
            }
        }

        static void InitApp()
        {
            var top = Application.Top;
            mainWindow = CreateMainWindow();
            helloWindow = HelloWindow();
            helloWindow.Visible = false;
            messageWindow = CreateMessagesWindow();
            messageWindow.Visible = false;
            windows = new[] { mainWindow, helloWindow, messageWindow };
            top.Add(mainWindow, helloWindow, messageWindow, CreateMenu());

            var timer = Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(1), loop =>
            {
                if (Ably is null)
                {
                    Configure();
                }

                return false;
            });
            Application.Run();
        }
    }
}