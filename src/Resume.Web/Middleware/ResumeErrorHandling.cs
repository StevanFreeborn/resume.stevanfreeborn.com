using System.Globalization;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.WebUtilities;

using Resume.Core.Data;
using Resume.Core.Models;
using Resume.Core.Rendering;

namespace Resume.Web.Middleware;

internal static class ResumeErrorHandling
{
  private static readonly CultureInfo s_inv = CultureInfo.InvariantCulture;

  private static readonly JsonSerializerOptions s_jsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  public static WebApplication UseResumeErrorPages(this WebApplication app)
  {
    ArgumentNullException.ThrowIfNull(app);

    app.UseExceptionHandler(static errorApp =>
    {
      errorApp.Run(static context => WriteErrorResponseAsync(context, StatusCodes.Status500InternalServerError));
    });

    app.UseStatusCodePages(static statusCodeContext =>
      WriteErrorResponseAsync(statusCodeContext.HttpContext, statusCodeContext.HttpContext.Response.StatusCode));

    return app;
  }

  internal static async Task WriteErrorResponseAsync(HttpContext context, int statusCode)
  {
    ArgumentNullException.ThrowIfNull(context);

    context.Response.StatusCode = statusCode;
    context.Response.Headers.CacheControl = "no-store";

    var (title, detail) = GetErrorCopy(statusCode);

    switch (NegotiateFormat(context.Request))
    {
      case ErrorResponseFormat.Json:
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        var payload = JsonSerializer.SerializeToUtf8Bytes(new ErrorPayload(statusCode, title, detail), s_jsonOptions);
        await context.Response.Body.WriteAsync(payload, context.RequestAborted);
        break;
      case ErrorResponseFormat.PlainText:
        context.Response.ContentType = "text/plain; charset=utf-8";
        var text = string.Create(s_inv, $"{statusCode} {title}\n{detail}\n");
        await context.Response.WriteAsync(text, Encoding.UTF8, context.RequestAborted);
        break;
      default:
        context.Response.ContentType = "text/html; charset=utf-8";
        var html = RenderErrorHtml(statusCode, title, detail, context.Request.PathBase.ToString());
        await context.Response.WriteAsync(html, Encoding.UTF8, context.RequestAborted);
        break;
    }
  }

  internal static string RenderErrorHtml(int statusCode, string title, string detail, string pathBase)
  {
    var normalizedBase = pathBase.TrimEnd('/');
    var homeHref = string.IsNullOrEmpty(normalizedBase) ? "/" : normalizedBase;
    var statusText = statusCode.ToString(s_inv);
    var encodedHomeHref = ResumeHtmlFragments.Encode(homeHref);
    var encodedTitle = ResumeHtmlFragments.Encode(title);
    var encodedDetail = ResumeHtmlFragments.Encode(detail);
    var encodedName = ResumeHtmlFragments.Encode(StevanResume.Default.Header.Name);
    var formatBarSection = statusCode == StatusCodes.Status404NotFound
      ? $"\n    <p>{ResumeHtmlFragments.RenderFormatLinks(normalizedBase)}</p>"
      : string.Empty;

    var footerHtml = ResumeHtmlFragments.RenderFooter(GitMetadata.CommitSha, StevanResume.RepositoryUrl);

    return $$"""
      <!DOCTYPE html>
      <html lang="en">
      <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>{{statusText}} - {{encodedTitle}} | {{encodedName}}</title>
        <style>
      {{ResumeHtmlFragments.Stylesheet}}
        </style>
      </head>
      <body class="error-page">
        <main>
          <h1>{{statusText}}</h1>
          <p><strong>{{encodedTitle}}</strong></p>
          <p>{{encodedDetail}}</p>
          <p><a href="{{encodedHomeHref}}">View Resume</a></p>{{formatBarSection}}
        </main>
      {{footerHtml}}
      </body>
      </html>
      """;
  }

  private static (string Title, string Detail) GetErrorCopy(int statusCode)
  {
    return statusCode switch
    {
      StatusCodes.Status404NotFound => ("Page Not Found", "The requested page or format could not be found."),
      >= StatusCodes.Status500InternalServerError => ("Internal Server Error", "An unexpected error occurred while processing your request. Please try again later."),
      _ => (ResolveFallbackTitle(statusCode), "The request could not be completed."),
    };
  }

  private static string ResolveFallbackTitle(int statusCode)
  {
    var phrase = ReasonPhrases.GetReasonPhrase(statusCode);
    return string.IsNullOrEmpty(phrase) ? "Error" : phrase;
  }

  private static ErrorResponseFormat NegotiateFormat(HttpRequest request)
  {
    var acceptHeaders = request.GetTypedHeaders().Accept;

    if (acceptHeaders is null || acceptHeaders.Count is 0)
    {
      return ErrorResponseFormat.Html;
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
        return ErrorResponseFormat.Html;
      }

      if (
        type.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
        type.Equals("application/problem+json", StringComparison.OrdinalIgnoreCase)
      )
      {
        return ErrorResponseFormat.Json;
      }

      if (type.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
      {
        return ErrorResponseFormat.PlainText;
      }
    }

    return ErrorResponseFormat.Html;
  }

  private sealed record ErrorPayload(int Status, string Title, string Detail);

  private enum ErrorResponseFormat
  {
    Html,
    Json,
    PlainText,
  }
}
