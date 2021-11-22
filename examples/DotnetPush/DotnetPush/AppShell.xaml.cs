using DotnetPush.ViewModels;
using DotnetPush.Views;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DotnetPush
{
    /// <inheritdoc />
    public partial class AppShell : Shell
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppShell"/> class.
        /// </summary>
        public AppShell()
        {
            InitializeComponent();
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            await Current.GoToAsync("//LogPage");
        }
    }
}
