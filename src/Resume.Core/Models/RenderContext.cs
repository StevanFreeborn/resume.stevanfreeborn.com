namespace Resume.Core.Models;

public sealed record RenderContext(
  string CommitSha,
  DateTimeOffset GeneratedAtUtc,
  Uri? RepositoryUrl = null,
  bool IncludeWebToolbar = false,
  string BasePath = ""
);
