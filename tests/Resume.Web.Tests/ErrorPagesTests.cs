using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Resume.Core.Models;
using Resume.Web.Middleware;
using Resume.Web.Services;

namespace Resume.Web.Tests;

public sealed class ErrorPagesTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task NotFoundAtRoot_ReturnsClassicHtmlErrorPageWithRootLinks()
  {
    var ct = TestContext.Current.CancellationToken;
    var response = await _client.GetAsync(new Uri("/does-not-exist", UriKind.Relative), ct);
    var html = await response.Content.ReadAsStringAsync(ct);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    response.Headers.CacheControl!.NoStore.Should().BeTrue();
    html.Should().Contain("<h1>404</h1>");
    html.Should().Contain("Page Not Found");
    html.Should().Contain("prefers-color-scheme: dark");
    html.Should().Contain("href=\"/\"");
    html.Should().Contain("<a href=\"/pdf\" target=\"_blank\" rel=\"noopener noreferrer\">PDF</a>");
    html.Should().Contain("<a href=\"/txt\" target=\"_blank\" rel=\"noopener noreferrer\">Text</a>");
    html.Should().NotContain("href=\"/resume/pdf\"");
  }

  [Fact]
  public async Task NotFoundUnderResumeSubpath_ReturnsHtmlErrorPageWithSubpathLinks()
  {
    var ct = TestContext.Current.CancellationToken;
    var response = await _client.GetAsync(new Uri("/resume/does-not-exist", UriKind.Relative), ct);
    var html = await response.Content.ReadAsStringAsync(ct);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    response.Headers.CacheControl!.NoStore.Should().BeTrue();
    html.Should().Contain("<h1>404</h1>");
    html.Should().Contain("href=\"/resume\"");
    html.Should().Contain("<a href=\"/resume/pdf\" target=\"_blank\" rel=\"noopener noreferrer\">PDF</a>");
    html.Should().Contain("<a href=\"/resume/txt\" target=\"_blank\" rel=\"noopener noreferrer\">Text</a>");
    html.Should().NotContain("href=\"/pdf\"");
  }

  [Fact]
  public async Task MethodNotAllowed_Returns405HtmlErrorPage()
  {
    var ct = TestContext.Current.CancellationToken;
    using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/pdf", UriKind.Relative));
    var response = await _client.SendAsync(request, ct);
    var html = await response.Content.ReadAsStringAsync(ct);

    response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    response.Headers.CacheControl!.NoStore.Should().BeTrue();
    html.Should().Contain("<h1>405</h1>");
    html.Should().Contain("Method Not Allowed");
    html.Should().Contain("href=\"/\"");
    html.Should().NotContain("Formats:");
  }

  [Theory]
  [InlineData("application/json", "application/problem+json")]
  [InlineData("application/problem+json", "application/problem+json")]
  [InlineData("text/plain", "text/plain")]
  [InlineData("text/html", "text/html")]
  [InlineData("application/xhtml+xml", "text/html")]
  [InlineData("*/*", "text/html")]
  [InlineData("image/png", "text/html")]
  public async Task NotFound_NegotiatesFormatFromAcceptHeader(string accept, string expectedMediaType)
  {
    var ct = TestContext.Current.CancellationToken;
    using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/missing-page", UriKind.Relative));
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

    var response = await _client.SendAsync(request, ct);
    var body = await response.Content.ReadAsStringAsync(ct);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    response.Content.Headers.ContentType!.MediaType.Should().Be(expectedMediaType);

    if (expectedMediaType == "application/problem+json")
    {
      using var doc = JsonDocument.Parse(body);
      doc.RootElement.GetProperty("status").GetInt32().Should().Be(404);
      doc.RootElement.GetProperty("title").GetString().Should().Be("Page Not Found");
    }
    else if (expectedMediaType == "text/plain")
    {
      body.Should().StartWith("404 Page Not Found");
    }
    else
    {
      body.Should().Contain("<h1>404</h1>");
    }
  }

  [Fact]
  public async Task NotFound_RespectsAcceptQualityWeights()
  {
    var ct = TestContext.Current.CancellationToken;
    using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/missing-page", UriKind.Relative));
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain", 0.4));
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 0.9));

    var response = await _client.SendAsync(request, ct);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
  }

  [Fact]
  public async Task UnhandledException_Returns500ErrorPageWithoutLeakingDetails_AtRootAndSubpath()
  {
    var ct = TestContext.Current.CancellationToken;
    using var errorFactory = factory.WithWebHostBuilder(static builder =>
    {
      builder.ConfigureServices(static services =>
      {
        services.AddSingleton<IResumeService>(new ThrowingResumeService());
      });
    });
    using var client = errorFactory.CreateClient();

    var rootResponse = await client.GetAsync(new Uri("/", UriKind.Relative), ct);
    var rootHtml = await rootResponse.Content.ReadAsStringAsync(ct);
    rootResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    rootResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    rootResponse.Headers.CacheControl!.NoStore.Should().BeTrue();
    rootHtml.Should().Contain("<h1>500</h1>");
    rootHtml.Should().Contain("Internal Server Error");
    rootHtml.Should().Contain("href=\"/\"");
    rootHtml.Should().NotContain("sensitive internal boom");
    rootHtml.Should().NotContain("InvalidOperationException");

    var subpathResponse = await client.GetAsync(new Uri("/resume", UriKind.Relative), ct);
    var subpathHtml = await subpathResponse.Content.ReadAsStringAsync(ct);
    subpathResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    subpathHtml.Should().Contain("<h1>500</h1>");
    subpathHtml.Should().Contain("href=\"/resume\"");
    subpathHtml.Should().NotContain("sensitive internal boom");

    using var jsonRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("/pdf", UriKind.Relative));
    jsonRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    var jsonResponse = await client.SendAsync(jsonRequest, ct);
    var jsonBody = await jsonResponse.Content.ReadAsStringAsync(ct);
    jsonResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    jsonResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    jsonBody.Should().Contain("\"status\":500");
    jsonBody.Should().NotContain("sensitive internal boom");

    using var textRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("/pdf", UriKind.Relative));
    textRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
    var textResponse = await client.SendAsync(textRequest, ct);
    var textBody = await textResponse.Content.ReadAsStringAsync(ct);
    textResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    textResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
    textBody.Should().StartWith("500 Internal Server Error");
    textBody.Should().NotContain("sensitive internal boom");
  }

  [Fact]
  public async Task WriteErrorResponseAsync_HandlesUnknownStatusCodeFallback()
  {
    var ct = TestContext.Current.CancellationToken;
    var context = new DefaultHttpContext
    {
      RequestAborted = ct,
    };
    using var body = new MemoryStream();
    context.Response.Body = body;

    await ResumeErrorHandling.WriteErrorResponseAsync(context, 490);

    context.Response.StatusCode.Should().Be(490);
    var html = Encoding.UTF8.GetString(body.ToArray());
    html.Should().Contain("<h1>490</h1>");
    html.Should().Contain("<strong>Error</strong>");
  }

  private sealed class ThrowingResumeService : IResumeService
  {
    public string CommitSha => "abc1234";

    public RenderedResume GetWebHtml(string pathBase = "") => throw new InvalidOperationException("sensitive internal boom");

    public RenderedResume Get(ResumeFormat format) => throw new InvalidOperationException("sensitive internal boom");
  }
}
