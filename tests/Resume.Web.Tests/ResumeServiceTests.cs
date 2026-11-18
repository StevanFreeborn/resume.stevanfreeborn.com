using System.Text;

using Microsoft.Extensions.Caching.Memory;

using Resume.Core.Data;
using Resume.Core.Models;
using Resume.Core.Rendering;
using Resume.Web.Services;

namespace Resume.Web.Tests;

public sealed class ResumeServiceTests
{
  [Fact]
  public void GetAndGetWebHtml_CacheRenderedOutputsPerFormatAndBasePath()
  {
    var renderer = new CountingRenderer(ResumeFormat.Html);
    using var cache = new MemoryCache(new MemoryCacheOptions());
    var sut = new ResumeService(StevanResume.Default, [renderer], cache);

    var web1 = sut.GetWebHtml();
    var web2 = sut.GetWebHtml();
    var subpath1 = sut.GetWebHtml("/resume");
    var subpath2 = sut.GetWebHtml("/resume/");
    var export1 = sut.Get(ResumeFormat.Html);
    var export2 = sut.Get(ResumeFormat.Html);

    web1.Should().BeSameAs(web2);
    subpath1.Should().BeSameAs(subpath2);
    subpath1.Should().NotBeSameAs(web1);
    export1.Should().BeSameAs(export2);
    Encoding.UTF8.GetString(web1.Bytes).Should().Be("toolbar=True;basePath=");
    Encoding.UTF8.GetString(subpath1.Bytes).Should().Be("toolbar=True;basePath=/resume");
    Encoding.UTF8.GetString(export1.Bytes).Should().Be("toolbar=False;basePath=");
    renderer.CallCount.Should().Be(3);
  }

  [Fact]
  public void Get_ThrowsNotSupportedWhenRendererMissing()
  {
    var renderer = new CountingRenderer(ResumeFormat.Html);
    using var cache = new MemoryCache(new MemoryCacheOptions());
    var sut = new ResumeService(StevanResume.Default, [renderer], cache);

    var act = () => sut.Get(ResumeFormat.Pdf);
    act.Should().Throw<NotSupportedException>();
  }

  private sealed class CountingRenderer(ResumeFormat format) : IResumeRenderer
  {
    public int CallCount { get; private set; }

    public ResumeFormat Format { get; } = format;

    public string ContentType => "text/html; charset=utf-8";

    public string FileExtension => ".html";

    public string RouteSegment => "html";

    public byte[] Render(ResumeDocument document, RenderContext context)
    {
      CallCount++;
      return Encoding.UTF8.GetBytes($"toolbar={context.IncludeWebToolbar};basePath={context.BasePath}");
    }
  }
}
