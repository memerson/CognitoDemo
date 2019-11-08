using System;
using System.Linq;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CognitoDemo.Client.Sts
{
    public class AccessToken
    {
        private static readonly JsonWebTokenHandler JwtHandler = new JsonWebTokenHandler();
        private const double ExpirationCutoffSeconds = 1D;

        public AccessToken(string accessToken)
        {
            if(string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("accessToken");

            Value = accessToken;
        }

        public string Value { get; private set; }
        
        public bool IsExpired
        {
            get { return HasTokenExpired(Value); }
        }

        private static bool HasTokenExpired(string token)
        {
            var jsonWebToken = JwtHandler.ReadToken(token) as JsonWebToken;

            if (jsonWebToken == null)
                return true;

            var expirationClaim = jsonWebToken.Claims.FirstOrDefault(x => x.Type == "exp");

            if (expirationClaim == null)
                return false;

            var expiration = Convert.ToDouble(expirationClaim.Value);
            var now = GetSecondsSinceUnixEpoch(DateTime.Now);

            return now - expiration > ExpirationCutoffSeconds;
        }

        private static double GetSecondsSinceUnixEpoch(DateTimeOffset time)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return Math.Round((time - (DateTimeOffset)dateTime).TotalSeconds);
        }
    }
}