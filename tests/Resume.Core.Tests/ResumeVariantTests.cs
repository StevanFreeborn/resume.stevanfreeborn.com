using Resume.Core.Data;
using Resume.Core.Variants;

namespace Resume.Core.Tests;

public sealed class ResumeVariantTests
{
  [Fact]
  public void LoadFromFile_ParsesCommentsAndTrailingCommas_AndRejectsInvalidInputs()
  {
    var tempDir = Path.Combine(Path.GetTempPath(), "resume-variant-tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(tempDir);

    try
    {
      var validPath = Path.Combine(tempDir, "valid.json");

      File.WriteAllText(
        validPath,
        """
        {
          // Single-line comment supported
          "headline": "Staff Quality Engineer",
          "phoneOverride": "555-0100",
        }
        """
      );

      var loaded = ResumeVariant.LoadFromFile(validPath);
      loaded.Headline.Should().Be("Staff Quality Engineer");
      loaded.PhoneOverride.Should().Be("555-0100");

      var nullPath = Path.Combine(tempDir, "null.json");
      File.WriteAllText(nullPath, "null");
      var actNull = () => ResumeVariant.LoadFromFile(nullPath);
      actNull.Should().Throw<InvalidOperationException>();

      var actBlank = () => ResumeVariant.LoadFromFile("   ");
      actBlank.Should().Throw<ArgumentException>();
    }
    finally
    {
      if (Directory.Exists(tempDir))
      {
        Directory.Delete(tempDir, recursive: true);
      }
    }
  }

  [Fact]
  public void Apply_EmptyVariantPreservesBase_AndCoversAllOverrideBranches()
  {
    var unchanged = ResumeVariantApplier.Apply(StevanResume.Default, new ResumeVariant());
    unchanged.Header.Should().Be(StevanResume.Default.Header);
    unchanged.Skills.Should().HaveCount(StevanResume.Default.Skills.Count);
    unchanged.Projects.Should().HaveCount(StevanResume.Default.Projects.Count);
    unchanged.Experience.Should().HaveCount(StevanResume.Default.Experience.Count);

    var customized = ResumeVariantApplier.Apply(
      StevanResume.Default,
      new ResumeVariant(
        PhoneOverride: "555-0199",
        AdditionalSkills: new Dictionary<string, IReadOnlyList<string>>
        {
          ["Languages"] = ["C#", "PowerShell"],
          ["Cloud"] = ["AWS"],
        },
        ProjectNames: ["onx", "nonexistent-project"],
        ExperienceOverrides:
        [
          new ExperienceOverride(
            Company: "Ajinomoto Foods North America, Inc.",
            DateRange: "Jan 2016 – Feb 2019",
            Bullets: ["Replaced bullet only on earlier stint."]
          ),
        ]
      )
    );

    customized.Header.Phone.Should().Be("555-0199");
    customized.Skills.First(static s => s.Category == "Languages").Items.Should().Contain("PowerShell").And.ContainSingle(static i => i == "C#");
    customized.Skills.Should().Contain(static s => s.Category == "Cloud");
    customized.Projects.Should().ContainSingle(static p => p.Name == "onx");

    var firstAjinomoto = customized.Experience.First(static e => e.DateRange == "Jun 2019 – Aug 2020");
    var earlierAjinomoto = customized.Experience.First(static e => e.DateRange == "Jan 2016 – Feb 2019");
    firstAjinomoto.Bullets.Should().HaveCount(1).And.NotContain(static b => b.ToPlainText().Contains("Replaced"));
    earlierAjinomoto.Bullets.Select(static b => b.ToPlainText()).Should().Equal("Replaced bullet only on earlier stint.");
  }
}
