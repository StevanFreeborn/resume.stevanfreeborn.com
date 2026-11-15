using Microsoft.Extensions.DependencyInjection;

using Resume.Core.Data;
using Resume.Core.Rendering;

namespace Resume.Core.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddResumeCore(this IServiceCollection services)
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddSingleton(StevanResume.Default);
    services.AddSingleton<IResumeRenderer, PdfResumeRenderer>();
    services.AddSingleton<IResumeRenderer, DocxResumeRenderer>();
    services.AddSingleton<IResumeRenderer, HtmlResumeRenderer>();
    services.AddSingleton<IResumeRenderer, MarkdownResumeRenderer>();
    services.AddSingleton<IResumeRenderer, JsonResumeRenderer>();
    services.AddSingleton<IResumeRenderer, PlainTextResumeRenderer>();

    return services;
  }
}
