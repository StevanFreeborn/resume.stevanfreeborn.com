using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

using Microsoft.Extensions.Caching.Memory;

using Resume.Core.Data;
using Resume.Core.Models;
using Resume.Core.Rendering;

namespace Resume.Web.Services;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by DI container")]
internal sealed class ResumeService(
  ResumeDocument document,
  IEnumerable<IResumeRenderer> renderers,
  IMemoryCache cache
) : IResumeService
{
  private const string BaseFileName = "stevan_freeborn_resume";
  private readonly Dictionary<ResumeFormat, IResumeRenderer> _renderers = renderers.ToDictionary(static r => r.Format);
  private readonly DateTimeOffset _startupTime = DateTimeOffset.UtcNow;

  public string CommitSha => GitMetadata.CommitSha;

  public RenderedResume GetWebHtml(string pathBase = "")
  {
    var normalizedBasePath = string.IsNullOrWhiteSpace(pathBase) ? string.Empty : pathBase.TrimEnd('/');

    return cache.GetOrCreate(
      $"resume:web-html:{normalizedBasePath}",
      _ => RenderArtifact(ResumeFormat.Html, includeWebToolbar: true, basePath: normalizedBasePath)
    ) ?? throw new InvalidOperationException("Failed to render web HTML resume.");
  }

  public RenderedResume Get(ResumeFormat format)
  {
    return cache.GetOrCreate($"resume:format:{format}", _ => RenderArtifact(format, includeWebToolbar: false, basePath: string.Empty))
      ?? throw new InvalidOperationException($"Failed to render resume format '{format}'.");
  }

  private RenderedResume RenderArtifact(ResumeFormat format, bool includeWebToolbar, string basePath)
  {
    if (_renderers.TryGetValue(format, out var renderer) is false)
    {
      throw new NotSupportedException($"No renderer registered for format '{format}'.");
    }

    var context = new RenderContext(
      CommitSha: CommitSha,
      GeneratedAtUtc: _startupTime,
      RepositoryUrl: StevanResume.RepositoryUrl,
      IncludeWebToolbar: includeWebToolbar,
      BasePath: basePath
    );

    var bytes = renderer.Render(document, context);
    var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));

    return new RenderedResume(
      Format: renderer.Format,
      RouteSegment: renderer.RouteSegment,
      ContentType: renderer.ContentType,
      FileName: $"{BaseFileName}{renderer.FileExtension}",
      Bytes: bytes,
      ETag: $"\"{hash[..16]}\""
    );
  }
}
