using System;
using System.Threading;
using System.Threading.Tasks;

namespace CognitoDemo.Client.Sts
{
    public class CachingOAuthTokenServiceDecorator : IOAuthTokenService
    {
        private readonly IOAuthTokenService _oAuthTokenService;

        private bool _disposed;
        private AccessToken _accessToken;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public CachingOAuthTokenServiceDecorator(IOAuthTokenService oAuthTokenService)
        {
            if(oAuthTokenService == null)
                throw new ArgumentNullException("oAuthTokenService");

            _oAuthTokenService = oAuthTokenService;
        }

        public void Dispose()
        {
            _oAuthTokenService.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _oAuthTokenService.Dispose();
                _semaphore.Dispose();
            }

            _disposed = true;
        }

        public async Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken)
        {
            if (_accessToken != null && !_accessToken.IsExpired)
                return _accessToken;

            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_accessToken != null && !_accessToken.IsExpired)
                    return _accessToken;

                _accessToken = await _oAuthTokenService.GetTokenAsync(cancellationToken)
                    .ConfigureAwait(false);

                return _accessToken;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}