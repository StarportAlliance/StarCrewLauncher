using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

/// <summary>NotificationService 的单元测试：验证事件转发与参数校验。</summary>
public sealed class NotificationServiceTests
{
    [Fact]
    public void Notify_WithSubscriber_RaisesRequestedWithMessage()
    {
        NotificationService service = new();
        NotificationMessage? received = null;
        service.Requested += (_, message) => received = message;

        service.Notify("标题", "正文", NotificationSeverity.Success);

        Assert.NotNull(received);
        Assert.Equal("标题", received.Title);
        Assert.Equal("正文", received.Message);
        Assert.Equal(NotificationSeverity.Success, received.Severity);
    }

    [Fact]
    public void Notify_WithoutSubscriber_DoesNotThrow()
    {
        NotificationService service = new();

        service.Notify("标题", "正文", NotificationSeverity.Informational);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Notify_WithBlankTitle_ThrowsArgumentException(string title)
    {
        NotificationService service = new();

        Assert.Throws<ArgumentException>(() =>
            service.Notify(title, "正文", NotificationSeverity.Informational)
        );
    }

    [Fact]
    public void Notify_WithNullMessage_ThrowsArgumentNullException()
    {
        NotificationService service = new();

        Assert.Throws<ArgumentNullException>(() =>
            service.Notify("标题", null!, NotificationSeverity.Informational)
        );
    }
}
