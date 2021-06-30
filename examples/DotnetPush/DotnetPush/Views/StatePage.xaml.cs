﻿using DotnetPush.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DotnetPush.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StatePage : ContentPage
    {
        public StatePage()
        {
            InitializeComponent();
            this.BindingContext = new StateViewModel((message) => DisplayAlert("Alert", message, "ok"));
        }
    }
}