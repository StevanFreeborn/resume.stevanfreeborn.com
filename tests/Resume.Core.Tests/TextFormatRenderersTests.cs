using System.Text;

using Resume.Core.Data;
using Resume.Core.Models;
using Resume.Core.Rendering;
using Resume.Core.Validation;
using Resume.Core.Variants;

namespace Resume.Core.Tests;

public sealed class TextFormatRenderersTests
{
  private readonly RenderContext _context = new("abc1234", new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero), StevanResume.RepositoryUrl);

  [Fact]
  public void HtmlRenderer_IncludesLandmarksDarkModePrintCssAndTogglesToolbar()
  {
    var sut = new HtmlResumeRenderer();

    var webHtml = Encoding.UTF8.GetString(sut.Render(StevanResume.Default, _context with { IncludeWebToolbar = true }));
    webHtml.Should().Contain("<!DOCTYPE html>");
    webHtml.Should().Contain("<header>");
    webHtml.Should().Contain("<main>");
    webHtml.Should().Contain("<footer>");
    webHtml.Should().Contain("prefers-color-scheme: dark");
    webHtml.Should().Contain("@media print");
    webHtml.Should().Contain("class=\"format-bar\"");
    webHtml.Should().Contain("href=\"/pdf\" target=\"_blank\" rel=\"noopener noreferrer\"");
    webHtml.Should().Contain("href=\"/docx\" target=\"_blank\" rel=\"noopener noreferrer\"");
    webHtml.Should().Contain("href=\"/md\" target=\"_blank\" rel=\"noopener noreferrer\"");
    webHtml.Should().Contain("href=\"/html\" target=\"_blank\" rel=\"noopener noreferrer\"");
    webHtml.Should().Contain("href=\"/json\" target=\"_blank\" rel=\"noopener noreferrer\"");
    webHtml.Should().Contain("href=\"/txt\" target=\"_blank\" rel=\"noopener noreferrer\"");
    webHtml.Should().Contain("abc1234");

    var subpathHtml = Encoding.UTF8.GetString(sut.Render(StevanResume.Default, _context with { IncludeWebToolbar = true, BasePath = "/resume" }));
    subpathHtml.Should().Contain("href=\"/resume/pdf\" target=\"_blank\" rel=\"noopener noreferrer\"");
    subpathHtml.Should().Contain("href=\"/resume/docx\" target=\"_blank\" rel=\"noopener noreferrer\"");
    subpathHtml.Should().Contain("href=\"/resume/md\" target=\"_blank\" rel=\"noopener noreferrer\"");
    subpathHtml.Should().Contain("href=\"/resume/html\" target=\"_blank\" rel=\"noopener noreferrer\"");
    subpathHtml.Should().Contain("href=\"/resume/json\" target=\"_blank\" rel=\"noopener noreferrer\"");
    subpathHtml.Should().Contain("href=\"/resume/txt\" target=\"_blank\" rel=\"noopener noreferrer\"");
    subpathHtml.Should().NotContain("href=\"/pdf\"");

    var trailingSlashSubpathHtml = Encoding.UTF8.GetString(sut.Render(StevanResume.Default, _context with { IncludeWebToolbar = true, BasePath = "/resume/" }));
    trailingSlashSubpathHtml.Should().Be(subpathHtml);

    var standaloneHtml = Encoding.UTF8.GetString(sut.Render(StevanResume.Default, _context with { IncludeWebToolbar = false }));
    standaloneHtml.Should().NotContain("<nav class=\"format-bar\"");
  }

  [Fact]
  public void HtmlFragments_LoadEmbeddedResource_ThrowsWhenResourceMissing()
  {
    var act = static () => ResumeHtmlFragments.LoadEmbeddedResource(typeof(ResumeHtmlFragments).Assembly, "nonexistent.css");
    act.Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void MarkdownRenderer_ProducesCleanMarkdownWithLinksAndCommitFooter()
  {
    var sut = new MarkdownResumeRenderer();
    var md = Encoding.UTF8.GetString(sut.Render(StevanResume.Default, _context));

    md.Should().StartWith("# Stevan Freeborn");
    md.Should().Contain("## Experience");
    md.Should().Contain("[**onx (Rust)**](https://github.com/StevanFreeborn/onx):");
    md.Should().Contain("abc1234");
  }

  [Fact]
  public void JsonRenderer_ProducesValidJsonResumeSchema()
  {
    var sut = new JsonResumeRenderer();
    var bytes = sut.Render(StevanResume.Default, _context);

    var errors = ResumeArtifactValidator.ValidateJson(bytes);
    errors.Should().BeEmpty();

    var json = Encoding.UTF8.GetString(bytes);
    json.Should().Contain("\"Stevan Freeborn\"");
    json.Should().Contain("\"abc1234\"");
  }

  [Fact]
  public void PlainTextRenderer_ProducesReadableTerminalOutput()
  {
    var sut = new PlainTextResumeRenderer();
    var txt = Encoding.UTF8.GetString(sut.Render(StevanResume.Default, _context));

    txt.Should().Contain("STEVAN FREEBORN");
    txt.Should().Contain("SUMMARY");
    txt.Should().Contain("SELECTED PROJECTS");
    txt.Should().Contain("Commit: abc1234");
  }

  [Fact]
  public void VariantApplier_OverridesTargetFieldsWithoutMutatingBase()
  {
    var variant = new ResumeVariant(
      Headline: "Principal SDET",
      Summary: "Tailored summary.",
      IncludePhone: false,
      AdditionalSkills: new Dictionary<string, IReadOnlyList<string>> { ["Testing"] = ["k6"] },
      ProjectNames: ["onx", "onx-skill"],
      ExperienceOverrides:
      [
        new ExperienceOverride("Mid-Land Enterprises", AppendBullets: ["Extra PM note."])
      ]
    );

    var applied = ResumeVariantApplier.Apply(StevanResume.Default, variant);

    applied.Header.Headline.Should().Be("Principal SDET");
    applied.Header.Phone.Should().BeNull();
    applied.Summary.ToPlainText().Should().Be("Tailored summary.");
    applied.Projects.Select(static p => p.Name).Should().Equal("onx", "onx-skill");
    applied.Skills.First(static s => s.Category == "Testing").Items.Should().Contain("k6");
    applied.Experience.First(static e => e.Company == "Mid-Land Enterprises").Bullets.Should().HaveCount(2);

    StevanResume.Default.Header.Phone.Should().Be("417-540-8870");
    StevanResume.Default.Projects.Should().HaveCount(5);
  }

  [Fact]
  public void DefaultResume_PassesLinkValidationAndGeneralizesContent()
  {
    var linkErrors = ResumeArtifactValidator.ValidateLinks(StevanResume.Default);
    linkErrors.Should().BeEmpty();

    StevanResume.Default.Summary.ToPlainText().Should().NotContain("WISEcode");
    StevanResume.Default.Summary.ToPlainText().Should().NotContain("nutrition computations");
  }
}
