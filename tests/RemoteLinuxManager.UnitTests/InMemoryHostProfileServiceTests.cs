using RemoteLinuxManager.Domain.Models;
using RemoteLinuxManager.Infrastructure.SshNet.Services;

namespace RemoteLinuxManager.UnitTests;

public class InMemoryHostProfileServiceTests
{
    [Fact]
    public async Task SaveAndGetAll_ReturnsProfilesSortedByName()
    {
        var service = new InMemoryHostProfileService();

        await service.SaveAsync(CreateProfile("beta"), CancellationToken.None);
        await service.SaveAsync(CreateProfile("alpha"), CancellationToken.None);

        var profiles = await service.GetAllAsync(CancellationToken.None);

        Assert.Collection(
            profiles,
            profile => Assert.Equal("alpha", profile.Name),
            profile => Assert.Equal("beta", profile.Name));
    }

    [Fact]
    public async Task Delete_RemovesProfileByName()
    {
        var service = new InMemoryHostProfileService();

        await service.SaveAsync(CreateProfile("target"), CancellationToken.None);
        await service.DeleteAsync("target", CancellationToken.None);

        var profiles = await service.GetAllAsync(CancellationToken.None);

        Assert.Empty(profiles);
    }

    private static HostProfile CreateProfile(string name)
    {
        return new HostProfile
        {
            Name = name,
            Host = "192.168.1.10",
            Port = 22,
            Username = "tester",
            AuthenticationType = AuthenticationType.Password,
            PasswordSecretId = $"{name}:password"
        };
    }
}
