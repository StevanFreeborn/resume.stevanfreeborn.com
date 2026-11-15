using Resume.Core.Models;

namespace Resume.Core.Rendering;

public interface IResumeRenderer
{
  ResumeFormat Format { get; }

  string ContentType { get; }

  string FileExtension { get; }

  string RouteSegment { get; }

  byte[] Render(ResumeDocument document, RenderContext context);
}
