using System;
using Microsoft.Extensions.Logging;
using Xunit;

namespace KunstButikken.Common.Logging.Tests;

public class LogMessagesTests
{
    [Fact]
    public void LoggerMessages_DefineDelegates_DoNotThrow()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("test");

        var args = new object[] { logger, "user", (Exception?)null };
        LogMessages.HttpRequestFailed(logger, "user", null);
        LogMessages.Warning_Msg_3001(logger, null);

        Assert.NotNull(LogMessages.HttpRequestFailed);
        Assert.NotNull(LogMessages.Warning_Msg_3001);
    }
}

