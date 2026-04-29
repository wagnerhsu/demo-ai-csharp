using RemoteLinuxManager.Domain.Models;

namespace RemoteLinuxManager.UnitTests;

public class TransferProgressTests
{
    [Fact]
    public void Percentage_ReturnsZero_WhenTotalBytesIsZero()
    {
        var progress = new TransferProgress(200, 0);

        Assert.Equal(0, progress.Percentage);
    }

    [Fact]
    public void Percentage_ComputesExpectedValue()
    {
        var progress = new TransferProgress(50, 200);

        Assert.Equal(25, progress.Percentage);
    }
}
