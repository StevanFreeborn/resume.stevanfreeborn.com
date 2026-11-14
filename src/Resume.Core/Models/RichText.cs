namespace Resume.Core.Models;

public abstract record TextRun;

public sealed record PlainRun(string Text) : TextRun;

public sealed record BoldRun(string Text) : TextRun;

public sealed record LinkRun(string Label, Uri Url) : TextRun;

public sealed record RichText(IReadOnlyList<TextRun> Runs)
{
  public static implicit operator RichText(string text) => FromPlain(text);

  public static RichText ToRichText(string text) => FromPlain(text);

  public static RichText FromPlain(string text)
  {
    ArgumentNullException.ThrowIfNull(text);
    return new([new PlainRun(text)]);
  }

  public static RichText Of(params TextRun[] runs)
  {
    ArgumentNullException.ThrowIfNull(runs);
    return new(runs);
  }

  public string ToPlainText() => string.Concat(Runs.Select(static r => r switch
  {
    PlainRun p => p.Text,
    BoldRun b => b.Text,
    LinkRun l => l.Label,
    _ => string.Empty
  }));
}
