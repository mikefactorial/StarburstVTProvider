using System;
using System.Net.Http;

namespace Starburst.Plugins
{
    public class HttpClientWrapper
    {
        public HttpClientWrapper()
        {
        }
        public virtual string GetResponse(string uri, string credentialType, string authCredentials, HttpContent requestBody)
        {
            HttpMessageHandler handler = new HttpClientHandler();
            var httpClient = new HttpClient(handler)
            {
                //2 minute timeout
                BaseAddress = new Uri(uri),
                Timeout = new TimeSpan(0, 2, 0)
            };

            httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
            if (!string.IsNullOrEmpty(credentialType) && !string.IsNullOrEmpty(authCredentials))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"{credentialType} {authCredentials}");
            }

            HttpResponseMessage response = null;
            //make the call
            if (requestBody != null)
            {
                response = httpClient.PostAsync(uri, requestBody).GetAwaiter().GetResult();
            }
            else
            {
                response = httpClient.GetAsync(uri).GetAwaiter().GetResult();
            }
            if (!response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new Exception($"Error: {response.StatusCode} - {response.ReasonPhrase} - {content}");
            }
            //return the response
            if (response != null)
            {
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            return string.Empty;
        }
    }
}
