using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using CognitoDemo.Client.Sts;
using CognitoDemo.Client.Utility;

namespace CognitoDemo.Client.Controllers
{
    public class HomeController : Controller
    {
        private static readonly IOAuthTokenService _oAuthTokenService;

        static HomeController()
        {
            var httpClientGovernor = new HttpClientGovernor();
            var clientId = @"";
            var clientSecret = @"";
            var scope = @"";
            var tokenEndpoint = @"";
            
            _oAuthTokenService = new CachingOAuthTokenServiceDecorator(
                new ReliableOAuthTokenServiceDecorator(
                    new OAuthTokenService(httpClientGovernor, clientId, clientSecret, scope, tokenEndpoint)));
        }

        // GET: Default
        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            var token = await _oAuthTokenService.GetTokenAsync(cancellationToken);
            ViewBag.Token = token;
            return View();
        }
    }
}