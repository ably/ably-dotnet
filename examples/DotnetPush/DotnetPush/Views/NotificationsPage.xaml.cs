using DotnetPush.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DotnetPush.Views
{
    /// <summary>
    /// Page to show logs.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NotificationsPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Notifications"/> class.
        /// </summary>
        public NotificationsPage()
        {
            InitializeComponent();
        }
    }
}
