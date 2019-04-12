using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using static System.Console;

namespace ConnectToCrmWebApi
{
    class Program
    {

        /// <summary>
        /// Holds the Authentication context based on the Authentication URL
        /// </summary>
        static AuthenticationContext authContext;

        /// <summary>
        /// Holds the actual authentication token once after successful authentication
        /// </summary>
        static AuthenticationResult authToken;

        /// <summary>
        /// This is the API data url which we will be using to automatically get the
        ///  a) Resource URL - nothing but the CRM url
        ///  b) Authority URL - the Microsoft Azure URL related to our organization on to which we actually authenticate against
        /// </summary>
        static string apiUrl = "https://trial0204.crm.dynamics.com/api/data";

        /// <summary>
        /// Client ID or Application ID of the App registration in Azure
        /// </summary>
        static string clientId = "e898c59e-ddfc-497e-8377-894acdeffd83";


        /// <summary>
        /// The Redirect URL which we defined during the App Registration
        /// </summary>
        static string redirectUrl = "http://localhost:62916/";

        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            GetToken("ghanashree@trial0204.onmicrosoft.com","Admin@123");

            ReadLine();
        }

        internal static async void GetToken(string userId, string password)
        {
            // Get the Resource Url & Authority Url using the Api method. This is the best way to get authority URL
            // for any Azure service api.
            AuthenticationParameters ap = AuthenticationParameters.CreateFromResourceUrlAsync(new Uri(apiUrl)).Result;

            string resourceUrl = ap.Resource;
            string authorityUrl = ap.Authority;

            //Generate the Authority context .. For the sake of simplicity for the post, I haven't splitted these
            // in to multiple methods. Ideally, you would want to use some sort of design pattern to generate the context and store
            // till the end of the program.
            authContext = new AuthenticationContext(authorityUrl, false);

            // UserCrendetial object will only accept User Id
            //          starting from the latest version of .NET
            // Thats the reason why we are using UserPasswordCredential
            //         object which is actually inherited by UserCredential.
            UserCredential credentials =new UserPasswordCredential(userId, password);

            //Genertae the AuthToken by using Credentials object.
            authToken = await authContext.AcquireTokenAsync
            (resourceUrl, clientId, credentials);

            WriteLine("Got the authentication token, Getting data from Webapi !!");

            GetData(authToken.AccessToken);
        }

        internal static async void GetData(string token)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.Timeout = new TimeSpan(0, 2, 0);  // 2 minutes time out period.

                // Pass the Bearer token as part of request headers.
                httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);


                var data = await httpClient.GetAsync("https://trial0204.crm.dynamics.com/api/data/v9.1/accounts?$select=name");


                if (data.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // If the status code is success... then print the api output.
                    WriteLine(await data.Content.ReadAsStringAsync());
                }
                else
                {
                    // Failed .. ???
                    WriteLine($"Some thing went wrong with the data retrieval. Error code : {data.StatusCode} ");
                }
                ReadLine();

            }
        }
    }
}