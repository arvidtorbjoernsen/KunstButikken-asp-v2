using System;
using Xunit;
using KunstButikken.ServiceDefaults;

namespace KunstButikken.AppHost.Tests;

public class ResourceBuilderExceptionTests
{
    private class ThrowingEndpoint
    {
        public string GetEndpoint(string name) => throw new InvalidOperationException("boom");
    }

    [Fact]
    public void GetEndpoint_ReturnsNull_WhenReflectionInvocationThrows()
    {
        var t = new ThrowingEndpoint();
        // GetEndpoint uses reflection and catches TargetInvocationException and other exceptions
        var result = ResourceBuilderExtensions.GetEndpoint(t, "web");
        Assert.Null(result);
    }

    [Fact]
    public void GetEndpointString_ReturnsEmpty_WhenReflectionInvocationThrows()
    {
        var t = new ThrowingEndpoint();
        var result = ResourceBuilderExtensions.GetEndpointString(t, "web");
        Assert.Equal(string.Empty, result);
    }
}

