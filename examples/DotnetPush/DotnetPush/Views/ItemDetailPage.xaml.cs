using DotnetPush.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace DotnetPush.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}