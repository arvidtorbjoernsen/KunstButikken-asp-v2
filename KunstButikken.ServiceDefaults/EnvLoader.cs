using DotNetEnv;

namespace KunstButikken.ServiceDefaults;

/// <summary>
///   Interface for loading environment variables from a .env file.
///   Extracted to make the behavior testable and injectable.
/// </summary>
public interface IEnvLoader
{
  void LoadEnv();
}

/// <summary>
///   Concrete injectable implementation of <see cref="IEnvLoader" />.
///   Contains the original .env loading logic.
/// </summary>
public sealed class EnvLoaderCore : IEnvLoader
{
  public void LoadEnv()
  {
    try
    {
      var candidates = new[]
      {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(AppContext.BaseDirectory, "..", ".env"),
        Path.Combine(AppContext.BaseDirectory, ".env")
      };
      var envPath = candidates.FirstOrDefault(File.Exists);
      if (!string.IsNullOrEmpty(envPath))
      {
        Env.Load(envPath);
        Console.WriteLine($"Loaded environment variables from {envPath}");
      }
      else
      {
        // Try default load which looks in the current directory
        try
        {
          Env.Load();
        }
        catch (IOException ex) // Catch specific exception for file operations
        {
          Console.WriteLine($"Warning: failed to load .env via DotNetEnv: {ex.Message}");
        }
        catch (Exception ex) // Catch other potential exceptions during Env.Load()
        {
          Console.WriteLine($"Warning: unexpected error loading .env via DotNetEnv: {ex.Message}");
        }
      }
    }
    catch (IOException ex) // Catch specific exception for file operations
    {
      Console.WriteLine($"Warning: failed to load .env via DotNetEnv: {ex.Message}");
    }
    catch (Exception ex) // Catch other potential exceptions during candidate path generation
    {
      Console.WriteLine($"Warning: unexpected error during .env path resolution: {ex.Message}");
    }
  }
}

/// <summary>
///   Backwards-compatible static shim. Existing callsites that call <c>EnvLoader.LoadEnv()</c>
///   continue to work; they delegate to the injectable implementation above. Tests can
///   instead construct or register <see cref="IEnvLoader" /> and call its instance method.
/// </summary>
public static class EnvLoader
{
  public static void LoadEnv()
  {
    var impl = new EnvLoaderCore();
    impl.LoadEnv();
  }
}