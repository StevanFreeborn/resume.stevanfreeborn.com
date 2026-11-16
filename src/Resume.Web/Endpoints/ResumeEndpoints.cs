using Microsoft.Net.Http.Headers;

using Resume.Core.Models;
using Resume.Web.Services;

namespace Resume.Web.Endpoints;

internal static class ResumeEndpoints
{
  public static IEndpointRouteBuilder MapResumeEndpoints(this IEndpointRouteBuilder endpoints)
  {
    endpoints.MapGet("/", ServeRoot)
      .WithName("GetResumeHtmlPage");

    endpoints.MapGet(
      "/pdf",
      (HttpContext ctx, IResumeService resume, bool? download) => ServeArtifact(ctx, resume.Get(ResumeFormat.Pdf), forceDownload: download is true)
    );

    endpoints.MapGet(
      "/docx",
      (HttpContext ctx, IResumeService resume) => ServeArtifact(ctx, resume.Get(ResumeFormat.Docx), forceDownload: true)
    );

    endpoints.MapGet(
      "/md",
      (HttpContext ctx, IResumeService resume, bool? download) => ServeArtifact(ctx, resume.Get(ResumeFormat.Markdown), forceDownload: download is true)
    );

    endpoints.MapGet(
      "/html",
      (HttpContext ctx, IResumeService resume, bool? download) => ServeArtifact(ctx, resume.Get(ResumeFormat.Html), forceDownload: download is true)
    );

    endpoints.MapGet(
      "/json",
      (HttpContext ctx, IResumeService resume, bool? download) => ServeArtifact(ctx, resume.Get(ResumeFormat.Json), forceDownload: download is true)
    );

    endpoints.MapGet(
      "/txt",
      (HttpContext ctx, IResumeService resume, bool? download) => ServeArtifact(ctx, resume.Get(ResumeFormat.Text), forceDownload: download is true)
    );

    endpoints.MapGet(
      "/health",
      (IResumeService resume) => Results.Ok(new HealthResponse("healthy", resume.CommitSha))
    );

    return endpoints;
  }

  private static IResult ServeRoot(HttpContext context, IResumeService resume)
  {
    context.Response.Headers.Vary = HeaderNames.Accept;

    var artifact = NegotiateArtifact(context.Request, resume);
    var forceDownload = artifact.Format == ResumeFormat.Docx;
    return ServeArtifact(context, artifact, forceDownload);
  }

  private static RenderedResume NegotiateArtifact(HttpRequest request, IResumeService resume)
  {
    var pathBase = request.PathBase.ToString();
    var acceptHeaders = request.GetTypedHeaders().Accept;

    if (acceptHeaders is null || acceptHeaders.Count is 0)
    {
      return resume.GetWebHtml(pathBase);
    }

    var ordered = acceptHeaders
      .Where(static m => m.MediaType.HasValue)
      .OrderByDescending(static m => m.Quality ?? 1.0);

    foreach (var media in ordered)
    {
      var type = media.MediaType.Value!;

      if (
        type.Equals("text/html", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("*/*", StringComparison.OrdinalIgnoreCase)
      )
      {
        return resume.GetWebHtml(pathBase);
      }

      if (type.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
      {
        return resume.Get(ResumeFormat.Pdf);
      }

      if (type.Equals("text/markdown", StringComparison.OrdinalIgnoreCase))
      {
        return resume.Get(ResumeFormat.Markdown);
      }

      if (type.Equals("application/json", StringComparison.OrdinalIgnoreCase))
      {
        return resume.Get(ResumeFormat.Json);
      }

      if (type.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
      {
        return resume.Get(ResumeFormat.Text);
      }

      if (type.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase))
      {
        return resume.Get(ResumeFormat.Docx);
      }
    }

    return resume.GetWebHtml(pathBase);
  }

  private static IResult ServeArtifact(HttpContext context, RenderedResume artifact, bool forceDownload)
  {
    context.Response.Headers.ETag = artifact.ETag;
    context.Response.Headers.CacheControl = "public, max-age=300";

    var ifNoneMatch = context.Request.GetTypedHeaders().IfNoneMatch;

    if (
      ifNoneMatch.Count > 0 && 
      ifNoneMatch.Any(tag => tag.Equals(EntityTagHeaderValue.Any) || string.Equals(tag.Tag.Value, artifact.ETag, StringComparison.Ordinal))
    )
    {
      return Results.StatusCode(StatusCodes.Status304NotModified);
    }

    if (forceDownload)
    {
      return Results.File(artifact.Bytes, artifact.ContentType, artifact.FileName);
    }

    if (artifact.Format is ResumeFormat.Pdf)
    {
      context.Response.Headers.ContentDisposition = $"inline; filename=\"{artifact.FileName}\"";
    }

    return Results.Bytes(artifact.Bytes, artifact.ContentType);
  }

  private sealed record HealthResponse(string Status, string Commit);
}
