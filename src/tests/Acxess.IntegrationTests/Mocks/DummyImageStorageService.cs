using Acxess.Shared.Abstractions;
using Acxess.Shared.ResultManager;

namespace Acxess.IntegrationTests.Mocks;

public class DummyImageStorageService : IImageStorageService
{
    public Task<Result> DeleteImageAsync(string photoUrl, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<string>> SaveImageAsync(string base64Image, string fileName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result<string>.Success("https://test-cdn.com/dummy.webp"));
    }
}
