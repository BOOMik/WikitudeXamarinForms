﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Geolocator.Plugin;
using Wikitude.Demo.Model;
using WikitudeXamarinForms.Controls;
using WikitudeXamarinForms.Services;
using Xamarin.Forms;
using Position = Geolocator.Plugin.Abstractions.Position;

namespace WikitudeXamarinForms.Pages
{
    public partial class ArPage : ContentPage
    {
        private readonly IEnumerable<PoiModel> _pois;
        private bool _initDone;

        public ArPage(IEnumerable<PoiModel> pois)
        {
            InitializeComponent();
            Debug.WriteLine("new ArPage");
            _pois = pois;

            Title = "AR";
            _initDone = false;
           // Open.Command = new Command(() => Navigation.PushAsync(new ArPage(App.Points)));
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!_initDone)
                await Task.Run(async () => await InitPage());
            Debug.WriteLine("OnAppearing");

        }

        protected override void OnDisappearing()
        {
            ArViewer.State = false;
            _initDone = false;
            Debug.WriteLine("OnDisappearing");
            DependencyService.Get<IPlatformService>().HideLoading();
            base.OnDisappearing();
        }

        private async Task InitPage()
        {
            _initDone = true;

            if (_pois == null) return;

            var supported = await ArViewer.SupportedTask.Task;
            Debug.WriteLine(supported ? "Supported" : "UnSupported");
            if (!supported)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Navigation.PopAsync(true);
                    Application.Current.MainPage.DisplayAlert("Localization.Error_Title",
                        "Localization.Error_AR_Unsupported",
                        "Localization.Error_Btn_Ok");
                });
                return;
            }

            DependencyService.Get<IPlatformService>().ShowLoading("Detect Location");

            Position position = null;

            ArViewer.State = true;

            try
            {
                position = await CrossGeolocator.Current.GetPositionAsync(20000);
            }
            catch (Exception ignoredException)
            {
                Debug.WriteLine(ignoredException.Message);
            }

            if (position == null)
            {

                Device.BeginInvokeOnMainThread(() =>
                {
                    DependencyService.Get<IPlatformService>().HideLoading();
                    Application.Current.MainPage.DisplayAlert("Error", "Location error", "Ok");
                });
                return;
            }
            ArViewer.Position = position;
            Debug.WriteLine("Current pos = " + position.Latitude + ", " + position.Longitude+" / "+position.Altitude);
            var pois = (from poi in _pois let distance = poi.DistanceTo(position.Latitude, position.Longitude) where distance <= 10000 select poi).ToList();

            await Task.Delay(500);
            if (pois.Count == 0)
            {
                Debug.WriteLine("No objects");
                Device.BeginInvokeOnMainThread(() =>
                {
                    DependencyService.Get<IPlatformService>().HideLoading();
                    Application.Current.MainPage.DisplayAlert("Error", "No objects", "Ok");

                });
                return;
            }

            ArViewer.Items = pois.OrderBy(p => p.NumberDistance);
            ArViewer.ItemClickedCommand = new Command(ArItemClickedCommandExecute);

            Debug.WriteLine("Page Init");
            DependencyService.Get<IPlatformService>().HideLoading();
        }

        private void ArItemClickedCommandExecute(object o)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Debug.WriteLine("Clicked");
                Navigation.PushAsync(new ContentPage
                {
                    Title = "Main in stack",

                    Content = new StackLayout
                    {
                        VerticalOptions = LayoutOptions.Center,
                        Children =
                    {
                        new Label
                        {
                            XAlign = TextAlignment.Center,
                            Text = o.ToString()
                        },
                        new Button
                        {
                            Text = "Open Next Instance Wikitude",
                            Command = new Command(() => Navigation.PushAsync(new ArPage(App.Points)))
                        }
                    }
                    }
                });

            }); 
        }
    }
}
