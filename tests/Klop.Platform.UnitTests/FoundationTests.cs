using Klop.Platform.Application.Abstractions;
using Xunit;

namespace Klop.Platform.UnitTests;

public sealed class FoundationTests
{
    [Fact]
    public void ApplicationClockContract_IsAnInterface() => Assert.True(typeof(ICurrentClock).IsInterface);
}
