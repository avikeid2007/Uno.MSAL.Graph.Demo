﻿using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Uno.Extensions;
using Uno.UI.MSAL;
using Prompt = Microsoft.Identity.Client.Prompt;

namespace Uno.MSAL.Graph.Demo
{
    public sealed partial class MainPage : Page, IAuthenticationProvider
    {
        private const string CLIENT_ID = "a74f513b-2d8c-45c0-a15a-15e63f7a7862";
        private const string TENANT_ID = "6d53ef61-b6d1-4150-ae0b-43b90e75e0cd";

#if __WASM__
        private const string REDIRECT_URI = "http://localhost:5000/authentication/login-callback.htm";
#elif __IOS__ || __MACOS__
        private const string REDIRECT_URI = "msal" + CLIENT_ID + "://auth";
#elif __ANDROID__
        private const string REDIRECT_URI = "msauth://Uno.MSAL.Graph.Demo/BUWXtvbCbxw6rdZidSYhNH6gLvA%3D";
#else
        private const string REDIRECT_URI = "https://login.microsoftonline.com/common/oauth2/nativeclient";
#endif

        private readonly string[] SCOPES = new[] { "https://graph.microsoft.com/User.Read", "https://graph.microsoft.com/email", "https://graph.microsoft.com/profile" };

        private IPublicClientApplication _app;

        public MainPage()
        {
            this.InitializeComponent();

            _app = PublicClientApplicationBuilder
                .Create(CLIENT_ID)
                .WithTenantId(TENANT_ID)
                .WithRedirectUri(REDIRECT_URI)
                .WithUnoHelpers()
#if __IOS__
                .WithIosKeychainSecurityGroup("86AC3CZ5DN.com.microsoft.adalcache")
#endif
                .Build();
        }

        private async void SignIn(object sender, RoutedEventArgs e)
        {
            var result = await _app.AcquireTokenInteractive(SCOPES)
                .WithPrompt(Prompt.SelectAccount)
                .WithUnoHelpers()
                .ExecuteAsync();

            tokenBox.Text = result.AccessToken;
        }

        private async void LoadFromGraph(object sender, RoutedEventArgs e)
        {
#if __WASM__
            var http = new HttpClient(new Uno.UI.Wasm.WasmHttpHandler());
#else
            var http = new HttpClient();
#endif
            var client = new GraphServiceClient(http);
            client.AuthenticationProvider = this;

            var imageTask = client.Me.Photo.Content.Request().GetAsync();
            var nameTask = client.Me.Request().GetAsync();

            try
            {
                using (var stream = await imageTask)
                {
                    var bitmap = new BitmapImage();
#if HAS_UNO
                    bitmap.SetSource(new MemoryStream(stream.ReadBytes()));
#else
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
#endif
                    thumbnail.Source = bitmap;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
            }

            try
            {
                var me = await nameTask;

                name.Text = me.DisplayName;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
            }
        }

        public Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenBox.Text);
            return Task.CompletedTask;
        }
    }
}
