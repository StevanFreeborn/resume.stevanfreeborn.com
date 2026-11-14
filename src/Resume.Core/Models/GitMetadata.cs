using System.Reflection;

namespace Resume.Core.Models;

public static class GitMetadata
{
  public static string CommitSha { get; } = ResolveCommitSha();

  private static string ResolveCommitSha()
  {
    var attribute = typeof(GitMetadata).Assembly
      .GetCustomAttributes<AssemblyMetadataAttribute>()
      .FirstOrDefault(static a => string.Equals(a.Key, "GitCommitSha", StringComparison.Ordinal));

    return string.IsNullOrWhiteSpace(attribute?.Value) ? "dev" : attribute.Value;
  }
}
