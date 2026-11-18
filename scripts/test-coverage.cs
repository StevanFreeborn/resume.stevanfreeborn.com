using System.Diagnostics;

var configuration = "Release";
var noBuild = false;
var noRestore = false;
var openReport = false;

for (var i = 0; i < args.Length; i++)
{
  var arg = args[i];

  switch (arg)
  {
    case "-c":
    case "--configuration":
      if (i + 1 >= args.Length)
      {
        Console.Error.WriteLine($"Missing value for '{arg}'.");
        PrintUsage();
        return 1;
      }

      configuration = args[++i];
      break;
    case "--no-build":
      noBuild = true;
      break;
    case "--no-restore":
      noRestore = true;
      break;
    case "--open":
      openReport = true;
      break;
    case "-h":
    case "--help":
      PrintUsage();
      return 0;
    default:
      Console.Error.WriteLine($"Unknown option '{arg}'.");
      PrintUsage();
      return 1;
  }
}

var entryPath = AppContext.GetData("EntryPointFilePath") as string;
var scriptDir = string.IsNullOrEmpty(entryPath) ? null : Path.GetDirectoryName(entryPath);
var repoRoot = string.IsNullOrEmpty(scriptDir)
  ? Environment.CurrentDirectory
  : Path.GetFullPath(Path.Combine(scriptDir, ".."));

var settingsPath = Path.Combine(repoRoot, "tests", "coverage.settings.xml");
var resultsDir = Path.Combine(repoRoot, "TestResults");
var reportDir = Path.Combine(resultsDir, "CoverageReport");

if (Directory.Exists(resultsDir))
{
  Directory.Delete(resultsDir, recursive: true);
}

var testArgs = new List<string>
{
  "test",
  "-c",
  configuration,
};

if (noBuild)
{
  testArgs.Add("--no-build");
}
else if (noRestore)
{
  testArgs.Add("--no-restore");
}

testArgs.AddRange(
[
  "--coverage",
  "--coverage-output-format",
  "cobertura",
  "--coverage-settings",
  settingsPath,
  "--results-directory",
  resultsDir,
]);

var testExitCode = RunProcess("dotnet", testArgs, repoRoot);

if (testExitCode is not 0)
{
  return testExitCode;
}

var mergedDir = Path.Combine(resultsDir, "merged");
var coverageFiles = Directory.Exists(mergedDir)
  ? Directory.GetFiles(mergedDir, "*.cobertura.xml")
  : [];

if (coverageFiles.Length is 0 && Directory.Exists(resultsDir))
{
  coverageFiles = Directory.GetFiles(resultsDir, "*.cobertura.xml");
}

if (coverageFiles.Length is 0)
{
  Console.Error.WriteLine($"No Cobertura coverage reports were generated under '{resultsDir}'.");
  return 1;
}

var reportsArg = string.Join(';', coverageFiles);

if (noRestore is false)
{
  var restoreExitCode = RunProcess("dotnet", ["tool", "restore"], repoRoot);

  if (restoreExitCode is not 0)
  {
    return restoreExitCode;
  }
}

var reportExitCode = RunProcess(
  "dotnet",
  [
    "reportgenerator",
    $"-reports:{reportsArg}",
    $"-targetdir:{reportDir}",
    "-reporttypes:Html_Dark;TextSummary;MarkdownSummaryGithub",
    "-verbosity:Warning",
  ],
  repoRoot);

if (reportExitCode is not 0)
{
  return reportExitCode;
}

var summaryPath = Path.Combine(reportDir, "Summary.txt");

if (File.Exists(summaryPath))
{
  Console.WriteLine();
  Console.WriteLine(File.ReadAllText(summaryPath));
}

var indexHtmlPath = Path.Combine(reportDir, "index.html");
Console.WriteLine($"HTML report: {indexHtmlPath}");

var isCi = string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

if (openReport && isCi is false && File.Exists(indexHtmlPath))
{
  OpenBrowser(indexHtmlPath);
}

return 0;

static int RunProcess(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
{
  var startInfo = new ProcessStartInfo(fileName)
  {
    WorkingDirectory = workingDirectory,
    UseShellExecute = false,
  };

  foreach (var argument in arguments)
  {
    startInfo.ArgumentList.Add(argument);
  }

  using var process = Process.Start(startInfo);

  if (process is null)
  {
    Console.Error.WriteLine($"Failed to start '{fileName}'.");
    return 1;
  }

  process.WaitForExit();

  return process.ExitCode;
}

static void OpenBrowser(string urlOrPath)
{
  ProcessStartInfo startInfo;

  if (OperatingSystem.IsWindows())
  {
    startInfo = new ProcessStartInfo(urlOrPath)
    {
      UseShellExecute = true,
    };
  }
  else if (OperatingSystem.IsMacOS())
  {
    startInfo = new ProcessStartInfo("open")
    {
      UseShellExecute = false,
    };
    startInfo.ArgumentList.Add(urlOrPath);
  }
  else
  {
    startInfo = new ProcessStartInfo("xdg-open")
    {
      UseShellExecute = false,
    };
    startInfo.ArgumentList.Add(urlOrPath);
  }

  using var browserProcess = Process.Start(startInfo);
}

static void PrintUsage()
{
  Console.WriteLine("Usage: dotnet run scripts/test-coverage.cs [-- [-c|--configuration <Debug|Release>] [--no-build] [--no-restore] [--open]]");
}
