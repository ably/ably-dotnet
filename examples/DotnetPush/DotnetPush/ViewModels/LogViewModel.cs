using DotnetPush.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using DotnetPush.Models;
using Xamarin.Forms;

namespace DotnetPush.ViewModels
{
    /// <summary>
    /// View model for the Log page.
    /// </summary>
    public class LogViewModel : BaseViewModel
    {
        /// <summary>
        /// The current LoggerSink.
        /// </summary>
        public AppLoggerSink LoggerSink => DependencyService.Get<AppLoggerSink>();

        /// <summary>
        /// A flag used to filter the displayed logs. If `true` it will show logs only starting with ActivationStateMachine and
        /// if `false` it will show all AblyRealtime logs.
        /// Default: false.
        /// </summary>
        public bool ShowOnlyStateMachineLogs { get; set; } = false;

        /// <summary>
        /// Command to Load log entries.
        /// </summary>
        public Command LoadLogEntriesCommand { get; }

        /// <summary>
        /// Command to toggle ShowOnlyStateMachineLogs flag and reload the entries.
        /// </summary>
        public Command FilterItemsCommand { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogViewModel"/> class.
        /// </summary>
        public LogViewModel()
        {
            LogEntries = new ObservableCollection<LogEntry>();
            LoadLogEntriesCommand = new Command(async () => await ExecuteLoadItemsCommand());
            FilterItemsCommand = new Command(async () =>
            {
                ShowOnlyStateMachineLogs = !ShowOnlyStateMachineLogs;
                await ExecuteLoadItemsCommand();
            });
        }

        /// <summary>
        /// Observable collection of LogEntries.
        /// </summary>
        public ObservableCollection<LogEntry> LogEntries { get; set; }

        private Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                LogEntries.Clear();
                foreach (var item in LoggerSink.GetMessages())
                {
                    switch (ShowOnlyStateMachineLogs)
                    {
                        case true:
                        {
                            if (item.Message.Contains("ActivationStateMachine"))
                            {
                                LogEntries.Add(item);
                            }

                            break;
                        }

                        default:
                            LogEntries.Add(item);
                            break;
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Executed before the View is displayed.
        /// </summary>
        public void OnAppearing()
        {
            IsBusy = true;
        }
    }
}
