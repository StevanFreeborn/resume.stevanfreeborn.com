using System.Text;

using Resume.Core.Models;

namespace Resume.Core.Rendering;

public sealed class MarkdownResumeRenderer : IResumeRenderer
{
  public ResumeFormat Format => ResumeFormat.Markdown;

  public string ContentType => "text/markdown; charset=utf-8";

  public string FileExtension => ".md";

  public string RouteSegment => "md";

  public byte[] Render(ResumeDocument document, RenderContext context)
  {
    ArgumentNullException.ThrowIfNull(document);
    ArgumentNullException.ThrowIfNull(context);

    var contactLine = FormatContactLine(document.Header);

    var skillsSection = string.Join(
      '\n', 
      document.Skills.Select(static group => $"- **{group.Category}:** {string.Join(", ", group.Items)}")
    );

    var experienceSection = string.Join("\n\n", document.Experience.Select(RenderRole));
    var projectsSection = string.Join('\n', document.Projects.Select(RenderProject));

    var educationLines = document.Education.Select(static edu => $"- **{edu.Credential}** | {edu.Institution}")
      .Append($"- **Certifications** | {string.Join(" • ", document.Certifications)}");

    var educationSection = string.Join('\n', educationLines);
    var footer = RenderFooter(context);

    var markdown = $"""
      # {document.Header.Name}

      **{document.Header.Headline}**

      {contactLine}

      ## Summary

      {RenderRichMarkdown(document.Summary)}

      ## Skills

      {skillsSection}

      ## Experience

      {experienceSection}

      ## Selected Projects

      {projectsSection}

      ## Education & Certifications

      {educationSection}

      ---

      {footer}

      """;

    return Encoding.UTF8.GetBytes(markdown);
  }

  private static string FormatContactLine(ResumeHeader header)
  {
    var contactParts = new List<string> { header.Location };

    if (string.IsNullOrWhiteSpace(header.Phone) is false)
    {
      contactParts.Add(header.Phone);
    }

    contactParts.Add($"[{header.Email}](mailto:{header.Email})");

    foreach (var link in header.Links)
    {
      contactParts.Add($"[{link.Label}]({link.Url})");
    }

    return string.Join(" | ", contactParts);
  }

  private static string RenderRole(WorkExperience role)
  {
    var meta = string.IsNullOrWhiteSpace(role.Subtitle)
      ? role.Location
      : $"{role.Location} • {role.Subtitle}";

    var bullets = string.Join(
      '\n', 
      role.Bullets.Select(static bullet => $"- {RenderRichMarkdown(bullet)}")
    );

    return $"""
      ### {role.Title} | {role.Company} ({role.DateRange})

      *{meta}*

      {bullets}
      """;
  }

  private static string RenderProject(ProjectItem project)
  {
    var qualifier = project.Qualifier is null ? string.Empty : $" ({project.Qualifier})";
    var label = project.Url is not null
      ? $"[**{project.Name}{qualifier}**]({project.Url}):"
      : $"**{project.Name}{qualifier}:**";

    return $"- {label} {RenderRichMarkdown(project.Description)}";
  }

  private static string RenderFooter(RenderContext context)
  {
    if (context.RepositoryUrl is not null)
    {
      var commitUrl = new Uri($"{context.RepositoryUrl.ToString().TrimEnd('/')}/commit/{context.CommitSha}");
      return $"*Auto-generated from commit [`{context.CommitSha}`]({commitUrl}).*";
    }

    return $"*Auto-generated from commit `{context.CommitSha}`.*";
  }

  private static string RenderRichMarkdown(RichText richText)
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
          parts.Add($"**{bold.Text}**");
          break;
        case LinkRun link:
          parts.Add($"[{link.Label}]({link.Url})");
          break;
      }
    }

    return string.Concat(parts);
  }
}
