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
    /// Page to display the phone's Ably RealtimePush state.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StatePage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatePage"/> class.
        /// </summary>
        public StatePage()
        {
            InitializeComponent();
            BindingContext = new StateViewModel((message) => DisplayAlert("Alert", message, "ok"));
        }
    }
}
