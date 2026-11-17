using System.Text;

using Resume.Core.Data;
using Resume.Core.Models;
using Resume.Core.Rendering;
using Resume.Core.Validation;

namespace Resume.Core.Tests;

public sealed class RendererEdgeCasesTests
{
  private static readonly RenderContext s_contextWithoutRepo = new(
    CommitSha: "deadbeef",
    GeneratedAtUtc: new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero),
    RepositoryUrl: null,
    IncludeWebToolbar: false
  );

  private static ResumeDocument CreateRichEdgeCaseDocument()
  {
    var richText = RichText.Of(
      new PlainRun("Built "),
      new BoldRun("resilient <systems>"),
      new PlainRun(" using "),
      new LinkRun("Aspire", new Uri("https://aspire.dev")),
      new PlainRun(" & tests.")
    );

    return new ResumeDocument(
      Header: new ResumeHeader(
        Name: "Stevan Freeborn",
        Headline: "Quality & Platform Engineer",
        Location: "Olathe, KS",
        Phone: null,
        Email: "stevan.freeborn@gmail.com",
        Links:
        [
          new ContactLink("stevanfreeborn.com", new Uri("https://stevanfreeborn.com")),
        ]
      ),
      Summary: richText,
      Skills: [new SkillGroup("Testing", ["xUnit", "Playwright"])],
      Experience:
      [
        new WorkExperience(
          Title: "Lead Engineer",
          Company: "ExampleCorp",
          DateRange: "2026",
          Location: "Remote",
          Subtitle: null,
          Bullets: [richText, RichText.FromPlain(string.Empty)]
        ),
      ],
      Projects:
      [
        new ProjectItem(
          Name: "Internal Tool",
          Qualifier: null,
          Url: null,
          Description: richText
        ),
      ],
      Education: [new EducationItem("B.S., Biochemistry", "MSSU")],
      Certifications: ["Six Sigma Green Belt"],
      Theme: new ResumeTheme()
    );
  }

  [Fact]
  public void AllRenderers_HandleBoldLinkRunsNullPhoneNullProjectUrlAndNullRepositoryUrl()
  {
    var doc = CreateRichEdgeCaseDocument();

    var pdf = new PdfResumeRenderer().Render(doc, s_contextWithoutRepo);
    ResumeArtifactValidator.ValidatePdf(pdf).Should().BeEmpty();

    var docx = new DocxResumeRenderer().Render(doc, s_contextWithoutRepo);
    ResumeArtifactValidator.ValidateDocx(docx).Should().BeEmpty();

    var html = Encoding.UTF8.GetString(new HtmlResumeRenderer().Render(doc, s_contextWithoutRepo));
    html.Should().Contain("<strong>resilient &lt;systems&gt;</strong>");
    html.Should().Contain("href=\"https://aspire.dev/\"");
    html.Should().Contain("<strong>Internal Tool:</strong>");
    html.Should().NotContain(">Source<");

    var md = Encoding.UTF8.GetString(new MarkdownResumeRenderer().Render(doc, s_contextWithoutRepo));
    md.Should().Contain("**resilient <systems>**");
    md.Should().Contain("[Aspire](https://aspire.dev/)");
    md.Should().Contain("- **Internal Tool:**");
    md.Should().Contain("*Auto-generated from commit `deadbeef`.*");

    var jsonBytes = new JsonResumeRenderer().Render(doc, s_contextWithoutRepo);
    ResumeArtifactValidator.ValidateJson(jsonBytes).Should().BeEmpty();
    var json = Encoding.UTF8.GetString(jsonBytes);
    json.Should().Contain("\"stevanfreeborn.com\"");
    json.Should().Contain("\"startDate\": \"2026\"");

    var txt = Encoding.UTF8.GetString(new PlainTextResumeRenderer().Render(doc, s_contextWithoutRepo));
    txt.Should().Contain("Aspire (https://aspire.dev/)");
    txt.Should().Contain("* Internal Tool:");
    txt.Should().Contain("Commit: deadbeef");
  }

  [Fact]
  public void PdfRenderer_HandlesEmptySections_And_FontBootstrapperIsIdempotent()
  {
    FontBootstrapper.EnsureInitialized();
    FontBootstrapper.EnsureInitialized();

    var emptyDoc = StevanResume.Default with
    {
      Skills = [],
      Experience = [],
      Projects = [],
    };

    var pdf = new PdfResumeRenderer().Render(emptyDoc, s_contextWithoutRepo);
    ResumeArtifactValidator.ValidatePdf(pdf, maxPages: 1).Should().BeEmpty();
  }
}
