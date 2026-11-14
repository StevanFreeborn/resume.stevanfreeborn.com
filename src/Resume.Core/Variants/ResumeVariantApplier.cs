using Resume.Core.Models;

namespace Resume.Core.Variants;

public static class ResumeVariantApplier
{
  public static ResumeDocument Apply(ResumeDocument baseDocument, ResumeVariant variant)
  {
    ArgumentNullException.ThrowIfNull(baseDocument);
    ArgumentNullException.ThrowIfNull(variant);

    var header = baseDocument.Header;

    if (variant.Headline is not null)
    {
      header = header with { Headline = variant.Headline };
    }

    if (variant.IncludePhone is false)
    {
      header = header with { Phone = null };
    }
    else if (variant.PhoneOverride is not null)
    {
      header = header with { Phone = variant.PhoneOverride };
    }

    var summary = variant.Summary is not null
      ? RichText.FromPlain(variant.Summary)
      : baseDocument.Summary;

    var skills = ApplySkills(baseDocument.Skills, variant.AdditionalSkills);
    var projects = ApplyProjects(baseDocument.Projects, variant.ProjectNames);
    var experience = ApplyExperience(baseDocument.Experience, variant.ExperienceOverrides);

    return baseDocument with
    {
      Header = header,
      Summary = summary,
      Skills = skills,
      Projects = projects,
      Experience = experience
    };
  }

  private static IReadOnlyList<SkillGroup> ApplySkills(
    IReadOnlyList<SkillGroup> baseSkills,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? additionalSkills
  )
  {
    if (additionalSkills is null || additionalSkills.Count is 0)
    {
      return baseSkills;
    }

    var result = new List<SkillGroup>(baseSkills.Count);
    var handled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var group in baseSkills)
    {
      if (additionalSkills.TryGetValue(group.Category, out var extra))
      {
        handled.Add(group.Category);
        var merged = group.Items
          .Concat(extra)
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .ToList();
        result.Add(group with { Items = merged });
      }
      else
      {
        result.Add(group);
      }
    }

    foreach (var kvp in additionalSkills)
    {
      if (handled.Contains(kvp.Key) is false)
      {
        result.Add(new SkillGroup(kvp.Key, kvp.Value));
      }
    }

    return result;
  }

  private static IReadOnlyList<ProjectItem> ApplyProjects(
    IReadOnlyList<ProjectItem> baseProjects,
    IReadOnlyList<string>? projectNames
  )
  {
    if (projectNames is null || projectNames.Count is 0)
    {
      return baseProjects;
    }

    var byName = baseProjects.ToDictionary(static p => p.Name, StringComparer.OrdinalIgnoreCase);
    var selected = new List<ProjectItem>(projectNames.Count);

    foreach (var name in projectNames)
    {
      if (byName.TryGetValue(name, out var project))
      {
        selected.Add(project);
      }
    }

    return selected;
  }

  private static IReadOnlyList<WorkExperience> ApplyExperience(
    IReadOnlyList<WorkExperience> baseExperience,
    IReadOnlyList<ExperienceOverride>? overrides
  )
  {
    if (overrides is null || overrides.Count is 0)
    {
      return baseExperience;
    }

    var result = new List<WorkExperience>(baseExperience.Count);

    foreach (var role in baseExperience)
    {
      var match = overrides.FirstOrDefault(o =>
        string.Equals(o.Company, role.Company, StringComparison.OrdinalIgnoreCase)
        && (o.DateRange is null || string.Equals(o.DateRange, role.DateRange, StringComparison.OrdinalIgnoreCase))
      );

      if (match is null)
      {
        result.Add(role);
        continue;
      }

      var bullets = match.Bullets is not null
        ? match.Bullets.Select(RichText.FromPlain).ToList()
        : [.. role.Bullets];

      if (match.AppendBullets is not null)
      {
        bullets.AddRange(match.AppendBullets.Select(RichText.FromPlain));
      }

      result.Add(role with { Bullets = bullets });
    }

    return result;
  }
}
