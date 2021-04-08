namespace EnvioWorldsysSubcomercios.Stores
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Net.Cache;
    using System.Net.Http;
    using BatchProcess.Models;
    using GP.Core.WebClient;
    using INFINITUS.Client;
    using Newtonsoft.Json;

    /// <summary>
    /// Repositorio clientes REST.
    /// </summary>
    public static class RestClientStore
    {
        private static object syncRoot = new object();
        private static IINFINITUSClient infinitusClient;
        private static string token;

        /// <summary>
        /// Retorna una instancia singleton del cliente de INFINITUS.
        /// </summary>
        /// <param name="cliente">The cliente.</param>
        /// <returns>cliente INFINITUS.</returns>
        public static IINFINITUSClient InfinitusClient(ClienteRest cliente)
        {
            {
                if (infinitusClient == null)
                {
                    lock (syncRoot)
                    {
                        if (infinitusClient != null)
                        {
                            return infinitusClient;
                        }

                        var client = new INFINITUSClient(
                                        new Uri(cliente.Endpoint),
                                        // new TokenCredentials(tokenProvider),
                                        Token,
                                        new WebRequestHandler() { CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default) },
                                        new CorrelationHandler());

                        client.SetRetryPolicy(null);

                        infinitusClient = client;
                    }
                }

                return infinitusClient;
            }
        }

        /// <summary>
        /// Gets token obtenido para proceso.
        /// </summary>
        public static string Token
        {
            get
            {
                if (string.IsNullOrEmpty(token))
                {
                    token = GetToken();
                }

                return token;
            }
        }

        private static string GetToken()
        {
            HttpClient client = new HttpClient();
            var content = new List<KeyValuePair<string, string>>();
            content.Add(new KeyValuePair<string, string>("username", ConfigurationManager.AppSettings["keycloak-apiUser"]));
            content.Add(new KeyValuePair<string, string>("password", ConfigurationManager.AppSettings["keycloak-apiPasswd"]));
            content.Add(new KeyValuePair<string, string>("grant_type", "password"));
            content.Add(new KeyValuePair<string, string>("client_id", ConfigurationManager.AppSettings["keycloak-apiClientId"]));
            content.Add(new KeyValuePair<string, string>("client_secret", ConfigurationManager.AppSettings["keycloak-apiClientSecret"]));
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(ConfigurationManager.AppSettings["keycloak-url"] + "/realms/GlobalProcessing/protocol/openid-connect/token"));
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new FormUrlEncodedContent(content);
            HttpResponseMessage response = client.SendAsync(request).Result;
            string jsonContent = string.Empty;

            if (response.IsSuccessStatusCode)
            {
                jsonContent = response.Content.ReadAsStringAsync().Result;
            }

            var tkt = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
            client.Dispose();
            return tkt["access_token"];
        }
    }
}