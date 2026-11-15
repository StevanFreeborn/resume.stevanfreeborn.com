using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using Resume.Core.Models;

namespace Resume.Core.Rendering;

public sealed class PdfResumeRenderer : IResumeRenderer
{
  public ResumeFormat Format => ResumeFormat.Pdf;

  public string ContentType => "application/pdf";

  public string FileExtension => ".pdf";

  public string RouteSegment => "pdf";

  public byte[] Render(ResumeDocument document, RenderContext context)
  {
    ArgumentNullException.ThrowIfNull(document);
    ArgumentNullException.ThrowIfNull(context);

    FontBootstrapper.EnsureInitialized();

    var accent = $"#{document.Theme.AccentHex}";
    var muted = $"#{document.Theme.MutedHex}";
    var fontFamily = document.Theme.FontFamily;

    var pdf = Document.Create(container =>
    {
      container.Page(page =>
      {
        page.Size(PageSizes.Letter);
        page.MarginVertical(0.5f, Unit.Inch);
        page.MarginHorizontal(0.6f, Unit.Inch);
        page.DefaultTextStyle(style => style
          .FontFamily(fontFamily)
          .FontSize(9.75f)
          .LineHeight(1.14f)
          .FontColor("#1A1A1A")
        );

        page.Content().Column(col =>
        {
          col.Spacing(0);

          ComposeHeader(col, document.Header, accent, muted);

          col.Item().ShowEntire().Column(summaryCol =>
          {
            ComposeSectionTitle(summaryCol, "SUMMARY", accent);
            summaryCol.Item().PaddingBottom(2).Text(text => RenderRichText(text, document.Summary, accent));
          });

          if (document.Skills.Count > 0)
          {
            col.Item().ShowEntire().Column(skillsFirst =>
            {
              ComposeSectionTitle(skillsFirst, "SKILLS", accent);
              ComposeSkillGroup(skillsFirst, document.Skills[0]);
            });

            for (var i = 1; i < document.Skills.Count; i++)
            {
              ComposeSkillGroup(col, document.Skills[i]);
            }
          }

          if (document.Experience.Count > 0)
          {
            col.Item().ShowEntire().Column(expHead =>
            {
              ComposeSectionTitle(expHead, "EXPERIENCE", accent);
              ComposeRoleHeaderAndFirstBullet(expHead, document.Experience[0], accent, muted);
            });

            for (var b = 1; b < document.Experience[0].Bullets.Count; b++)
            {
              ComposeBullet(col, document.Experience[0].Bullets[b], accent);
            }

            for (var r = 1; r < document.Experience.Count; r++)
            {
              var role = document.Experience[r];

              col.Item().ShowEntire().Column(roleHead => ComposeRoleHeaderAndFirstBullet(roleHead, role, accent, muted));

              for (var b = 1; b < role.Bullets.Count; b++)
              {
                ComposeBullet(col, role.Bullets[b], accent);
              }
            }
          }

          if (document.Projects.Count > 0)
          {
            col.Item().ShowEntire().Column(projHead =>
            {
              ComposeSectionTitle(projHead, "SELECTED PROJECTS  (GITHUB.COM/STEVANFREEBORN)", accent);
              ComposeProjectItem(projHead, document.Projects[0], accent);
            });

            for (var i = 1; i < document.Projects.Count; i++)
            {
              ComposeProjectItem(col, document.Projects[i], accent);
            }
          }

          col.Item().ShowEntire().Column(eduCol =>
          {
            ComposeSectionTitle(eduCol, "EDUCATION & CERTIFICATIONS", accent);

            foreach (var edu in document.Education)
            {
              eduCol.Item().PaddingBottom(1.5f).Text(text =>
              {
                text.Span(edu.Credential).Bold();
                text.Span($"  |  {edu.Institution}");
              });
            }

            eduCol.Item().PaddingBottom(1.5f).Text(text =>
            {
              text.Span("Certifications").Bold();
              text.Span($"  |  {string.Join("  •  ", document.Certifications)}");
            });
          });
        });

        page.Footer().PaddingTop(4).Row(row =>
        {
          row.RelativeItem().Text($"{document.Header.Name}  •  commit {context.CommitSha}")
            .FontSize(8f)
            .FontColor("#666666");

          row.AutoItem().Text(text =>
          {
            text.DefaultTextStyle(s => s.FontSize(8f).FontColor("#666666"));
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
          });
        });
      });
    });

    pdf.WithMetadata(new DocumentMetadata
    {
      Title = $"{document.Header.Name} - Resume",
      Author = document.Header.Name,
      Subject = $"Git commit: {context.CommitSha}",
      Keywords = "software quality engineer, test automation, playwright, c#, .net",
      Creator = "Resume.Core (QuestPDF)",
      CreationDate = context.GeneratedAtUtc.UtcDateTime,
      ModifiedDate = context.GeneratedAtUtc.UtcDateTime
    });

    return pdf.GeneratePdf();
  }

