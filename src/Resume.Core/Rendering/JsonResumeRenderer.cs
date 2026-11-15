using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Resume.Core.Models;

namespace Resume.Core.Rendering;

public sealed class JsonResumeRenderer : IResumeRenderer
{
  private static readonly JsonSerializerOptions s_jsonOptions = new()
  {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  public ResumeFormat Format => ResumeFormat.Json;

  public string ContentType => "application/json; charset=utf-8";

  public string FileExtension => ".json";

  public string RouteSegment => "json";

  public byte[] Render(ResumeDocument document, RenderContext context)
  {
    ArgumentNullException.ThrowIfNull(document);
    ArgumentNullException.ThrowIfNull(context);

    var payload = new JsonResumePayload(
      Schema: "https://raw.githubusercontent.com/jsonresume/resume-schema/v1.0.0/schema.json",
      Basics: new JsonResumeBasics(
        Name: document.Header.Name,
        Label: document.Header.Headline,
        Email: document.Header.Email,
        Phone: document.Header.Phone,
        Summary: document.Summary.ToPlainText(),
        Location: new JsonResumeLocation(document.Header.Location),
        Profiles: [.. document.Header.Links.Select(static link => new JsonResumeProfile(InferNetwork(link.Url), link.Label, link.Url))]
      ),
      Work: [.. document.Experience
        .Select(static role =>
        {
          var (startDate, endDate) = ParseDateRange(role.DateRange);
          return new JsonResumeWork(
            Name: role.Company,
            Position: role.Title,
            Location: role.Location,
            StartDate: startDate,
            EndDate: endDate,
            Summary: role.Subtitle,
            Highlights: [.. role.Bullets.Select(static b => b.ToPlainText())]
          );
        })],
      Education: [.. document.Education
        .Select(static edu => new JsonResumeEducation(
          Institution: edu.Institution,
          StudyType: edu.Credential
        ))],
      Certificates: [.. document.Certifications.Select(static cert => new JsonResumeCertificate(cert))],
      Skills: [.. document.Skills.Select(static group => new JsonResumeSkill(group.Category, group.Items))],
      Projects: [.. document.Projects
        .Select(static project => new JsonResumeProject(
          Name: project.Name,
          Description: project.Description.ToPlainText(),
          Url: project.Url,
          Keywords: project.Qualifier is null
            ? null
            : project.Qualifier.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        ))],
      Meta: new JsonResumeMeta(
        Version: context.CommitSha,
        LastModified: context.GeneratedAtUtc.ToString("O"),
        Canonical: new Uri("https://resume.stevanfreeborn.com/json")
      )
    );

    var json = JsonSerializer.Serialize(payload, s_jsonOptions);
    return Encoding.UTF8.GetBytes(json);
  }

  private static string InferNetwork(Uri url)
  {
    var host = url.Host;

    if (host.Contains("linkedin", StringComparison.OrdinalIgnoreCase))
    {
      return "LinkedIn";
    }

    if (host.Contains("github", StringComparison.OrdinalIgnoreCase))
    {
      return "GitHub";
    }

    return host;
  }

  private static (string StartDate, string? EndDate) ParseDateRange(string dateRange)
  {
    var parts = dateRange.Split('–', StringSplitOptions.TrimEntries);

    if (parts.Length is not 2)
    {
      return (dateRange, null);
    }

    var end = string.Equals(parts[1], "Present", StringComparison.OrdinalIgnoreCase)
      ? null
      : parts[1];

    return (parts[0], end);
  }

  private sealed record JsonResumePayload(
    [property: JsonPropertyName("$schema")] string Schema,
    JsonResumeBasics Basics,
    IReadOnlyList<JsonResumeWork> Work,
    IReadOnlyList<JsonResumeEducation> Education,
    IReadOnlyList<JsonResumeCertificate> Certificates,
    IReadOnlyList<JsonResumeSkill> Skills,
    IReadOnlyList<JsonResumeProject> Projects,
    JsonResumeMeta Meta
  );

  private sealed record JsonResumeBasics(
    string Name,
    string Label,
    string Email,
    string? Phone,
    string Summary,
    JsonResumeLocation Location,
    IReadOnlyList<JsonResumeProfile> Profiles
  );

  private sealed record JsonResumeLocation(string City);

  private sealed record JsonResumeProfile(string Network, string Username, Uri Url);

  private sealed record JsonResumeWork(
    string Name,
    string Position,
    string Location,
    string StartDate,
    string? EndDate,
    string? Summary,
    IReadOnlyList<string> Highlights
  );

  private sealed record JsonResumeEducation(string Institution, string StudyType);

  private sealed record JsonResumeCertificate(string Name);

  private sealed record JsonResumeSkill(string Name, IReadOnlyList<string> Keywords);

  private sealed record JsonResumeProject(
    string Name,
    string Description,
    Uri? Url,
    IReadOnlyList<string>? Keywords
  );

  private sealed record JsonResumeMeta(string Version, string LastModified, Uri Canonical);
}
