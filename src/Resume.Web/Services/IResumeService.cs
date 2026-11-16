using Resume.Core.Models;

namespace Resume.Web.Services;

internal sealed record RenderedResume(
  ResumeFormat Format,
  string RouteSegment,
  string ContentType,
  string FileName,
  byte[] Bytes,
  string ETag
);

internal interface IResumeService
{
  string CommitSha { get; }

  RenderedResume GetWebHtml(string pathBase = "");

  RenderedResume Get(ResumeFormat format);
}
