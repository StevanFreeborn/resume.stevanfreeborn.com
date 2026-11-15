using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

using Resume.Core.Models;

namespace Resume.Core.Rendering;

public sealed class DocxResumeRenderer : IResumeRenderer
{
  private const string FontName = "Calibri";

  public ResumeFormat Format => ResumeFormat.Docx;

  public string ContentType => "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

  public string FileExtension => ".docx";

  public string RouteSegment => "docx";

  public byte[] Render(ResumeDocument document, RenderContext context)
  {
    ArgumentNullException.ThrowIfNull(document);
    ArgumentNullException.ThrowIfNull(context);

    using var stream = new MemoryStream();
    WritePackage(stream, document, context);
    return stream.ToArray();
  }

  private static void WritePackage(MemoryStream stream, ResumeDocument document, RenderContext context)
  {
    using var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true);
    var mainPart = wordDoc.AddMainDocumentPart();
    mainPart.Document = new Document();

    AddStyles(mainPart);
    AddNumbering(mainPart);

    var body = new Body();
    var accent = document.Theme.AccentHex;
    var muted = document.Theme.MutedHex;

    AppendHeader(body, mainPart, document.Header, accent, muted);

    AppendSectionHeading(body, "SUMMARY", accent);

    body.AppendChild(CreateRichParagraph(mainPart, document.Summary, spaceAfter: "40"));

    AppendSectionHeading(body, "SKILLS", accent);

    foreach (var group in document.Skills)
    {
      var p = CreateBaseParagraph(spaceAfter: "30");
      p.AppendChild(CreateRun($"{group.Category}: ", bold: true));
      p.AppendChild(CreateRun(string.Join(", ", group.Items)));
      body.AppendChild(p);
    }

    AppendSectionHeading(body, "EXPERIENCE", accent);

    foreach (var role in document.Experience)
    {
      var roleHeading = new Paragraph(
        new ParagraphProperties(
          new KeepNext(),
          new Tabs(new TabStop { Val = TabStopValues.Right, Position = 10368 }),
          new SpacingBetweenLines { Before = "100", After = "0", Line = "252", LineRule = LineSpacingRuleValues.Auto }
        )
      );
      roleHeading.AppendChild(CreateRun(role.Title, bold: true, fontSizeHalfPoints: "22"));
      roleHeading.AppendChild(CreateRun($"  |  {role.Company}", bold: true, colorHex: accent, fontSizeHalfPoints: "22"));
      roleHeading.AppendChild(new Run(new TabChar()));
      roleHeading.AppendChild(CreateRun(role.DateRange, italic: true, colorHex: muted, fontSizeHalfPoints: "21"));
      body.AppendChild(roleHeading);

      var metaText = string.IsNullOrWhiteSpace(role.Subtitle)
        ? role.Location
        : $"{role.Location}  •  {role.Subtitle}";
      var metaPara = new Paragraph(
        new ParagraphProperties(
          new KeepNext(),
          new SpacingBetweenLines { Before = "0", After = "40", Line = "252", LineRule = LineSpacingRuleValues.Auto }
        )
      );
      metaPara.AppendChild(CreateRun(metaText, italic: true, colorHex: muted, fontSizeHalfPoints: "19"));
      body.AppendChild(metaPara);

      foreach (var bullet in role.Bullets)
      {
        body.AppendChild(CreateBulletParagraph(mainPart, bullet));
      }
    }

    AppendSectionHeading(body, "SELECTED PROJECTS  (GITHUB.COM/STEVANFREEBORN)", accent);

    foreach (var project in document.Projects)
    {
      var bullet = CreateBulletParagraphShell();
      var label = project.Qualifier is null
        ? $"{project.Name}:"
        : $"{project.Name} ({project.Qualifier}):";

      if (project.Url is not null)
      {
        bullet.AppendChild(CreateHyperlink(mainPart, label, project.Url, bold: true, colorHex: accent));
      }
      else
      {
        bullet.AppendChild(CreateRun(label, bold: true));
      }

      bullet.AppendChild(CreateRun(" "));
      AppendRichRuns(bullet, mainPart, project.Description);
      body.AppendChild(bullet);
    }

    AppendSectionHeading(body, "EDUCATION & CERTIFICATIONS", accent);

    foreach (var edu in document.Education)
    {
      var p = CreateBaseParagraph(spaceAfter: "25");
      p.AppendChild(CreateRun(edu.Credential, bold: true));
      p.AppendChild(CreateRun($"  |  {edu.Institution}"));
      body.AppendChild(p);
    }

    var certPara = CreateBaseParagraph(spaceAfter: "25");
    certPara.AppendChild(CreateRun("Certifications", bold: true));
    certPara.AppendChild(CreateRun($"  |  {string.Join("  •  ", document.Certifications)}"));
    body.AppendChild(certPara);

