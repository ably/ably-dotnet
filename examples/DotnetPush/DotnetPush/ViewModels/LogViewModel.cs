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
    public class LogViewModel : BaseViewModel
    {
        public AppLoggerSink LoggerSync => DependencyService.Get<AppLoggerSink>();

        public bool ShowOnlyStateMachineLogs { get; set; } = false;
        public Command LoadItemsCommand { get; }
        public Command FilterItemsCommand { get; }

        public LogViewModel()
        {
            LogEntries = new ObservableCollection<LogEntry>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            FilterItemsCommand = new Command(async () =>
            {
                ShowOnlyStateMachineLogs = !ShowOnlyStateMachineLogs;
                await ExecuteLoadItemsCommand();
            });
        }

        public ObservableCollection<LogEntry> LogEntries { get; set; }

        private async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                LogEntries.Clear();
                foreach (var item in LoggerSync.Messages)
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
        }

        public void OnAppearing()
        {
            IsBusy = true;
        }
    }
}
