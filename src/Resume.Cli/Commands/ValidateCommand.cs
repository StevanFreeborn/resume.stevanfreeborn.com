using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using Spectre.Console;
using Spectre.Console.Cli;

using Resume.Core.Data;
using Resume.Core.Models;
using Resume.Core.Rendering;
using Resume.Core.Validation;

namespace Resume.Cli.Commands;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Spectre.Console.Cli")]
internal sealed class ValidateSettings : CommandSettings
{
  [CommandOption("-i|--input <DIR>")]
  [Description("Optional directory containing generated resume files to validate. If omitted, validates freshly rendered outputs in memory.")]
  public string? InputDirectory { get; init; }

  [CommandOption("-f|--filename <NAME>")]
  [Description("Base filename without extension when validating an input directory.")]
  [DefaultValue("stevan_freeborn_resume")]
  public string Filename { get; init; } = "stevan_freeborn_resume";

  [CommandOption("--max-pages <N>")]
  [Description("Maximum allowed PDF page count.")]
  [DefaultValue(2)]
  public int MaxPages { get; init; } = 2;
}

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Spectre.Console.Cli")]
internal sealed class ValidateCommand(
  ResumeDocument document,
  IEnumerable<IResumeRenderer> renderers) : Command<ValidateSettings>
{
  private readonly Dictionary<ResumeFormat, IResumeRenderer> _renderers = renderers.ToDictionary(static r => r.Format);

  public override int Execute(CommandContext context, ValidateSettings settings, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var renderContext = new RenderContext(
      CommitSha: GitMetadata.CommitSha,
      GeneratedAtUtc: DateTimeOffset.UtcNow,
      RepositoryUrl: StevanResume.RepositoryUrl
    );

    var allErrors = new List<string>();

    allErrors.AddRange(ResumeArtifactValidator.ValidateLinks(document));

    var pdfBytes = LoadOrRender(settings, _renderers[ResumeFormat.Pdf], document, renderContext);
    allErrors.AddRange(ResumeArtifactValidator.ValidatePdf(pdfBytes, settings.MaxPages));
    var pageCount = ResumeArtifactValidator.CountPdfPages(pdfBytes);

    var docxBytes = LoadOrRender(settings, _renderers[ResumeFormat.Docx], document, renderContext);
    allErrors.AddRange(ResumeArtifactValidator.ValidateDocx(docxBytes));

    var jsonBytes = LoadOrRender(settings, _renderers[ResumeFormat.Json], document, renderContext);
    allErrors.AddRange(ResumeArtifactValidator.ValidateJson(jsonBytes));

    if (allErrors.Count > 0)
    {
      foreach (var error in allErrors)
      {
        AnsiConsole.MarkupLine($"[red]FAIL:[/] {Markup.Escape(error)}");
      }

      return 1;
    }

    AnsiConsole.MarkupLine($"[green]PASS:[/] All validations succeeded (PDF pages: {pageCount}/{settings.MaxPages}, DOCX OpenXML errors: 0, JSON Resume valid).");
    return 0;
  }

  private static byte[] LoadOrRender(
    ValidateSettings settings,
    IResumeRenderer renderer,
    ResumeDocument document,
    RenderContext renderContext)
  {
    if (string.IsNullOrWhiteSpace(settings.InputDirectory))
    {
      return renderer.Render(document, renderContext);
    }

    var filePath = Path.Combine(settings.InputDirectory, $"{settings.Filename}{renderer.FileExtension}");

    if (File.Exists(filePath) is false)
    {
      throw new FileNotFoundException($"Expected generated artifact not found: '{filePath}'.", filePath);
    }

    return File.ReadAllBytes(filePath);
  }
}
