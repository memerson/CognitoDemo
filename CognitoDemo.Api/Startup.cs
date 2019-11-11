using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Jwt;
using Owin;

[assembly: OwinStartup(typeof(CognitoDemo.Api.Startup))]
namespace CognitoDemo.Api
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureOAuth(app);
        }

        public void ConfigureOAuth(IAppBuilder app)
        {
            // Get this from https://cognito-idp.{region}.amazonaws.com/{userPoolId}/.well-known/jwks.json
            // Pick the value of the "n" property for the key with the "kid" used in the JWT sent by the
            // client. This should be stored in config.
            var publicKey =
                @"ivT_bUYGFN-NoOfXROEjr0h0L1NOlLXHwoldiapQ8ZZ4Fq1SwYBnn09VQbHsz604l5t0r8s5fV9dJ6ZVYBvw8ScV84-KnqTCMWQlibwBlZl9fZ-XCSRbmcBiXtgaqK4tXDaj8GJnXvlOlNzuTBMlVHXWNDj4nXk6zPNsUgRwyaKL2V2rgUJWqCTE70Oq1TgAeqKIvYfgOIheb6yFYIK-tXH-88-jiWHSIYmjJXi7hiyDlCjcbFUPRvE6fnGhOtu6ueKnFI7rCasS6v2GZ_5UxemfGlVuClwPLvOR06wc4RSVqrqo16_m9u4wyjB7KgsCAmujU0osl76GxJjKk-VJOQ";

            // One or more Cognito client ids if only certain clients
            // are able to access this API (i.e. some clients who can get
            // valid tokens still shouldn't be able to access this API)
            var audiences = new[] {@"362b88659454lt0g8b3ontlr7e"};

            // The Cognito user pool URL.
            var issuer = @"https://cognito-idp.us-east-1.amazonaws.com/us-east-1_9buMk3Psj";

            app.UseJwtBearerAuthentication(
                new JwtBearerAuthenticationOptions
                {
                    AuthenticationMode = AuthenticationMode.Active,
                    TokenValidationParameters = GetTokenValidationParameters(publicKey, audiences, issuer)
                });
        }

        private TokenValidationParameters GetTokenValidationParameters(string publicKey, IEnumerable<string> audiences, string issuer)
        {
            var rsaParameters = new RSAParameters
            {
                Modulus = Base64UrlEncoder.DecodeBytes(publicKey),
                Exponent = Base64UrlEncoder.DecodeBytes("AQAB")
            };
            var rsa = new RSACryptoServiceProvider();

            rsa.ImportParameters(rsaParameters);

            var validationParameters = new TokenValidationParameters()
            {

                RequireExpirationTime = false,
                RequireSignedTokens = true,

                // Validate audiences only for situations where a known set of
                //  clients is authorized to use this API.
                ValidateAudience = false,
                ValidAudiences = audiences,

                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsa),

                // Check that the token is not expired, with some leeway
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(5)
            };

            return validationParameters;
        }
    }
}