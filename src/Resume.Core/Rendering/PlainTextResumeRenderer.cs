using System.Text;

using Resume.Core.Models;

namespace Resume.Core.Rendering;

public sealed class PlainTextResumeRenderer : IResumeRenderer
{
  private const int LineWidth = 80;
  private static readonly string s_headerRule = new('=', LineWidth);
  private static readonly string s_footerRule = new('-', LineWidth);

  public ResumeFormat Format => ResumeFormat.Text;

  public string ContentType => "text/plain; charset=utf-8";

  public string FileExtension => ".txt";

  public string RouteSegment => "txt";

  public byte[] Render(ResumeDocument document, RenderContext context)
  {
    ArgumentNullException.ThrowIfNull(document);
    ArgumentNullException.ThrowIfNull(context);

    var contactLine = FormatContactLine(document.Header);
    var summaryBody = WrapText(RenderRichText(document.Summary), string.Empty);
    var skillsBody = string.Join('\n', document.Skills.Select(static group =>
      WrapText($"{group.Category}: {string.Join(", ", group.Items)}", string.Empty, "  ")));
    var experienceBody = string.Join("\n\n", document.Experience.Select(FormatRole));
    var projectsBody = string.Join('\n', document.Projects.Select(FormatProject));
    var educationLines = document.Education.Select(static edu => $"{edu.Credential} | {edu.Institution}")
      .Append(WrapText($"Certifications | {string.Join(" | ", document.Certifications)}", string.Empty, "  "));
    var educationBody = string.Join('\n', educationLines);

    var text = $"""
      {document.Header.Name.ToUpperInvariant()}
      {document.Header.Headline}
      {contactLine}
      {s_headerRule}

      {FormatSection("SUMMARY", summaryBody)}

      {FormatSection("SKILLS", skillsBody)}

      {FormatSection("EXPERIENCE", experienceBody)}

      {FormatSection("SELECTED PROJECTS", projectsBody)}

      {FormatSection("EDUCATION & CERTIFICATIONS", educationBody)}

      {s_footerRule}
      Commit: {context.CommitSha}

      """;

    return Encoding.UTF8.GetBytes(text);
  }

  private static string FormatContactLine(ResumeHeader header)
  {
    var contact = new List<string> { header.Location };

    if (!string.IsNullOrWhiteSpace(header.Phone))
    {
      contact.Add(header.Phone);
    }

    contact.Add(header.Email);

    foreach (var link in header.Links)
    {
      contact.Add(link.Url.ToString());
    }

    return string.Join(" | ", contact);
  }

  private static string FormatSection(string title, string body)
  {
    var underline = new string('-', title.Length);
    return string.IsNullOrEmpty(body)
      ? $"{title}\n{underline}"
      : $"{title}\n{underline}\n{body}";
  }

  private static string FormatRole(WorkExperience role)
  {
    var meta = string.IsNullOrWhiteSpace(role.Subtitle)
      ? role.Location
      : $"{role.Location} - {role.Subtitle}";
    var bullets = role.Bullets
      .Select(static bullet => WrapText(RenderRichText(bullet), "* ", "  "))
      .Where(static line => line.Length > 0);

    var lines = new List<string>
    {
      $"{role.Title} | {role.Company} ({role.DateRange})",
      meta,
    };
    lines.AddRange(bullets);

    return string.Join('\n', lines);
  }

  private static string FormatProject(ProjectItem project)
  {
    var qualifier = project.Qualifier is null ? string.Empty : $" ({project.Qualifier})";
    var urlSuffix = project.Url is null ? string.Empty : $" [{project.Url}]";
    return WrapText($"{project.Name}{qualifier}{urlSuffix}: {RenderRichText(project.Description)}", "* ", "  ");
  }

  private static string RenderRichText(RichText richText)
  {
    var parts = new List<string>(richText.Runs.Count);

    foreach (var run in richText.Runs)
    {
      switch (run)
      {
        case PlainRun plain:
          parts.Add(plain.Text);
          break;
        case BoldRun bold:
          parts.Add(bold.Text);
          break;
        case LinkRun link:
          parts.Add($"{link.Label} ({link.Url})");
          break;
      }
    }

    return string.Concat(parts);
  }

  private static string WrapText(string text, string firstPrefix, string continuationPrefix = "")
  {
    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (words.Length == 0)
    {
      return string.Empty;
    }

    var lines = new List<string>();
    var current = new StringBuilder(firstPrefix);
    var firstWordOnLine = true;

    foreach (var word in words)
    {
      var needed = firstWordOnLine ? word.Length : word.Length + 1;

      if (!firstWordOnLine && current.Length + needed > LineWidth)
      {
        lines.Add(current.ToString());
        current.Clear();
        current.Append(continuationPrefix);
        current.Append(word);
      }
      else
      {
        if (!firstWordOnLine)
        {
          current.Append(' ');
        }

        current.Append(word);
        firstWordOnLine = false;
      }
    }

    lines.Add(current.ToString());
    return string.Join('\n', lines);
  }
}
