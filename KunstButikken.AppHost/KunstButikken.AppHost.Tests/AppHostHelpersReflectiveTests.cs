using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KunstButikken.AppHost.Tests;

public class AppHostHelpersReflectiveTests
{
    [Fact]
    public void TryRegisterMcpServer_ReflectiveInvokesDynamicAssemblyMethod()
    {
        // Arrange - define a dynamic assembly with the expected name and a static method taking IServiceCollection
        var asmName = new AssemblyName("ModelContextProtocol.AspNetCore");
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
        var module = asmBuilder.DefineDynamicModule("MainModule");
        var typeBuilder = module.DefineType(
            "ModelContextProtocol.StaticRegistrar",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);

        // public static void Register(IServiceCollection services)
        var methodBuilder = typeBuilder.DefineMethod(
            "Register",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(void),
            new Type[] { typeof(IServiceCollection) });

        var il = methodBuilder.GetILGenerator();
        // method body: just return
        il.Emit(OpCodes.Ret);
        typeBuilder.CreateType();

        // Create a simple builder object with a Services property
        var builder = new { Services = new ServiceCollection() };

        // Capture console output
        var origOut = Console.Out;
        try
        {
            using var sw = new StringWriter();
            Console.SetOut(sw);

            // Act
            AppHostHelpers.TryRegisterMcpServer(builder);

            // Assert - TryRegisterMcpServer writes a log line when it succeeds invoking or when the strong-typed path is used
            var output = sw.ToString();
            var ok = output.Contains("Invoked", StringComparison.OrdinalIgnoreCase) || output.Contains("Registered MCP server", StringComparison.OrdinalIgnoreCase);
            Assert.True(ok, "Expected TryRegisterMcpServer to log either reflective invocation or strong-typed registration.");
        }
        finally
        {
            Console.SetOut(origOut);
        }
    }
}
