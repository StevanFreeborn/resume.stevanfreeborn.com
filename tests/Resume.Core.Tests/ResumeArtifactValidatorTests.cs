using System.Text;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Resume.Core.Data;
using Resume.Core.Models;
using Resume.Core.Validation;

namespace Resume.Core.Tests;

public sealed class ResumeArtifactValidatorTests
{
  [Fact]
  public void ValidateDocx_DetectsEmptyAndSchemaInvalidDocuments()
  {
    ResumeArtifactValidator.ValidateDocx([]).Should().ContainSingle(e => e.Contains("empty"));

    using var stream = new MemoryStream();
    using var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true);
    var mainPart = doc.AddMainDocumentPart();
    // Place SectionProperties before a Paragraph to violate OOXML schema order
    mainPart.Document = new Document(
      new Body(
        new SectionProperties(),
        new Paragraph(new Run(new Text("Invalid order")))
      )
    );
    mainPart.Document.Save();

    var errors = ResumeArtifactValidator.ValidateDocx(stream.ToArray());
    errors.Should().NotBeEmpty();
  }

  [Fact]
  public void ValidatePdf_And_CountPdfPages_CoverAllBranches()
  {
    ResumeArtifactValidator.ValidatePdf([]).Should().ContainSingle(e => e.Contains("%PDF-"));

    var noEof = Encoding.ASCII.GetBytes("%PDF-1.4\n/Type /Page\n");
    var noEofErrors = ResumeArtifactValidator.ValidatePdf(noEof);
    noEofErrors.Should().Contain(e => e.Contains("%%EOF"));

    var zeroPages = Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF");
    ResumeArtifactValidator.ValidatePdf(zeroPages).Should().ContainSingle(e => e.Contains("page count"));

    var twoSinglePages = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj << /Type /Page >>\n2 0 obj << /Type /Page >>\n%%EOF");
    ResumeArtifactValidator.CountPdfPages(twoSinglePages).Should().Be(2);
    ResumeArtifactValidator.ValidatePdf(twoSinglePages, maxPages: 1)
      .Should().ContainSingle(e => e.Contains("exceeding maxPages=1"));
  }

  [Fact]
  public void ValidateJson_DetectsInvalidJsonArrayRootAndMissingProperties()
  {
    ResumeArtifactValidator.ValidateJson(Encoding.UTF8.GetBytes("{not-json"))
      .Should().ContainSingle(e => e.Contains("Invalid JSON"));

    ResumeArtifactValidator.ValidateJson(Encoding.UTF8.GetBytes("[]"))
      .Should().ContainSingle(e => e.Contains("must be an object"));

    var missingErrors = ResumeArtifactValidator.ValidateJson(Encoding.UTF8.GetBytes("{}"));
    missingErrors.Should().HaveCount(8);
  }

  [Fact]
  public void ValidateLinks_FlagsRelativeAndUnsupportedSchemeUris()
  {
    var relativeUri = new Uri("/relative/path", UriKind.Relative);
    var httpUri = new Uri("http://insecure.example.com");

    var badDoc = StevanResume.Default with
    {
      Header = StevanResume.Default.Header with
      {
        Links = [new ContactLink("BadLink", relativeUri)],
      },
      Summary = RichText.Of(new LinkRun("SummaryHttp", httpUri)),
      Experience =
      [
        StevanResume.Default.Experience[0] with
        {
          Bullets = [RichText.Of(new LinkRun("ExpRel", relativeUri))],
        },
      ],
      Projects =
      [
        StevanResume.Default.Projects[0] with
        {
          Url = httpUri,
          Description = RichText.Of(new LinkRun("ProjHttp", httpUri)),
        },
      ],
    };

    var errors = ResumeArtifactValidator.ValidateLinks(badDoc);
    errors.Should().HaveCount(5);
    errors.Should().Contain(e => e.Contains("not absolute"));
    errors.Should().Contain(e => e.Contains("unsupported scheme 'http'"));
  }
}
