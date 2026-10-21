namespace Klop.Platform.Application.Abstractions;

public interface ICurrentClock
{
    DateTimeOffset UtcNow { get; }
}