    body.AppendChild(
      new SectionProperties(
        new PageSize { Width = 12240U, Height = 15840U },
        new PageMargin
        {
          Top = 792,
          Right = 936U,
          Bottom = 792,
          Left = 936U,
          Header = 720U,
          Footer = 720U,
          Gutter = 0U
        }
      )
    );

    mainPart.Document.AppendChild(body);
    mainPart.Document.Save();

    wordDoc.PackageProperties.Creator = document.Header.Name;
    wordDoc.PackageProperties.Title = $"{document.Header.Name} - Resume";
    wordDoc.PackageProperties.Description = $"Git commit: {context.CommitSha}";
    wordDoc.PackageProperties.Created = context.GeneratedAtUtc.UtcDateTime;
    wordDoc.PackageProperties.Modified = context.GeneratedAtUtc.UtcDateTime;
  }

  private static void AddStyles(MainDocumentPart mainPart)
  {
    var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();

    stylesPart.Styles = new Styles(
      new Style(
        new StyleName { Val = "Normal" },
        new PrimaryStyle(),
        new StyleParagraphProperties(
          new SpacingBetweenLines { After = "0", Before = "0", Line = "252", LineRule = LineSpacingRuleValues.Auto }
        ),
        new StyleRunProperties(
          new RunFonts { Ascii = FontName, HighAnsi = FontName, EastAsia = FontName, ComplexScript = FontName },
          new FontSize { Val = "21" },
          new FontSizeComplexScript { Val = "21" }
        )
      )
      {
        Type = StyleValues.Paragraph,
        StyleId = "Normal",
        Default = true
      },
      new Style(
        new StyleName { Val = "Hyperlink" },
        new StyleRunProperties(
          new Color { Val = "1F4E79" },
          new Underline { Val = UnderlineValues.Single }
        )
      )
      {
        Type = StyleValues.Character,
        StyleId = "Hyperlink"
      }
    );

    stylesPart.Styles.Save();
  }

  private static void AddNumbering(MainDocumentPart mainPart)
  {
    var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();

    numberingPart.Numbering = new Numbering(
      new AbstractNum(
        new MultiLevelType { Val = MultiLevelValues.HybridMultilevel },
        new Level(
          new StartNumberingValue { Val = 1 },
          new NumberingFormat { Val = NumberFormatValues.Bullet },
          new LevelText { Val = "•" },
          new LevelJustification { Val = LevelJustificationValues.Left },
          new PreviousParagraphProperties(
            new Indentation { Left = "360", Hanging = "180" }
          ),
          new NumberingSymbolRunProperties(
            new RunFonts { Ascii = FontName, HighAnsi = FontName, ComplexScript = FontName }
          )
        )
        {
          LevelIndex = 0
        }
      )
      {
        AbstractNumberId = 1
      },
      new NumberingInstance(
        new AbstractNumId { Val = 1 }
      )
      {
        NumberID = 1
      }
    );

    numberingPart.Numbering.Save();
  }

  private static void AppendHeader(
    Body body,
    MainDocumentPart mainPart,
    ResumeHeader header,
    string accent,
    string muted
  )
  {
    var namePara = new Paragraph(
      new ParagraphProperties(
        new SpacingBetweenLines { Before = "0", After = "20", Line = "240", LineRule = LineSpacingRuleValues.Auto },
        new Justification { Val = JustificationValues.Center }
      )
    );
    namePara.AppendChild(CreateRun(header.Name.ToUpperInvariant(), bold: true, colorHex: accent, fontSizeHalfPoints: "44"));
    body.AppendChild(namePara);

    var headlinePara = new Paragraph(
      new ParagraphProperties(
        new SpacingBetweenLines { Before = "0", After = "30", Line = "240", LineRule = LineSpacingRuleValues.Auto },
        new Justification { Val = JustificationValues.Center }
      )
    );
    headlinePara.AppendChild(CreateRun(header.Headline, bold: true, colorHex: muted, fontSizeHalfPoints: "23"));
    body.AppendChild(headlinePara);

    var contactPara = new Paragraph(
      new ParagraphProperties(
        new SpacingBetweenLines { Before = "20", After = "40", Line = "240", LineRule = LineSpacingRuleValues.Auto },
        new Justification { Val = JustificationValues.Center }
      )
    );
    contactPara.AppendChild(CreateRun(header.Location, fontSizeHalfPoints: "19"));

    if (string.IsNullOrWhiteSpace(header.Phone) is false)
    {
      contactPara.AppendChild(CreateRun($"  |  {header.Phone}", fontSizeHalfPoints: "19"));
    }

    contactPara.AppendChild(CreateRun("  |  ", fontSizeHalfPoints: "19"));
    contactPara.AppendChild(CreateHyperlink(mainPart, header.Email, new Uri($"mailto:{header.Email}"), fontSizeHalfPoints: "19", colorHex: accent));

    foreach (var link in header.Links)
    {
      contactPara.AppendChild(CreateRun("  |  ", fontSizeHalfPoints: "19"));
      contactPara.AppendChild(CreateHyperlink(mainPart, link.Label, link.Url, fontSizeHalfPoints: "19", colorHex: accent));
    }

    body.AppendChild(contactPara);
  }

  private static void AppendSectionHeading(Body body, string title, string accent)
  {
    var p = new Paragraph(
      new ParagraphProperties(
        new KeepNext(),
        new ParagraphBorders(
          new BottomBorder { Val = BorderValues.Single, Size = 8U, Space = 1U, Color = accent }
        ),
        new SpacingBetweenLines { Before = "160", After = "60", Line = "240", LineRule = LineSpacingRuleValues.Auto }
      )
    );
    p.AppendChild(CreateRun(title, bold: true, colorHex: accent, fontSizeHalfPoints: "23"));
    body.AppendChild(p);
  }

  private static Paragraph CreateBaseParagraph(string spaceAfter = "0")
  {
    return new Paragraph(
      new ParagraphProperties(
        new SpacingBetweenLines { Before = "0", After = spaceAfter, Line = "252", LineRule = LineSpacingRuleValues.Auto }
      )
    );
  }

  private static Paragraph CreateRichParagraph(MainDocumentPart mainPart, RichText richText, string spaceAfter = "0")
  {
    var p = CreateBaseParagraph(spaceAfter);
    AppendRichRuns(p, mainPart, richText);
    return p;
  }

  private static Paragraph CreateBulletParagraphShell()
  {
    return new Paragraph(
      new ParagraphProperties(
        new NumberingProperties(
          new NumberingLevelReference { Val = 0 },
          new NumberingId { Val = 1 }
        ),
        new SpacingBetweenLines { Before = "0", After = "40", Line = "252", LineRule = LineSpacingRuleValues.Auto }
      )
    );
  }

  private static Paragraph CreateBulletParagraph(MainDocumentPart mainPart, RichText richText)
  {
    var p = CreateBulletParagraphShell();
    AppendRichRuns(p, mainPart, richText);
    return p;
  }

  private static void AppendRichRuns(Paragraph paragraph, MainDocumentPart mainPart, RichText richText)
  {
    foreach (var run in richText.Runs)
    {
      switch (run)
      {
        case PlainRun plain:
          paragraph.AppendChild(CreateRun(plain.Text));
          break;
        case BoldRun bold:
          paragraph.AppendChild(CreateRun(bold.Text, bold: true));
          break;
        case LinkRun link:
          paragraph.AppendChild(CreateHyperlink(mainPart, link.Label, link.Url));
          break;
      }
    }
  }

  private static Run CreateRun(
    string text,
    bool bold = false,
    bool italic = false,
    string? colorHex = null,
    string? fontSizeHalfPoints = null
  )
  {
    var runProperties = new RunProperties
    {
      RunFonts = new RunFonts { Ascii = FontName, HighAnsi = FontName, EastAsia = FontName, ComplexScript = FontName }
    };

    if (bold)
    {
      runProperties.Bold = new Bold();
    }

    if (italic)
    {
      runProperties.Italic = new Italic();
    }

    if (colorHex is not null)
    {
      runProperties.Color = new Color { Val = colorHex };
    }

    if (fontSizeHalfPoints is not null)
    {
      runProperties.FontSize = new FontSize { Val = fontSizeHalfPoints };
      runProperties.FontSizeComplexScript = new FontSizeComplexScript { Val = fontSizeHalfPoints };
    }

    return new Run(
      runProperties,
      new Text(text) { Space = SpaceProcessingModeValues.Preserve }
    );
  }

  private static Hyperlink CreateHyperlink(
    MainDocumentPart mainPart,
    string label,
    Uri url,
    bool bold = false,
    string? colorHex = null,
    string? fontSizeHalfPoints = null
  )
  {
    var relationship = mainPart.AddHyperlinkRelationship(url, true);

    var runProperties = new RunProperties
    {
      RunStyle = new RunStyle { Val = "Hyperlink" },
      RunFonts = new RunFonts { Ascii = FontName, HighAnsi = FontName, EastAsia = FontName, ComplexScript = FontName }
    };

    if (bold)
    {
      runProperties.Bold = new Bold();
    }

    if (colorHex is not null)
    {
      runProperties.Color = new Color { Val = colorHex };
    }

    if (fontSizeHalfPoints is not null)
    {
      runProperties.FontSize = new FontSize { Val = fontSizeHalfPoints };
      runProperties.FontSizeComplexScript = new FontSizeComplexScript { Val = fontSizeHalfPoints };
    }

    return new Hyperlink(
      new Run(
        runProperties,
        new Text(label) { Space = SpaceProcessingModeValues.Preserve }
      )
    )
    {
      Id = relationship.Id,
      History = OnOffValue.FromBoolean(true)
    };
  }
}
