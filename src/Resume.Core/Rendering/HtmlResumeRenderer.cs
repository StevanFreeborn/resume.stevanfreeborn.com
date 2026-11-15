using System.Text;

using Resume.Core.Models;

namespace Resume.Core.Rendering;

public sealed class HtmlResumeRenderer : IResumeRenderer
{
  public ResumeFormat Format => ResumeFormat.Html;

  public string ContentType => "text/html; charset=utf-8";

  public string FileExtension => ".html";

  public string RouteSegment => "html";

  public byte[] Render(ResumeDocument document, RenderContext context)
  {
    ArgumentNullException.ThrowIfNull(document);
    ArgumentNullException.ThrowIfNull(context);

    var encodedName = Enc(document.Header.Name);
    var encodedHeadline = Enc(document.Header.Headline);
    var encodedSha = Enc(context.CommitSha);
    var toolbarSection = context.IncludeWebToolbar ? $"\n{RenderToolbar(context.BasePath)}" : string.Empty;
    var headerHtml = RenderHeader(document.Header);
    var skillsHtml = RenderSkills(document.Skills);
    var experienceHtml = string.Join('\n', document.Experience.Select(RenderRole));
    var projectsHtml = RenderProjects(document.Projects);
    var educationHtml = RenderEducation(document.Education, document.Certifications);
    var footerHtml = ResumeHtmlFragments.RenderFooter(context.CommitSha, context.RepositoryUrl);

    var html = $$"""
      <!DOCTYPE html>
      <html lang="en">
      <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>{{encodedName}} - Resume</title>
        <meta name="description" content="{{encodedHeadline}}">
        <meta property="og:title" content="{{encodedName}} — {{encodedHeadline}}">
        <meta property="og:description" content="{{Enc(document.Summary.ToPlainText())}}">
        <meta property="og:type" content="profile">
        <style>
      {{ResumeHtmlFragments.Stylesheet}}
        </style>
      </head>
      <body>
        <!-- Generated from commit {{encodedSha}} -->{{toolbarSection}}
      {{headerHtml}}
        <main>
          <section id="summary">
            <h2>Summary</h2>
            <p>{{RenderRichHtml(document.Summary)}}</p>
          </section>
      {{skillsHtml}}
          <section id="experience">
            <h2>Experience</h2>
      {{experienceHtml}}
          </section>
      {{projectsHtml}}
      {{educationHtml}}
        </main>
      {{footerHtml}}
      </body>
      </html>
      """;

    return Encoding.UTF8.GetBytes(html);
  }

  private static string RenderToolbar(string basePath)
  {
    return $"""
        <nav class="format-bar" aria-label="Resume formats">
          <span>{ResumeHtmlFragments.RenderFormatLinks(basePath)}</span>
          <button type="button" onclick="window.print()">Print</button>
        </nav>
      """;
  }

  private static string RenderHeader(ResumeHeader header)
  {
    var contactParts = new List<string> { Enc(header.Location) };

    if (string.IsNullOrWhiteSpace(header.Phone) is false)
    {
      contactParts.Add(Enc(header.Phone));
    }

    contactParts.Add($"<a href=\"mailto:{Enc(header.Email)}\">{Enc(header.Email)}</a>");

    foreach (var link in header.Links)
    {
      contactParts.Add($"<a href=\"{Enc(link.Url.ToString())}\" target=\"_blank\" rel=\"noopener noreferrer\">{Enc(link.Label)}</a>");
    }

    return $"""
        <header>
          <h1>{Enc(header.Name)}</h1>
          <p><strong>{Enc(header.Headline)}</strong></p>
          <p>{string.Join(" | ", contactParts)}</p>
        </header>
      """;
  }

  private static string RenderSkills(IReadOnlyList<SkillGroup> skills)
  {
    var items = string.Join(
      '\n', 
      skills.Select(static group =>$"        <li><strong>{Enc(group.Category)}:</strong> {Enc(string.Join(", ", group.Items))}</li>")
    );

    return $"""
          <section id="skills">
            <h2>Skills</h2>
            <ul>
      {items}
            </ul>
          </section>
      """;
  }

  private static string RenderRole(WorkExperience role)
  {
    var subtitleSuffix = string.IsNullOrWhiteSpace(role.Subtitle)
      ? string.Empty
      : $" &bull; {Enc(role.Subtitle)}";

    var bullets = string.Join(
      '\n', 
      role.Bullets.Select(static bullet => $"          <li>{RenderRichHtml(bullet)}</li>")
    );

    return $"""
            <article class="role">
              <div class="role-header">
                <h3>{Enc(role.Title)} | {Enc(role.Company)}</h3>
                <span class="role-date">{Enc(role.DateRange)}</span>
              </div>
              <p class="role-meta"><em>{Enc(role.Location)}{subtitleSuffix}</em></p>
              <ul>
      {bullets}
              </ul>
            </article>
      """;
  }

  private static string RenderProjects(IReadOnlyList<ProjectItem> projects)
  {
    var items = string.Join('\n', projects.Select(static project =>
    {
      var qualifier = project.Qualifier is null ? string.Empty : $" ({Enc(project.Qualifier)})";
      var titleHtml = project.Url is not null
        ? $"<a href=\"{Enc(project.Url.ToString())}\" target=\"_blank\" rel=\"noopener noreferrer\"><strong>{Enc(project.Name)}{qualifier}:</strong></a>"
        : $"<strong>{Enc(project.Name)}{qualifier}:</strong>";

      return $"        <li>{titleHtml} {RenderRichHtml(project.Description)}</li>";
    }));

    return $"""
          <section id="projects">
            <h2>Selected Projects</h2>
            <ul>
      {items}
            </ul>
          </section>
      """;
  }

  private static string RenderEducation(IReadOnlyList<EducationItem> education, IReadOnlyList<string> certifications)
  {
    var items = education.Select(static edu => $"        <li><strong>{Enc(edu.Credential)}</strong> | {Enc(edu.Institution)}</li>")
      .Append($"        <li><strong>Certifications</strong> | {Enc(string.Join(" • ", certifications))}</li>");

    return $"""
          <section id="education">
            <h2>Education &amp; Certifications</h2>
            <ul>
      {string.Join('\n', items)}
            </ul>
          </section>
      """;
  }

  private static string RenderRichHtml(RichText richText)
  {
    var parts = new List<string>(richText.Runs.Count);

    foreach (var run in richText.Runs)
    {
      switch (run)
      {
        case PlainRun plain:
          parts.Add(Enc(plain.Text));
          break;
        case BoldRun bold:
          parts.Add($"<strong>{Enc(bold.Text)}</strong>");
          break;
        case LinkRun link:
          parts.Add($"<a href=\"{Enc(link.Url.ToString())}\" target=\"_blank\" rel=\"noopener noreferrer\">{Enc(link.Label)}</a>");
          break;
      }
    }

    return string.Concat(parts);
  }

  private static string Enc(string value) => ResumeHtmlFragments.Encode(value);
}
