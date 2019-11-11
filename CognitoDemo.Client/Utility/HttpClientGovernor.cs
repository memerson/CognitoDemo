using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CognitoDemo.Client.Utility
{
    /// <summary>
    /// Strikes a balance between optimizing socket and connection reuse
    /// while also avoiding stale DNS data. The best we can do without access
    /// to HttpClientFactory of .NET Core.
    /// </summary>
    public interface IHttpClientGovernor : IDisposable
    {
        Task<T> ExecuteAsync<T>(Func<HttpClient, Task<T>> action, CancellationToken cancellationToken);
    }

    public class HttpClientGovernor : IHttpClientGovernor
    {
        private HttpClient _httpClient;
        private DateTime _lastExpirationTime = DateTime.MinValue;
        private bool _disposed, _disposing;

        private readonly ConcurrentQueue<HttpClientToCleanUp> _cleanupQueue
            = new ConcurrentQueue<HttpClientToCleanUp>();

        private readonly SemaphoreSlim _cleanupSemaphore = new SemaphoreSlim(1,1);
        private readonly SemaphoreSlim _expirationSemaphore = new SemaphoreSlim(1, 1);

        private static readonly TimeSpan HttpClientLifespan = TimeSpan.FromMinutes(2D);
        private static readonly TimeSpan HttpClientGracePeriod = TimeSpan.FromSeconds(15D);

        public void Dispose()
        {
            _disposing = true;

            _httpClient.Dispose();

            foreach(var x in _cleanupQueue)
                x.Cleanup();

            _disposed = true;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _disposing = true;

                _httpClient.Dispose();

                foreach (var x in _cleanupQueue)
                    x.Cleanup();
            }

            _disposed = true;
        }

        public async Task<T> ExecuteAsync<T>(Func<HttpClient, Task<T>> action, CancellationToken cancellationToken)
        {
            CheckDisposed();
            await ManageHttpClientsAsync(cancellationToken).ConfigureAwait(false);
            return await action.Invoke(_httpClient).ConfigureAwait(false);
        }

        private void CheckDisposed()
        {
            if(_disposing || _disposed)
                throw new ObjectDisposedException(@"HttpClientGovernor");
        }

        private async Task ManageHttpClientsAsync(CancellationToken cancellationToken)
        {
            await ExpireCurrentHttpClientAsync(cancellationToken).ConfigureAwait(false);
            await CleanupExpiredHttpClientsAsync(cancellationToken).ConfigureAwait(false);

        }

        private async Task ExpireCurrentHttpClientAsync(CancellationToken cancellationToken)
        {
            if (_httpClient == null)
            {
                _httpClient = new HttpClient();
                return;
            }

            if (!IsHttpClientExpired)
                return;

            await _expirationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!IsHttpClientExpired)
                    return;

                var cleanupTime = DateTime.Now + HttpClientGracePeriod;

                CheckDisposed();
                _cleanupQueue.Enqueue(new HttpClientToCleanUp(_httpClient, cleanupTime));
                _httpClient = new HttpClient();
                _lastExpirationTime = DateTime.Now;
            }
            finally
            {
                _expirationSemaphore.Release();
            }
        }

        private bool IsHttpClientExpired
        {
            get { return DateTime.Now > _lastExpirationTime + HttpClientLifespan; }
        }

        private async Task CleanupExpiredHttpClientsAsync(CancellationToken cancellationToken)
        {
            var gotLock = await _cleanupSemaphore.WaitAsync(0, cancellationToken)
                .ConfigureAwait(false);

            if (!gotLock)
                return;

            try
            {
                CleanupExpiredHttpClients();
            }
            finally
            {
                _cleanupSemaphore.Release();
            }
        }

        private void CleanupExpiredHttpClients()
        {
            while (true)
            {
                HttpClientToCleanUp httpClientToCleanUp;
                var emptyQueue = !_cleanupQueue.TryPeek(out httpClientToCleanUp);

                if (emptyQueue)
                    return;

                if (!httpClientToCleanUp.ShouldCleanUp)
                    return;

                httpClientToCleanUp.Cleanup();
                _cleanupQueue.TryDequeue(out httpClientToCleanUp);
            }
        }

        private class HttpClientToCleanUp
        {
            private readonly HttpClient _httpClient;
            private readonly DateTime _cleanupTime;

            public HttpClientToCleanUp(HttpClient httpClient, DateTime cleanupTime)
            {
                _httpClient = httpClient;
                _cleanupTime = cleanupTime;
            }

            public bool ShouldCleanUp
            {
                get { return DateTime.Now > _cleanupTime; }
            }

            public void Cleanup()
            {
                _httpClient.Dispose();
            }
        }
    }
}