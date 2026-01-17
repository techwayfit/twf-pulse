using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using TechWayFit.Pulse.Application.Services;
using Xunit;

namespace TechWayFit.Pulse.Tests.Application.Services;

public class FileServiceTests
{
    [Fact]
    public async Task ReadFileAsync_Should_Return_Cached_Content_When_File_Removed()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new FileService(cache, NullLogger<FileService>.Instance);
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
        var expected = "cached content";

        await File.WriteAllTextAsync(filePath, expected);

        var firstRead = await service.ReadFileAsync(filePath);
        File.Delete(filePath);

        var secondRead = await service.ReadFileAsync(filePath);

        firstRead.Should().Be(expected);
        secondRead.Should().Be(expected);
    }
}
