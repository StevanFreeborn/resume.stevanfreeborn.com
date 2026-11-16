using Microsoft.Extensions.DependencyInjection;

using Spectre.Console;
using Spectre.Console.Cli;

using Resume.Cli.Commands;
using Resume.Cli.Infrastructure;
using Resume.Core.Extensions;

namespace Resume.Cli;

internal static class CliApp
{
  public static async Task<int> RunAsync(string[] args)
  {
    ArgumentNullException.ThrowIfNull(args);

    var services = new ServiceCollection();
    services.AddResumeCore();

    var registrar = new TypeRegistrar(services);
    var app = new CommandApp(registrar);

    app.Configure(static config =>
    {
      config.SetApplicationName("resume-cli");
      config.PropagateExceptions();

      config.AddCommand<GenerateCommand>("generate")
        .WithDescription("Generate resume files across configured formats.")
        .WithExample("generate", "--output", "./artifacts");

      config.AddCommand<ValidateCommand>("validate")
        .WithDescription("Validate generated or in-memory resume artifacts.")
        .WithExample("validate", "--max-pages", "2");
    });

    try
    {
      return await app.RunAsync(args);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
      AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
      return 1;
    }
  }
}
