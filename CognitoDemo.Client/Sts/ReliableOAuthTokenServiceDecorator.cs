using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;

namespace CognitoDemo.Client.Sts
{
    public class ReliableOAuthTokenServiceDecorator : IOAuthTokenService
    {
        private readonly IOAuthTokenService _oAuthTokenService;
        private readonly AsyncPolicy _circuitBreakerPolicy;
        private readonly AsyncPolicy _retryPolicy;

        private bool _disposed;

        public ReliableOAuthTokenServiceDecorator(IOAuthTokenService oAuthTokenService)
        {
            if (oAuthTokenService == null)
                throw new ArgumentNullException("oAuthTokenService");

            _oAuthTokenService = oAuthTokenService;
            _circuitBreakerPolicy = CreateCircuitBreakerPolicy();
            _retryPolicy = CreateWaitAndRetryPolicy();
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
                _oAuthTokenService.Dispose();

            _disposed = true;
        }

        public Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken)
        {
            return _retryPolicy.ExecuteAsync(
                ct1 => _circuitBreakerPolicy.ExecuteAsync(
                    ct2 => _oAuthTokenService.GetTokenAsync(ct2), ct1), cancellationToken);
        }

        private static AsyncPolicy CreateCircuitBreakerPolicy()
        {
            return Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 4,
                    durationOfBreak: TimeSpan.FromSeconds(10D));
        }

        private static AsyncPolicy CreateWaitAndRetryPolicy()
        {
            return Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException))
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100));
        }
    }
}