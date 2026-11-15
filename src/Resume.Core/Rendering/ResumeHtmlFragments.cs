using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;

namespace Resume.Core.Rendering;

public static class ResumeHtmlFragments
{
  private const string StylesheetResourceName = "Resume.Core.Resources.Styles.resume.css";

  private static readonly Lazy<string> s_stylesheet = new(
    static () => LoadEmbeddedResource(typeof(ResumeHtmlFragments).Assembly, StylesheetResourceName).Trim()
  );

  private static readonly (string Segment, string Label)[] s_formats =
  [
    ("pdf", "PDF"),
    ("docx", "DOCX"),
    ("md", "Markdown"),
    ("html", "HTML"),
    ("json", "JSON"),
    ("txt", "Text"),
  ];

  public static string Stylesheet => s_stylesheet.Value;

  public static string Encode(string value) => WebUtility.HtmlEncode(value);

  public static string RenderFormatLinks(string basePath)
  {
    var encodedBase = string.IsNullOrEmpty(basePath)
      ? string.Empty
      : Encode(basePath.TrimEnd('/'));

    var links = string.Join(
      " | ",
      s_formats.Select(format => $"<a href=\"{encodedBase}/{format.Segment}\" target=\"_blank\" rel=\"noopener noreferrer\">{format.Label}</a>")
    );

    return $"Formats: {links}";
  }

  public static string RenderFooter(string commitSha, Uri? repositoryUrl)
  {
    var dotnetMajor = Environment.Version.Major.ToString(CultureInfo.InvariantCulture);
    var encodedSha = Encode(commitSha);
    var sourceLink = repositoryUrl is not null
      ? $" &bull; <a href=\"{Encode(repositoryUrl.ToString())}\" target=\"_blank\" rel=\"noopener noreferrer\">Source</a>"
      : string.Empty;

    return $"""
        <footer>
          <p><small>Built with .NET {dotnetMajor} &bull; Commit <code>{encodedSha}</code>{sourceLink}</small></p>
        </footer>
      """;
  }

  internal static string LoadEmbeddedResource(Assembly assembly, string resourceName)
  {
    ArgumentNullException.ThrowIfNull(assembly);

    using var stream = assembly.GetManifestResourceStream(resourceName)
      ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
    using var reader = new StreamReader(stream, Encoding.UTF8);
    return reader.ReadToEnd();
  }
}
