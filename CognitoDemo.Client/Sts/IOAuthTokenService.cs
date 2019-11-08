using System;
using System.Threading;
using System.Threading.Tasks;

namespace CognitoDemo.Client.Sts
{
    public interface IOAuthTokenService : IDisposable
    {
        Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken);
    }
}