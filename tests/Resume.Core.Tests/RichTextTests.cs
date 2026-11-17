using Resume.Core.Models;

namespace Resume.Core.Tests;

public sealed class RichTextTests
{
  private sealed record CustomRun(string Value) : TextRun;

  [Fact]
  public void ConversionsAndToPlainText_SupportAllRunTypes()
  {
    RichText implicitText = "hello";
    var explicitText = RichText.ToRichText("world");
    var combined = RichText.Of(
      new PlainRun("Built "),
      new BoldRun("onx"),
      new PlainRun(" at "),
      new LinkRun("GitHub", new Uri("https://github.com/StevanFreeborn/onx")),
      new CustomRun("ignored")
    );

    implicitText.ToPlainText().Should().Be("hello");
    explicitText.ToPlainText().Should().Be("world");
    combined.ToPlainText().Should().Be("Built onx at GitHub");
  }

  [Fact]
  public void GuardClauses_ThrowOnNullInput()
  {
    var act1 = () => RichText.FromPlain(null!);
    var act2 = () => RichText.Of(null!);

    act1.Should().Throw<ArgumentNullException>();
    act2.Should().Throw<ArgumentNullException>();
  }
}
