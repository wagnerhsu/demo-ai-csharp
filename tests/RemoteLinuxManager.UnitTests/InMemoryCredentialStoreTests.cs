using RemoteLinuxManager.Infrastructure.SshNet.Security;

namespace RemoteLinuxManager.UnitTests;

public class InMemoryCredentialStoreTests
{
    [Fact]
    public async Task SaveAndGet_ReturnsStoredValue()
    {
        var store = new InMemoryCredentialStore();

        await store.SaveAsync("profile:password", "secret", CancellationToken.None);
        var value = await store.GetAsync("profile:password", CancellationToken.None);

        Assert.Equal("secret", value);
    }

    [Fact]
    public async Task Delete_RemovesStoredValue()
    {
        var store = new InMemoryCredentialStore();

        await store.SaveAsync("profile:password", "secret", CancellationToken.None);
        await store.DeleteAsync("profile:password", CancellationToken.None);

        var value = await store.GetAsync("profile:password", CancellationToken.None);

        Assert.Null(value);
    }
}