  private static void ComposeHeader(ColumnDescriptor col, ResumeHeader header, string accent, string muted)
  {
    col.Item().AlignCenter().Text(header.Name.ToUpperInvariant())
      .FontSize(20f)
      .Bold()
      .FontColor(accent);

    col.Item().AlignCenter().PaddingTop(1).Text(header.Headline)
      .FontSize(10.5f)
      .Bold()
      .FontColor(muted);

    col.Item().AlignCenter().PaddingTop(2).PaddingBottom(2).Text(text =>
    {
      text.DefaultTextStyle(s => s.FontSize(9f).FontColor("#1A1A1A"));
      text.Span(header.Location);

      if (string.IsNullOrWhiteSpace(header.Phone) is false)
      {
        text.Span($"  |  {header.Phone}");
      }

      text.Span("  |  ");
      text.Hyperlink(header.Email, $"mailto:{header.Email}").FontColor(accent);

      foreach (var link in header.Links)
      {
        text.Span("  |  ");
        text.Hyperlink(link.Label, link.Url.ToString()).FontColor(accent);
      }
    });
  }

  private static void ComposeSkillGroup(ColumnDescriptor col, SkillGroup group)
  {
    col.Item().PaddingBottom(1.5f).Text(text =>
    {
      text.Span($"{group.Category}: ").Bold();
      text.Span(string.Join(", ", group.Items));
    });
  }

  private static void ComposeRoleHeaderAndFirstBullet(
    ColumnDescriptor col,
    WorkExperience role,
    string accent,
    string muted
  )
  {
    col.Item().PaddingTop(4).Column(roleCol =>
    {
      roleCol.Item().Row(row =>
      {
        row.RelativeItem().Text(text =>
        {
          text.Span(role.Title).Bold().FontSize(10.5f);
          text.Span($"  |  {role.Company}").Bold().FontSize(10.5f).FontColor(accent);
        });

        row.AutoItem().AlignRight().Text(role.DateRange).Italic().FontSize(9.5f).FontColor(muted);
      });

      var metaLine = string.IsNullOrWhiteSpace(role.Subtitle)
        ? role.Location
        : $"{role.Location}  •  {role.Subtitle}";

      roleCol.Item().PaddingBottom(2).Text(metaLine).Italic().FontSize(9f).FontColor(muted);

      if (role.Bullets.Count > 0)
      {
        ComposeBullet(roleCol, role.Bullets[0], accent);
      }
    });
  }

  private static void ComposeBullet(ColumnDescriptor col, RichText bullet, string accent)
  {
    col.Item().PaddingLeft(8).PaddingBottom(2).Row(bulletRow =>
    {
      bulletRow.ConstantItem(10).Text("•");
      bulletRow.RelativeItem().Text(text => RenderRichText(text, bullet, accent));
    });
  }

  private static void ComposeProjectItem(ColumnDescriptor col, ProjectItem project, string accent)
  {
    col.Item().PaddingLeft(8).PaddingBottom(2).Row(projectRow =>
    {
      projectRow.ConstantItem(10).Text("•");
      projectRow.RelativeItem().Text(text =>
      {
        var label = project.Qualifier is null
          ? $"{project.Name}:"
          : $"{project.Name} ({project.Qualifier}):";

        if (project.Url is not null)
        {
          text.Hyperlink(label, project.Url.ToString()).Bold().FontColor(accent);
        }
        else
        {
          text.Span(label).Bold();
        }

        text.Span(" ");
        RenderRichText(text, project.Description, accent);
      });
    });
  }

  private static void ComposeSectionTitle(ColumnDescriptor col, string title, string accent)
  {
    col.Item()
      .PaddingTop(8)
      .PaddingBottom(3)
      .BorderBottom(1)
      .BorderColor(accent)
      .PaddingBottom(1)
      .Text(title)
      .FontSize(10.5f)
      .Bold()
      .FontColor(accent);
  }

  private static void RenderRichText(TextDescriptor text, RichText richText, string accent)
  {
    foreach (var run in richText.Runs)
    {
      switch (run)
      {
        case PlainRun plain:
          text.Span(plain.Text);
          break;
        case BoldRun bold:
          text.Span(bold.Text).Bold();
          break;
        case LinkRun link:
          text.Hyperlink(link.Label, link.Url.ToString()).FontColor(accent).Underline();
          break;
      }
    }
  }
}
