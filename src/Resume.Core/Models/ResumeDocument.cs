namespace Resume.Core.Models;

public sealed record ContactLink(string Label, Uri Url);

public sealed record ResumeHeader(
  string Name,
  string Headline,
  string Location,
  string? Phone,
  string Email,
  IReadOnlyList<ContactLink> Links
);

public sealed record SkillGroup(string Category, IReadOnlyList<string> Items);

public sealed record WorkExperience(
  string Title,
  string Company,
  string DateRange,
  string Location,
  string? Subtitle,
  IReadOnlyList<RichText> Bullets
);

public sealed record ProjectItem(
  string Name,
  string? Qualifier,
  Uri? Url,
  RichText Description
);

public sealed record EducationItem(string Credential, string Institution);

public sealed record ResumeTheme(
  string AccentHex = "1F4E79",
  string MutedHex = "404040",
  string FontFamily = "Carlito"
);

public sealed record ResumeDocument(
  ResumeHeader Header,
  RichText Summary,
  IReadOnlyList<SkillGroup> Skills,
  IReadOnlyList<WorkExperience> Experience,
  IReadOnlyList<ProjectItem> Projects,
  IReadOnlyList<EducationItem> Education,
  IReadOnlyList<string> Certifications,
  ResumeTheme Theme
);
