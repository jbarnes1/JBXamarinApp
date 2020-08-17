using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace JBDIAD
{

    public partial class App : Application
    {
        public static IPublicClientApplication PCA = null;

        public static string TenantID = "9d025ee6-a016-4374-a1d9-120b9005c55f";
        public static string ClientID = "f98b0892-d0d0-43bc-8320-39dada3de7d4";

        public static string[] Scopes = { "User.Read" };
        public static string Username = string.Empty;

        public static object ParentWindow { get; set; }


        public static IAuthenticate Authenticator { get; private set; }

        public static void Init(IAuthenticate authenticator)
        {
            Authenticator = authenticator;
        }

        //AUTH
        //https://docs.microsoft.com/en-us/azure/app-service-mobile/app-service-mobile-xamarin-forms-get-started-users
        public interface IAuthenticate
        {
            Task<bool> Authenticate();
        }

        public App()
        {
            InitializeComponent();
            PCA = PublicClientApplicationBuilder.Create(ClientID)
                      .WithAuthority(AzureCloudInstance.AzurePublic, TenantID)
                      .WithRedirectUri($"msal{ClientID}://auth")
                      .Build();

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
