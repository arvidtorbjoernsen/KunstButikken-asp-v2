using KunstButikken.ServiceDefaults;

namespace KunstButikken.AuctionService.Tests.TestDoubles;

public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public FakeDateTimeProvider(DateTimeOffset fixedUtcNow)
    {
        UtcNow = fixedUtcNow;
        Now = fixedUtcNow.ToLocalTime();
    }

    public DateTimeOffset UtcNow { get; }
    public DateTimeOffset Now { get; }
}

