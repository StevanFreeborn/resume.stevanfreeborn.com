using System.Text;

namespace Resume.Cli.Tests;

[CollectionDefinition("WorkingDirectory", DisableParallelization = true)]
public sealed class WorkingDirectoryFixture;

[Collection("WorkingDirectory")]
public sealed class CliAppTests
{
  [Fact]
  public async Task Generate_CreatesAllSixFormats_And_ValidateInputPasses()
  {
    var tempDir = CreateTempDirectory();

    try
    {
      var genExit = await CliApp.RunAsync(["generate", "--output", tempDir]);
      genExit.Should().Be(0);

      Directory.GetFiles(tempDir).Should().HaveCount(6);
      File.Exists(Path.Combine(tempDir, "stevan_freeborn_resume.pdf")).Should().BeTrue();
      File.Exists(Path.Combine(tempDir, "stevan_freeborn_resume.docx")).Should().BeTrue();
      File.Exists(Path.Combine(tempDir, "stevan_freeborn_resume.html")).Should().BeTrue();
      File.Exists(Path.Combine(tempDir, "stevan_freeborn_resume.md")).Should().BeTrue();
      File.Exists(Path.Combine(tempDir, "stevan_freeborn_resume.json")).Should().BeTrue();
      File.Exists(Path.Combine(tempDir, "stevan_freeborn_resume.txt")).Should().BeTrue();

      var valExit = await CliApp.RunAsync(["validate", "--input", tempDir]);
      valExit.Should().Be(0);
    }
    finally
    {
      DeleteTempDirectory(tempDir);
    }
  }

  [Fact]
  public async Task Generate_WithFormatsFilterAndVariant_WritesSubsetWithOverrides()
  {
    var tempDir = CreateTempDirectory();

    try
    {
      var variantPath = Path.Combine(tempDir, "variant.json");

      await File.WriteAllTextAsync(
        variantPath,
        """
        {
          "headline": "Principal Automation Lead",
          "summary": "Custom summary for CLI test.",
          "includePhone": false
        }
        """,
        TestContext.Current.CancellationToken
      );

      var outDir = Path.Combine(tempDir, "out");
      var exitCode = await CliApp.RunAsync(
        ["generate", "--output", outDir, "--filename", "custom", "--formats", "pdf,md", "--variant", variantPath]
      );

      exitCode.Should().Be(0);
      Directory.GetFiles(outDir).Should().HaveCount(2);

      var md = await File.ReadAllTextAsync(Path.Combine(outDir, "custom.md"), TestContext.Current.CancellationToken);
      md.Should().Contain("Principal Automation Lead");
      md.Should().Contain("Custom summary for CLI test.");
      md.Should().NotContain("417-540-8870");
    }
    finally
    {
      DeleteTempDirectory(tempDir);
    }
  }

  [Fact]
  public async Task Generate_WithInvalidFormat_ReturnsNonZeroExitCode()
  {
    var tempDir = CreateTempDirectory();

    try
    {
      var exitCode = await CliApp.RunAsync(["generate", "--output", tempDir, "--formats", "not-a-format"]);
      exitCode.Should().Be(1);
    }
    finally
    {
      DeleteTempDirectory(tempDir);
    }
  }

  [Fact]
  public async Task Generate_SkipsLockedDocxGracefully_AndWritesOtherFormats()
  {
    var tempDir = CreateTempDirectory();

    try
    {
      var docxPath = Path.Combine(tempDir, "stevan_freeborn_resume.docx");

      using var lockStream = new FileStream(docxPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

      var exitCode = await CliApp.RunAsync(["generate", "--output", tempDir]);

      exitCode.Should().Be(0);
      lockStream.Length.Should().Be(0);

      File.Exists(Path.Combine(tempDir, "stevan_freeborn_resume.pdf")).Should().BeTrue();
      File.Exists(Path.Combine(tempDir, "stevan_freeborn_resume.txt")).Should().BeTrue();
    }
    finally
    {
      DeleteTempDirectory(tempDir);
    }
  }

  [Fact]
  public async Task Generate_WithReadmeFlag_WritesReadmeInWorkingDirectory()
  {
    var tempDir = CreateTempDirectory();
    var originalDir = Directory.GetCurrentDirectory();

    try
    {
      Directory.SetCurrentDirectory(tempDir);

      var outDir = Path.Combine(tempDir, "out");

      var exitCode = await CliApp.RunAsync(["generate", "--output", outDir, "--formats", "txt", "--readme"]);

      exitCode.Should().Be(0);
      File.Exists(Path.Combine(tempDir, "README.md")).Should().BeTrue();

      var readme = await File.ReadAllTextAsync(Path.Combine(tempDir, "README.md"), TestContext.Current.CancellationToken);
      readme.Should().StartWith("# Stevan Freeborn");
    }
    finally
    {
      Directory.SetCurrentDirectory(originalDir);
      DeleteTempDirectory(tempDir);
    }
  }

  [Fact]
  public async Task Validate_InMemory_ReturnsZero_And_StrictMaxPagesReturnsOne()
  {
    var passExit = await CliApp.RunAsync(["validate"]);
    passExit.Should().Be(0);

    var failExit = await CliApp.RunAsync(["validate", "--max-pages", "1"]);
    failExit.Should().Be(1);
  }

  [Fact]
  public async Task Validate_WithMissingOrInvalidInputFiles_ReturnsOne()
  {
    var tempDir = CreateTempDirectory();

    try
    {
      var missingExit = await CliApp.RunAsync(["validate", "--input", tempDir]);
      missingExit.Should().Be(1);

      var genExit = await CliApp.RunAsync(["generate", "--output", tempDir]);
      genExit.Should().Be(0);

      await File.WriteAllBytesAsync(
        Path.Combine(tempDir, "stevan_freeborn_resume.pdf"),
        Encoding.ASCII.GetBytes("not-a-pdf"),
        TestContext.Current.CancellationToken
      );

      var corruptExit = await CliApp.RunAsync(["validate", "--input", tempDir]);
      corruptExit.Should().Be(1);
    }
    finally
    {
      DeleteTempDirectory(tempDir);
    }
  }

  private static string CreateTempDirectory()
  {
    var path = Path.Combine(Path.GetTempPath(), "resume-cli-tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(path);
    return path;
  }

  private static void DeleteTempDirectory(string path)
  {
    if (Directory.Exists(path))
    {
      Directory.Delete(path, recursive: true);
    }
  }
}
