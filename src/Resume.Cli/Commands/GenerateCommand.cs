using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using Spectre.Console;
using Spectre.Console.Cli;

using Resume.Core.Data;
using Resume.Core.Models;
using Resume.Core.Rendering;
using Resume.Core.Variants;

namespace Resume.Cli.Commands;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Spectre.Console.Cli")]
internal sealed class GenerateSettings : CommandSettings
{
  [CommandOption("-o|--output <DIR>")]
  [Description("Output directory for generated resume artifacts.")]
  [DefaultValue("./artifacts")]
  public string OutputDirectory { get; init; } = "./artifacts";

  [CommandOption("-f|--filename <NAME>")]
  [Description("Base filename without extension.")]
  [DefaultValue("stevan_freeborn_resume")]
  public string Filename { get; init; } = "stevan_freeborn_resume";

  [CommandOption("--formats <LIST>")]
  [Description("Comma-separated formats (pdf,docx,html,md,json,txt). Defaults to all.")]
  public string? Formats { get; init; }

  [CommandOption("-v|--variant <PATH>")]
  [Description("Optional path to a JSON variant overlay file.")]
  public string? VariantPath { get; init; }

  [CommandOption("--readme")]
  [Description("Also write README.md to the current working directory.")]
  public bool WriteReadme { get; init; }
}

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by Spectre.Console.Cli")]
internal sealed class GenerateCommand(
  ResumeDocument document,
  IEnumerable<IResumeRenderer> renderers) : Command<GenerateSettings>
{
  private readonly List<IResumeRenderer> _renderers = [.. renderers];

  public override int Execute(CommandContext context, GenerateSettings settings, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(settings);

    var targetDocument = document;

    if (string.IsNullOrWhiteSpace(settings.VariantPath) is false)
    {
      var variant = ResumeVariant.LoadFromFile(settings.VariantPath);
      targetDocument = ResumeVariantApplier.Apply(targetDocument, variant);
      AnsiConsole.MarkupLine($"[grey]Applied variant:[/] {Markup.Escape(settings.VariantPath)}");
    }

    var renderContext = new RenderContext(
      CommitSha: GitMetadata.CommitSha,
      GeneratedAtUtc: DateTimeOffset.UtcNow,
      RepositoryUrl: StevanResume.RepositoryUrl,
      IncludeWebToolbar: false
    );

    var targetRenderers = ResolveRenderers(settings.Formats);
    Directory.CreateDirectory(settings.OutputDirectory);

    foreach (var renderer in targetRenderers)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var outPath = Path.Combine(settings.OutputDirectory, $"{settings.Filename}{renderer.FileExtension}");
      var bytes = renderer.Render(targetDocument, renderContext);
      TryWriteFile(outPath, bytes);
    }

    if (settings.WriteReadme)
    {
      var mdRenderer = _renderers.First(static r => r.Format == ResumeFormat.Markdown);
      var mdBytes = mdRenderer.Render(targetDocument, renderContext);
      TryWriteFile(Path.Combine(Directory.GetCurrentDirectory(), "README.md"), mdBytes);
    }

    return 0;
  }

  private List<IResumeRenderer> ResolveRenderers(string? formatsOption)
  {
    if (string.IsNullOrWhiteSpace(formatsOption))
    {
      return _renderers;
    }

    var byRoute = _renderers.ToDictionary(static r => r.RouteSegment, StringComparer.OrdinalIgnoreCase);
    var tokens = formatsOption.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    var resolved = new List<IResumeRenderer>(tokens.Length);

    foreach (var token in tokens)
    {
      if (byRoute.TryGetValue(token, out var renderer) is false)
      {
        throw new InvalidOperationException($"Unsupported format '{token}'. Expected one of: pdf, docx, html, md, json, txt.");
      }

      resolved.Add(renderer);
    }

    return resolved;
  }

  private static void TryWriteFile(string path, byte[] bytes)
  {
    try
    {
      File.WriteAllBytes(path, bytes);
      AnsiConsole.MarkupLine($"[green]Generated[/] {Markup.Escape(path)} [grey]({bytes.Length:N0} bytes)[/]");
    }
    catch (IOException ex)
    {
      AnsiConsole.MarkupLine($"[yellow]Skipped locked file[/] {Markup.Escape(path)}: {Markup.Escape(ex.Message)}");
    }
  }
}
