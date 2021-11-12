using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DotnetPush.Views
{
    /// <summary>
    /// Page to show channels with subscriptions.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChannelsPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelsPage"/> class.
        /// </summary>
        public ChannelsPage()
        {
            InitializeComponent();
        }
    }
}
