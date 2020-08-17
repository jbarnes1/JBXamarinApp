using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Microsoft.Azure.Devices.Client;
using System.Collections.ObjectModel;
using System.Threading;
using Xamarin.Forms.Xaml;
using System.Reflection;

namespace JBDIAD
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        // Track whether the user has authenticated.
        bool authenticated = false;

        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        // Either set the IOTHUB_DEVICE_CONN_STRING environment variable or within launchSettings.json:
        private static string DeviceConnectionString = "HostName=jbupsdiadiothub.azure-devices.net;DeviceId=JBTestDIAD1;SharedAccessKey=1ltsnEeW2C4+m9SBqdB2NnmNKA6blCgUbZ3uUDn9NbA=";

        private static int MESSAGE_COUNT = 5;
        private const int TEMPERATURE_THRESHOLD = 30;
        private static String deviceId = "JBTestDIAD1";
        private static float temperature;
        private static float humidity;
        private static Random rnd = new Random();
        private static TransportType s_transportType;
        private static DeviceClient s_deviceClient;

        public ObservableCollection<string> SentMessagesList = new ObservableCollection<string>();
        public ObservableCollection<string> ReceivedMessagesList = new ObservableCollection<string>();


        // Define an authenticated user.
        //private MobileServiceUser user;
        //public MobileServiceAuthenticationProvider context { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            deviceID.Text = deviceId;
            s_transportType = TransportType.Http1;
            s_deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, s_transportType);
        }
        [ContentProperty(nameof(Source))]
        public class ImageResourceExtension : IMarkupExtension
        {
            public string Source { get; set; }

            public object ProvideValue(IServiceProvider serviceProvider)
            {
                if (Source == null)
                {
                    return null;
                }

                // Do your translation lookup here, using whatever method you require
                var imageSource = ImageSource.FromResource(Source, typeof(ImageResourceExtension).GetTypeInfo().Assembly);

                return imageSource;
            }
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Refresh items only when authenticated.
            if (authenticated == true)
            {
                // Set syncItems to true in order to synchronize the data
                // on startup when running in offline mode.
                //                await RefreshItems(true, syncItems: false);
                var jb = 0;
                // Hide the Sign-in button.
           //     this.loginButton.IsVisible = false;
            }
        }

        async void loginButton_Clicked(object sender, EventArgs e)
        {
            if (App.Authenticator != null)
                authenticated = await App.Authenticator.Authenticate();

            // Set syncItems to true to synchronize the data on startup when offline is enabled.
            if (authenticated == true)
            {
                var jb1 = 0;
            };
            //       await RefreshItems(true, syncItems: false);
        }
        async void OnSignInSignOut(object sender, EventArgs e)
        {
            AuthenticationResult authResult = null;
            IEnumerable<IAccount> accounts = await App.PCA.GetAccountsAsync();
            try
            {
                if (btnSignInSignOut.Text == "Sign in")
                {
                    try
                    {
                        IAccount firstAccount = accounts.FirstOrDefault();
                        authResult = await App.PCA.AcquireTokenSilent(App.Scopes, firstAccount)
                                              .ExecuteAsync();
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        try
                        {
                            authResult = await App.PCA.AcquireTokenInteractive(App.Scopes)
                                                      .WithUseEmbeddedWebView(true)
                                                      .WithParentActivityOrWindow(App.ParentWindow)
                                                      .ExecuteAsync();
                        }
                        catch (Exception ex2)
                        {
                            await DisplayAlert("Acquire token interactive failed. See exception message for details: ", ex2.Message, "Dismiss");
                        }
                    }

                    if (authResult != null)
                    {
                        var content = await GetHttpContentWithTokenAsync(authResult.AccessToken);
                        UpdateUserContent(content);
                        Device.BeginInvokeOnMainThread(() => { btnSignInSignOut.Text = "Sign out"; });
                    }
                }
                else
                {
                    while (accounts.Any())
                    {
                        await App.PCA.RemoveAsync(accounts.FirstOrDefault());
                        accounts = await App.PCA.GetAccountsAsync();
                    }

                    slUser.IsVisible = false;
                    Device.BeginInvokeOnMainThread(() => { btnSignInSignOut.Text = "Sign in"; });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Authentication failed. See exception message for details: ", ex.Message, "Dismiss");
            }
        }

        private void UpdateUserContent(string content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                JObject user = JObject.Parse(content);

                slUser.IsVisible = true;

                Device.BeginInvokeOnMainThread(() =>
                {
                    lblDisplayName.Text = user["displayName"].ToString();
                //    lblGivenName.Text = user["givenName"].ToString();
                    lblId.Text = user["id"].ToString();
                //    lblSurname.Text = user["surname"].ToString();
                //    lblUserPrincipalName.Text = user["userPrincipalName"].ToString();

                    btnSignInSignOut.Text = "Sign out";
                });
            }
        }

        public async Task<string> GetHttpContentWithTokenAsync(string token)
        {
            try
            {
                //get data from API
                HttpClient client = new HttpClient();
                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
                message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage response = await client.SendAsync(message);
                string responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }
            catch (Exception ex)
            {
                await DisplayAlert("API call to graph failed: ", ex.Message, "Dismiss");
                return ex.ToString();
            }
        }

        private void Picker_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (Transport != null && Transport.SelectedIndex <= Transport.Items.Count)
            {
                var selecteditem = Transport.Items[Transport.SelectedIndex];
                switch (selecteditem)
                {
                    case "HTTP":
                        s_transportType = TransportType.Http1;
                        break;
                    case "MQTT":
                        s_transportType = TransportType.Mqtt;
                        break;
                    case "AMQP":
                        s_transportType = TransportType.Amqp;
                        break;
                }
                s_deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, s_transportType);
            }
        }
        async void SendEvent(object sender, EventArgs e)
        {
            string dataBuffer;

            Console.WriteLine("\n Device sending {0} messages to IoTHub...\n", MESSAGE_COUNT);
            SentMessagesList.Add(String.Format("Device sending {0} messages to IoTHub...\n", MESSAGE_COUNT));

            for (int count = 0; count < MESSAGE_COUNT; count++)
            {
                temperature = rnd.Next(20, 35);
                humidity = rnd.Next(60, 80);
                dataBuffer = string.Format("{{\"deviceId\":\"{0}\",\"messageId\":{1},\"temperature\":{2},\"humidity\":{3}}}", deviceId, count, temperature, humidity);
                Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                eventMessage.Properties.Add("temperatureAlert", (temperature > TEMPERATURE_THRESHOLD) ? "true" : "false");
                Console.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer);

                Device.BeginInvokeOnMainThread(() => {
                    SentMessagesList.Add(String.Format("> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), count, dataBuffer));
                    sentMessagesText.ItemsSource = SentMessagesList;
                });

                await s_deviceClient.SendEventAsync(eventMessage).ConfigureAwait(false);
            }
        }
        async void ReceiveCommands(object sender, EventArgs e)
        {
            Console.WriteLine("\nDevice waiting for commands from IoTHub...\n");
            ReceivedMessagesList.Add("Device waiting for commands from IoTHub...");
            Message receivedMessage;
            string messageData;

            while (true)
            {
                receivedMessage = await s_deviceClient.ReceiveAsync().ConfigureAwait(false);

                if (receivedMessage != null)
                {
                    messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Console.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);
                    Device.BeginInvokeOnMainThread(() => {
                        ReceivedMessagesList.Add(String.Format("Received message: {1}", DateTime.Now.ToLocalTime(), messageData));
                        receivedMessagesText.ItemsSource = ReceivedMessagesList;
                    });

                    int propCount = 0;
                    foreach (var prop in receivedMessage.Properties)
                    {
                        Console.WriteLine("\t\tProperty[{0}> Key={1} : Value={2}", propCount++, prop.Key, prop.Value);
                    }

                    await s_deviceClient.CompleteAsync(receivedMessage).ConfigureAwait(false);
                }

                //  Note: In this sample, the polling interval is set to 
                //  10 seconds to enable you to see messages as they are sent.
                //  To enable an IoT solution to scale, you should extend this //  interval. For example, to scale to 1 million devices, set 
                //  the polling interval to 25 minutes.
                //  For further information, see
                //  https://azure.microsoft.com/documentation/articles/iot-hub-devguide/#messaging
                Thread.Sleep(1000);
            }
        }
    }
}
