using System.Text;

using Resume.Core.Data;
using Resume.Core.Models;
using Resume.Core.Rendering;
using Resume.Core.Validation;

namespace Resume.Core.Tests;

public sealed class PdfResumeRendererTests
{
  private readonly PdfResumeRenderer _sut = new();
  private readonly RenderContext _context = new("abc1234", new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero), StevanResume.RepositoryUrl);

  [Fact]
  public void Render_ProducesValidTwoPagePdfWithCommitMetadata()
  {
    var bytes = _sut.Render(StevanResume.Default, _context);

    var errors = ResumeArtifactValidator.ValidatePdf(bytes, maxPages: 2);
    errors.Should().BeEmpty();

    var pages = ResumeArtifactValidator.CountPdfPages(bytes);
    pages.Should().BeInRange(1, 2);

    var latin1 = Encoding.Latin1.GetString(bytes);
    latin1.Should().Contain("Git commit: abc1234");
  }
}
