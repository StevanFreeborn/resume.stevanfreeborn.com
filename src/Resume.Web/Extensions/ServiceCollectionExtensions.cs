using Microsoft.AspNetCore.HttpOverrides;

using Resume.Core.Extensions;
using Resume.Web.Services;

namespace Resume.Web.Extensions;

internal static class ServiceCollectionExtensions
{
  public static IServiceCollection AddResumeWeb(this IServiceCollection services)
  {
    services.AddMemoryCache();
    services.AddResumeCore();
    services.AddSingleton<IResumeService, ResumeService>();

    services.Configure<ForwardedHeadersOptions>(static options =>
    {
      options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
      options.KnownIPNetworks.Clear();
      options.KnownProxies.Clear();
    });

    return services;
  }
}
