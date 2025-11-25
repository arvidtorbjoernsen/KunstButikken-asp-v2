using Microsoft.Extensions.DependencyInjection;

namespace KunstButikken.ServiceDefaults;

public static class EnvLoaderServiceCollectionExtensions
{
  /// <summary>
  ///   Register the default <see cref="IEnvLoader" /> implementation so tests and other components
  ///   can obtain it via DI. This is optional; existing static calls to EnvLoader.LoadEnv() will still work.
  /// </summary>
  public static IServiceCollection AddEnvLoader(this IServiceCollection services)
  {
    services.AddSingleton<IEnvLoader, EnvLoaderCore>();
    return services;
  }
}