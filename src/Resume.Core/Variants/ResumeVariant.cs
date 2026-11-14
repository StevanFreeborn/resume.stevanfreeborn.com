using System.Text.Json;

namespace Resume.Core.Variants;

public sealed record ExperienceOverride(
  string Company,
  string? DateRange = null,
  IReadOnlyList<string>? Bullets = null,
  IReadOnlyList<string>? AppendBullets = null
);

public sealed record ResumeVariant(
  string? Headline = null,
  string? Summary = null,
  bool? IncludePhone = null,
  string? PhoneOverride = null,
  IReadOnlyDictionary<string, IReadOnlyList<string>>? AdditionalSkills = null,
  IReadOnlyList<string>? ProjectNames = null,
  IReadOnlyList<ExperienceOverride>? ExperienceOverrides = null
)
{
  private static readonly JsonSerializerOptions s_jsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true
  };

  public static ResumeVariant LoadFromFile(string path)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<ResumeVariant>(json, s_jsonOptions)
      ?? throw new InvalidOperationException($"Variant file '{path}' deserialized to null.");
  }
}
