using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using CognitoDemo.Client.Sts;
using CognitoDemo.Client.Utility;

namespace CognitoDemo.Client.Controllers
{
    public class HomeController : Controller
    {
        private static readonly IHttpClientGovernor _httpClientGovernor;
        private static readonly IOAuthTokenService _oAuthTokenService;

        static HomeController()
        {
            // This stuff should come from config and get set up
            // as part of the app start-up (IoC config, etc...)

            _httpClientGovernor = new HttpClientGovernor();
            var clientId = @"362b88659454lt0g8b3ontlr7e";
            var clientSecret = @""; // Ask me for this.
            var scope = @"Default/STS";
            var tokenEndpoint = @"https://95be0fd7-c5a6-4581-967e-27a82f7abdc1.auth.us-east-1.amazoncognito.com/oauth2/token";

            _oAuthTokenService = new CachingOAuthTokenServiceDecorator(
                new ReliableOAuthTokenServiceDecorator(
                    new OAuthTokenService(_httpClientGovernor, clientId, clientSecret, scope, tokenEndpoint)));
        }

        // GET: Default
        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            var accessToken = await _oAuthTokenService.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            ViewBag.Token = accessToken;

            var dateTime = await GetDateTimeFromWebService(accessToken, cancellationToken).ConfigureAwait(false);
            ViewBag.Result = dateTime;

            return View();
        }

        private async Task<DateTime> GetDateTimeFromWebService(AccessToken accessToken, CancellationToken cancellationToken)
        {
            HttpResponseMessage httpResponseMessage;
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, @"http://localhost:34943/api/DateTime"))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);
                httpResponseMessage = await _httpClientGovernor
                    .ExecuteAsync(httpClient => httpClient.SendAsync(requestMessage, cancellationToken), cancellationToken)
                    .ConfigureAwait(false);
            }

            httpResponseMessage.EnsureSuccessStatusCode();

            var responseContent = await httpResponseMessage.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            return DateTime.Parse(responseContent);
        }
    }
}