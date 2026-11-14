using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;

using Resume.Core.Models;

namespace Resume.Core.Validation;

public static partial class ResumeArtifactValidator
{
  private static readonly string[] RequiredJsonProperties =
  [
    "$schema",
    "basics",
    "work",
    "education",
    "certificates",
    "skills",
    "projects",
    "meta",
  ];

  public static IReadOnlyList<string> ValidateLinks(ResumeDocument document)
  {
    ArgumentNullException.ThrowIfNull(document);
    var errors = new List<string>();

    ValidateUri(document.Header.Email, new Uri($"mailto:{document.Header.Email}"), errors);

    foreach (var link in document.Header.Links)
    {
      ValidateUri(link.Label, link.Url, errors);
    }

    ValidateRichTextUris(document.Summary, "Summary", errors);

    foreach (var role in document.Experience)
    {
      foreach (var bullet in role.Bullets)
      {
        ValidateRichTextUris(bullet, $"{role.Company} bullet", errors);
      }
    }

    foreach (var project in document.Projects)
    {
      if (project.Url is not null)
      {
        ValidateUri(project.Name, project.Url, errors);
      }

      ValidateRichTextUris(project.Description, $"Project '{project.Name}'", errors);
    }

    return errors;
  }

  public static IReadOnlyList<string> ValidateDocx(byte[] docxBytes)
  {
    ArgumentNullException.ThrowIfNull(docxBytes);

    if (docxBytes.Length is 0)
    {
      return ["DOCX payload is empty."];
    }

    using var stream = new MemoryStream(docxBytes, writable: false);
    using var doc = WordprocessingDocument.Open(stream, false);
    var validator = new OpenXmlValidator();

    return [.. validator.Validate(doc).Select(static e => $"{e.Path?.XPath}: {e.Description}")];
  }

  public static int CountPdfPages(byte[] pdfBytes)
  {
    ArgumentNullException.ThrowIfNull(pdfBytes);

    var ascii = Encoding.Latin1.GetString(pdfBytes);
    var pagesMatch = PdfPagesCountRegex().Match(ascii);

    if (pagesMatch.Success && int.TryParse(pagesMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pagesCount))
    {
      return pagesCount;
    }

    var pageMatches = PdfSinglePageRegex().Matches(ascii);
    return pageMatches.Count;
  }

  public static IReadOnlyList<string> ValidatePdf(byte[] pdfBytes, int maxPages = 2)
  {
    ArgumentNullException.ThrowIfNull(pdfBytes);
    var errors = new List<string>();

    if (pdfBytes.Length < 8 || Encoding.ASCII.GetString(pdfBytes, 0, 5) is not "%PDF-")
    {
      errors.Add("PDF payload does not begin with %PDF- signature.");
      return errors;
    }

    var tailLength = Math.Min(128, pdfBytes.Length);
    var tail = Encoding.ASCII.GetString(pdfBytes, pdfBytes.Length - tailLength, tailLength);

    if (tail.Contains("%%EOF", StringComparison.Ordinal) is false)
    {
      errors.Add("PDF payload is missing %%EOF trailer.");
    }

    var pageCount = CountPdfPages(pdfBytes);

    if (pageCount <= 0)
    {
      errors.Add("Could not determine PDF page count.");
    }
    else if (pageCount > maxPages)
    {
      errors.Add($"PDF rendered {pageCount} pages, exceeding maxPages={maxPages}.");
    }

    return errors;
  }

  public static IReadOnlyList<string> ValidateJson(byte[] jsonBytes)
  {
    ArgumentNullException.ThrowIfNull(jsonBytes);
    var errors = new List<string>();

    try
    {
      using var doc = JsonDocument.Parse(jsonBytes);

      if (doc.RootElement.ValueKind is not JsonValueKind.Object)
      {
        errors.Add("JSON Resume root must be an object.");
        return errors;
      }

      foreach (var property in RequiredJsonProperties)
      {
        if (doc.RootElement.TryGetProperty(property, out _) is false)
        {
          errors.Add($"JSON Resume is missing required property '{property}'.");
        }
      }
    }
    catch (JsonException ex)
    {
      errors.Add($"Invalid JSON: {ex.Message}");
    }

    return errors;
  }

  private static void ValidateRichTextUris(RichText richText, string owner, List<string> errors)
  {
    foreach (var run in richText.Runs)
    {
      if (run is LinkRun link)
      {
        ValidateUri($"{owner} ({link.Label})", link.Url, errors);
      }
    }
  }

  private static void ValidateUri(string label, Uri uri, List<string> errors)
  {
    if (uri.IsAbsoluteUri is false)
    {
      errors.Add($"URI for '{label}' is not absolute: '{uri}'.");
      return;
    }

    var validScheme = string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
      || string.Equals(uri.Scheme, "mailto", StringComparison.OrdinalIgnoreCase);

    if (validScheme is false)
    {
      errors.Add($"URI for '{label}' uses unsupported scheme '{uri.Scheme}': '{uri}'.");
    }
  }

  [GeneratedRegex(@"/Type\s*/Pages\b[^>]*/Count\s+(\d+)|/Count\s+(\d+)[^>]*/Type\s*/Pages\b", RegexOptions.Singleline)]
  private static partial Regex PdfPagesCountRegex();

  [GeneratedRegex(@"/Type\s*/Page(?![a-zA-Z])")]
  private static partial Regex PdfSinglePageRegex();
}
