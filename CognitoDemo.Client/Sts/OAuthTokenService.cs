using CognitoDemo.Client.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CognitoDemo.Client.Sts
{
    public class OAuthTokenService : IOAuthTokenService
    {
        private readonly IHttpClientGovernor _httpClientGovernor;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _scope;
        private readonly string _tokenEndpoint;

        private bool _disposed;

        public OAuthTokenService(IHttpClientGovernor httpClientGovernor, string clientId, string clientSecret, string scope, string tokenEndpoint)
        {
            if (httpClientGovernor == null)
                throw new ArgumentException("httpClientGovernor");
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("clientId");
            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new ArgumentException("clientSecret");
            if (string.IsNullOrWhiteSpace(scope))
                throw new ArgumentException("scope");
            if (string.IsNullOrWhiteSpace(tokenEndpoint))
                throw new ArgumentException("tokenEndpoint");

            _httpClientGovernor = httpClientGovernor;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _scope = scope;
            _tokenEndpoint = tokenEndpoint;
        }

        public void Dispose()
        {
            _httpClientGovernor.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                _httpClientGovernor.Dispose();

            _disposed = true;
        }

        public async Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken)
        {
            var authenticationHeaderValue =
                Convert.ToBase64String(Encoding.UTF8.GetBytes(_clientId + ":" + _clientSecret));

            var urlEncodedContent = new FormUrlEncodedContent(new KeyValuePair<string, string>[2]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", _scope)
            });

            HttpResponseMessage httpResponseMessage;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint))
            using (urlEncodedContent)
            {
                requestMessage.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", authenticationHeaderValue);
                requestMessage.Content = urlEncodedContent;
                httpResponseMessage = await _httpClientGovernor
                    .ExecuteAsync(httpClient => httpClient.SendAsync(requestMessage, cancellationToken), cancellationToken)
                    .ConfigureAwait(false);
            }

            httpResponseMessage.EnsureSuccessStatusCode();

            var responseContent = await httpResponseMessage.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
            var accessTokenResponse = JsonConvert.DeserializeObject<AccessTokenResponse>(responseContent);

            return new AccessToken(accessTokenResponse.AccessToken);
        }

        private class AccessTokenResponse
        {
            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("expires_in")]
            public string ExpiresIn { get; set; }
        }
    }
}