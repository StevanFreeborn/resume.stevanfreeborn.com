using DocumentFormat.OpenXml.Packaging;

using Resume.Core.Data;
using Resume.Core.Models;
using Resume.Core.Rendering;
using Resume.Core.Validation;

namespace Resume.Core.Tests;

public sealed class DocxResumeRendererTests
{
  private readonly DocxResumeRenderer _sut = new();
  private readonly RenderContext _context = new("abc1234", new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero), StevanResume.RepositoryUrl);

  [Fact]
  public void Render_ProducesSchemaValidOpenXmlDocument()
  {
    var bytes = _sut.Render(StevanResume.Default, _context);

    var errors = ResumeArtifactValidator.ValidateDocx(bytes);
    errors.Should().BeEmpty();
  }

  [Fact]
  public void Render_ContainsExpectedSectionsLinksAndMetadata()
  {
    var bytes = _sut.Render(StevanResume.Default, _context);

    using var stream = new MemoryStream(bytes);
    using var doc = WordprocessingDocument.Open(stream, false);

    doc.PackageProperties.Creator.Should().Be("Stevan Freeborn");
    doc.PackageProperties.Description.Should().Contain("abc1234");

    var mainPart = doc.MainDocumentPart!;
    var bodyText = mainPart.Document!.Body!.InnerText;

    bodyText.Should().Contain("STEVAN FREEBORN");
    bodyText.Should().Contain("417-540-8870");
    bodyText.Should().Contain("SUMMARY");
    bodyText.Should().Contain("EXPERIENCE");
    bodyText.Should().Contain("Senior Quality & Reliability Engineer");
    bodyText.Should().Contain("onx-skill");

    mainPart.HyperlinkRelationships.Should().Contain(r => r.Uri.ToString().Contains("github.com/StevanFreeborn/onx", StringComparison.Ordinal));
    mainPart.HyperlinkRelationships.Should().Contain(r => r.Uri.ToString() == "mailto:stevan.freeborn@gmail.com");
  }
}
