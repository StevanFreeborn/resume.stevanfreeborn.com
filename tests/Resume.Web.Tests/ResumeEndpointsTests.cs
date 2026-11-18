using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Resume.Web.Tests;

public sealed class ResumeEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Theory]
  [InlineData("/", "text/html")]
  [InlineData("/pdf", "application/pdf")]
  [InlineData("/docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
  [InlineData("/md", "text/markdown")]
  [InlineData("/html", "text/html")]
  [InlineData("/json", "application/json")]
  [InlineData("/txt", "text/plain")]
  public async Task FormatRoute_ReturnsOkWithExpectedMediaTypeAndETag(string path, string expectedMediaType)
  {
    var ct = TestContext.Current.CancellationToken;
    var response = await _client.GetAsync(new Uri(path, UriKind.Relative), ct);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Content.Headers.ContentType!.MediaType.Should().Be(expectedMediaType);
    response.Headers.ETag.Should().NotBeNull();

    var bytes = await response.Content.ReadAsByteArrayAsync(ct);
    bytes.Should().NotBeEmpty();
  }

  [Theory]
  [InlineData("/pdf", "stevan_freeborn_resume.pdf")]
  [InlineData("/md", "stevan_freeborn_resume.md")]
  [InlineData("/html", "stevan_freeborn_resume.html")]
  [InlineData("/json", "stevan_freeborn_resume.json")]
  [InlineData("/txt", "stevan_freeborn_resume.txt")]
  public async Task FormatRoute_WithDownloadQueryFlag_ForcesAttachmentDisposition(string path, string expectedFileName)
  {
    var ct = TestContext.Current.CancellationToken;
    var response = await _client.GetAsync(new Uri($"{path}?download=true", UriKind.Relative), ct);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
    response.Content.Headers.ContentDisposition.FileName.Should().Be(expectedFileName);
  }

  [Fact]
  public async Task PdfRoute_SupportsInlinePreviewByDefault()
  {
    var ct = TestContext.Current.CancellationToken;
    var inlineResponse = await _client.GetAsync(new Uri("/pdf", UriKind.Relative), ct);
    inlineResponse.Content.Headers.ContentDisposition!.DispositionType.Should().Be("inline");
    var pdfBytes = await inlineResponse.Content.ReadAsByteArrayAsync(ct);
    Encoding.ASCII.GetString(pdfBytes, 0, 5).Should().Be("%PDF-");
  }

  [Fact]
  public async Task DocxRoute_ForcesAttachmentDisposition()
  {
    var ct = TestContext.Current.CancellationToken;
    var response = await _client.GetAsync(new Uri("/docx", UriKind.Relative), ct);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
    response.Content.Headers.ContentDisposition.FileName.Should().Be("stevan_freeborn_resume.docx");
  }

  [Fact]
  public async Task FormatRoute_EvaluatesIfNoneMatchHeader()
  {
    var ct = TestContext.Current.CancellationToken;
    var first = await _client.GetAsync(new Uri("/json", UriKind.Relative), ct);
    var etag = first.Headers.ETag!;

    using var matchRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("/json", UriKind.Relative));
    matchRequest.Headers.IfNoneMatch.Add(etag);
    var matchResponse = await _client.SendAsync(matchRequest, ct);
    matchResponse.StatusCode.Should().Be(HttpStatusCode.NotModified);

    using var wildcardRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("/json", UriKind.Relative));
    wildcardRequest.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Any);
    var wildcardResponse = await _client.SendAsync(wildcardRequest, ct);
    wildcardResponse.StatusCode.Should().Be(HttpStatusCode.NotModified);

    using var mismatchRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("/json", UriKind.Relative));
    mismatchRequest.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"notmatching\""));
    var mismatchResponse = await _client.SendAsync(mismatchRequest, ct);
    mismatchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Theory]
  [InlineData("text/plain", "text/plain")]
  [InlineData("application/json", "application/json")]
  [InlineData("text/markdown", "text/markdown")]
  [InlineData("application/pdf", "application/pdf")]
  [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
  [InlineData("text/html", "text/html")]
  [InlineData("application/xhtml+xml", "text/html")]
  [InlineData("*/*", "text/html")]
  [InlineData("image/png", "text/html")]
  public async Task RootRoute_NegotiatesContentTypeFromAcceptHeader(string accept, string expectedMediaType)
  {
    var ct = TestContext.Current.CancellationToken;
    using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

    var response = await _client.SendAsync(request, ct);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Content.Headers.ContentType!.MediaType.Should().Be(expectedMediaType);
    response.Headers.Vary.Should().Contain("Accept");
  }

  [Fact]
  public async Task RootRoute_WithoutAcceptHeader_ReturnsHtml_AndRespectsQualityWeights()
  {
    var ct = TestContext.Current.CancellationToken;

    using var noAcceptRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
    var noAcceptResponse = await _client.SendAsync(noAcceptRequest, ct);
    noAcceptResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/html");

    using var weightedRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
    weightedRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain", 0.4));
    weightedRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 0.9));
    var weightedResponse = await _client.SendAsync(weightedRequest, ct);
    weightedResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
  }

  [Fact]
  public async Task HealthRoute_ReturnsHealthyJson()
  {
    var ct = TestContext.Current.CancellationToken;
    var response = await _client.GetAsync(new Uri("/health", UriKind.Relative), ct);
    var json = await response.Content.ReadAsStringAsync(ct);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    json.Should().Contain("\"status\":\"healthy\"");
    json.Should().Contain("\"commit\":");
  }

  [Theory]
  [InlineData("/resume", "text/html")]
  [InlineData("/resume/", "text/html")]
  [InlineData("/resume/pdf", "application/pdf")]
  [InlineData("/resume/docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
  [InlineData("/resume/md", "text/markdown")]
  [InlineData("/resume/html", "text/html")]
  [InlineData("/resume/json", "application/json")]
  [InlineData("/resume/txt", "text/plain")]
  public async Task SubpathFormatRoute_ReturnsOkWithExpectedMediaTypeAndETag(string path, string expectedMediaType)
  {
    var ct = TestContext.Current.CancellationToken;
    var response = await _client.GetAsync(new Uri(path, UriKind.Relative), ct);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Content.Headers.ContentType!.MediaType.Should().Be(expectedMediaType);
    response.Headers.ETag.Should().NotBeNull();

    var bytes = await response.Content.ReadAsByteArrayAsync(ct);
    bytes.Should().NotBeEmpty();
  }

  [Theory]
  [InlineData("/resume")]
  [InlineData("/resume/")]
  public async Task SubpathHtmlPage_PrefixesToolbarLinks_WhileRootUsesRootLinks(string subpath)
  {
    var ct = TestContext.Current.CancellationToken;

    var subpathHtml = await _client.GetStringAsync(new Uri(subpath, UriKind.Relative), ct);
    subpathHtml.Should().Contain("href=\"/resume/pdf\"");
    subpathHtml.Should().Contain("href=\"/resume/txt\"");
    subpathHtml.Should().NotContain("href=\"/pdf\"");

    var rootHtml = await _client.GetStringAsync(new Uri("/", UriKind.Relative), ct);
    rootHtml.Should().Contain("href=\"/pdf\"");
    rootHtml.Should().NotContain("href=\"/resume/pdf\"");
  }

  [Fact]
  public async Task SubpathRootRoute_NegotiatesContentTypeAndHealthEndpoint()
  {
    var ct = TestContext.Current.CancellationToken;

    using var negotiateRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("/resume", UriKind.Relative));
    negotiateRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
    var negotiateResponse = await _client.SendAsync(negotiateRequest, ct);
    negotiateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    negotiateResponse.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");

    var healthResponse = await _client.GetAsync(new Uri("/resume/health", UriKind.Relative), ct);
    healthResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var json = await healthResponse.Content.ReadAsStringAsync(ct);
    json.Should().Contain("\"status\":\"healthy\"");

    var nonMatchingSubpath = await _client.GetAsync(new Uri("/resume-other", UriKind.Relative), ct);
    nonMatchingSubpath.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }
}
