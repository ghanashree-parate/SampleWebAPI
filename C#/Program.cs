﻿// ================================================================================================
//  This file is part of the Microsoft Dynamics 365 SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft Development Tools and/or on-line 
//  documentation.  See these other materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR 
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR 
//  A PARTICULAR PURPOSE.
// ================================================================================================

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Crm.Sdk.Samples
{
 /// <summary>
 /// A basic Web API client application for Dynamics 365 (CRM). This sample authenticates 
 /// the user and then calls the WhoAmI Web API function. 
 /// </summary>
 /// <remarks> 
 /// Prerequisites: 
 ///   -  To run this application, you must have a CRM Online or on-premise account. 
 ///   -  For CRM Online or Internet-facing deployments (IFD), the application must be registered  
 ///      with Azure Active Directory as described in this article: 
 ///      https://msdn.microsoft.com/en-us/library/dn531010.aspx
 ///   
 /// The WhoAmI Web API function is documented here: 
 ///    https://msdn.microsoft.com/en-us/library/mt607925.aspx
 /// </remarks>
 static class SimpleWebApi
 {

  private static string serviceUrl = "https://trial0204.crm.dynamics.com/";   // CRM Online
  private static string userAccount = "<user-account>";  //CRM user account
  private static string domain = "<server-domain>";  //CRM server domain
  private static string clientId = "e898c59e-ddfc-497e-8377-894acdeffd83";     //e.g. "e5cf0024-a66a-4f16-85ce-99ba97a24bb2"
  private static string redirectUrl = "http://localhost:62916/";  //e.g. "http://localhost/SdkSample"

  static public void Main(string[] args)
  {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //One message handler for OAuth authentication, and the other for Windows integrated 
            // authentication.  (Assumes that HTTPS protocol only used for CRM Online.)
            HttpMessageHandler messageHandler;
   if (serviceUrl.StartsWith("https://"))
   {
    messageHandler = new OAuthMessageHandler(serviceUrl, clientId, redirectUrl,
             new HttpClientHandler());
   }
   else
   {
    //Prompt for user account password required for on-premise credentials.  (Better
    // approach is to use the SecureString class here.)
    Console.Write("Please enter the password for account {0}: ", userAccount);
    string password = Console.ReadLine().Trim();
    NetworkCredential credentials = new NetworkCredential(userAccount, password, domain);
    messageHandler = new HttpClientHandler() { Credentials = credentials };
   }
   try
   {
    //Create an HTTP client to send a request message to the CRM Web service.
    using (HttpClient httpClient = new HttpClient(messageHandler))
    {
     //Specify the Web API address of the service and the period of time each request 
     // has to execute.
     httpClient.BaseAddress = new Uri(serviceUrl);
     httpClient.Timeout = new TimeSpan(0, 2, 0);  //2 minutes

     //Send the WhoAmI request to the Web API using a GET request. 
     var response = httpClient.GetAsync("api/data/v8.1/WhoAmI",
             HttpCompletionOption.ResponseHeadersRead).Result;
     if (response.IsSuccessStatusCode)
     {
      //Get the response content and parse it.
      JObject body = JObject.Parse(response.Content.ReadAsStringAsync().Result);
      Guid userId = (Guid)body["UserId"];
      Console.WriteLine("Your system user ID is: {0}", userId);
     }
     else
     {
      Console.WriteLine("The request failed with a status of '{0}'",
             response.ReasonPhrase);
     }
    }
   }
   catch (Exception ex)
   {
    DisplayException(ex);
    throw;
   }
   finally
   {
    Console.WriteLine("Press <Enter> to exit the program.");
    Console.ReadLine();
   }
  }

  /// <summary> Displays exception information to the console. </summary>
  /// <param name="ex">The exception to output</param>
  private static void DisplayException(Exception ex)
  {
   Console.WriteLine("The application terminated with an error.");
   Console.WriteLine(ex.Message);
   while (ex.InnerException != null)
   {
    Console.WriteLine("\t* {0}", ex.InnerException.Message);
    ex = ex.InnerException;
   }
  }
 }

 /// <summary>
 ///Custom HTTP message handler that uses OAuth authentication thru ADAL.
 /// </summary>
 class OAuthMessageHandler : DelegatingHandler
 {
  private AuthenticationHeaderValue authHeader;

  public OAuthMessageHandler(string serviceUrl, string clientId, string redirectUrl,
          HttpMessageHandler innerHandler)
      : base(innerHandler)
  {
   // Obtain the Azure Active Directory Authentication Library (ADAL) authentication context.
   AuthenticationParameters ap = AuthenticationParameters.CreateFromResourceUrlAsync(
           new Uri(serviceUrl + "api/data/")).Result;
   AuthenticationContext authContext = new AuthenticationContext(ap.Authority, false);
   //Note that an Azure AD access token has finite lifetime, default expiration is 60 minutes.
   AuthenticationResult authResult = authContext.AcquireToken(serviceUrl, clientId, new Uri(redirectUrl), PromptBehavior.Always);
   authHeader = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
  }

  protected override Task<HttpResponseMessage> SendAsync(
           HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
  {
   request.Headers.Authorization = authHeader;
   return base.SendAsync(request, cancellationToken);
  }
 }
}


