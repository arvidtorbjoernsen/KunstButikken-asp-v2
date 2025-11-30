using System;
using System.Reflection;
using Xunit;

namespace KunstButikken.AppHost.Tests;

public class ApplyKeycloakEnvironmentTests
{
    [Fact]
    public void ApplyKeycloakEnvironment_MethodExistsAndHasIResourceWithEnvironmentConstraint()
    {
        var appCompType = typeof(KunstButikken.AppHost.AppCompositionBuilder);
        var methods = appCompType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo? target = null;
        foreach (var m in methods)
        {
            if (m.Name == "ApplyKeycloakEnvironment" && m.IsGenericMethodDefinition)
            {
                target = m;
                break;
            }
        }
        Assert.NotNull(target);

        var genArgs = target!.GetGenericArguments();
        Assert.Single(genArgs);
        var ga = genArgs[0];
        var constraints = ga.GetGenericParameterConstraints();
        // Ensure at least one constraint mentions IResourceWithEnvironment (the exact type comes from Aspire)
        Assert.Contains(constraints, c => c.Name.Contains("IResourceWithEnvironment", StringComparison.OrdinalIgnoreCase));
    }
}
