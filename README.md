# resume.stevanfreeborn.com

A resume-as-code solution built in **.NET 11** and served at **[resume.stevanfreeborn.com](https://resume.stevanfreeborn.com)** and **[steva.nz/resume](https://steva.nz/resume)**.

Career data is authored once as strongly typed C# records in `Resume.Core` and rendered into multiple formats, available both locally via CLI and over HTTP.

## Live Endpoints

All routes are served at both the root of `resume.stevanfreeborn.com` and under `/resume` (for `steva.nz/resume`):

| Route                                                 | Format                        | Notes                                                                  |
|-------------------------------------------------------|-------------------------------|------------------------------------------------------------------------|
| [`/`](https://resume.stevanfreeborn.com/)             | HTML (or `Accept` negotiated) | Document-style web view with light/dark mode and print styling         |
| [`/pdf`](https://resume.stevanfreeborn.com/pdf)       | PDF                           | Inline browser preview (`?download=true` for attachment)               |
| [`/docx`](https://resume.stevanfreeborn.com/docx)     | DOCX                          | ATS-clean Microsoft Word attachment                                    |
| [`/md`](https://resume.stevanfreeborn.com/md)         | Markdown                      | GitHub-flavored Markdown                                               |
| [`/html`](https://resume.stevanfreeborn.com/html)     | HTML                          | Standalone printable HTML export                                       |
| [`/json`](https://resume.stevanfreeborn.com/json)     | JSON Resume                   | [JSON Resume](https://jsonresume.org/schema/) schema                   |
| [`/txt`](https://resume.stevanfreeborn.com/txt)       | Plain Text                    | Terminal-friendly UTF-8 (`curl https://resume.stevanfreeborn.com/txt`) |
| [`/health`](https://resume.stevanfreeborn.com/health) | JSON                          | Service health and Git commit SHA                                      |

## Architecture

- **`src/Resume.Core`** — Strongly typed models, canonical data (`StevanResume`), JSON variant overlays, embedded Carlito fonts, six format renderers, and artifact validation.
- **`src/Resume.Cli`** — `Spectre.Console.Cli` tool (`generate` and `validate`) for local authoring, `dotnet watch`, and CI gates.
- **`src/Resume.Web`** — Minimal API service using built-in .NET DI with SHA-256 `ETag`-cached artifacts and SDK container publishing.
- **`tests/Resume.Core.Tests`, `tests/Resume.Web.Tests`, & `tests/Resume.Cli.Tests`** — xUnit v3 + AwesomeAssertions unit.

## Local Development

Generate all resume formats into `./artifacts`:

```powershell
dotnet run --project src/Resume.Cli -- generate --output ./artifacts
```

Watch for changes to `Resume.Core` and automatically regenerate (gracefully skips `.docx` if locked open in Microsoft Word):

```powershell
dotnet watch --project src/Resume.Cli run -- generate --output ./artifacts --filename dev_stevan_freeborn_resume
```

Run automated validation gates and unit/integration tests:

```powershell
dotnet test -c Release
dotnet run --project src/Resume.Cli -- validate --input ./artifacts
```

Run tests with merged Cobertura coverage and generate the HTML dark-mode + summary report into `TestResults/CoverageReport`:

```powershell
dotnet run scripts/test-coverage.cs
dotnet run scripts/test-coverage.cs -- --open
```

### Tailored Variants

To tailor a resume for a specific job without altering the public site, copy [`variants/example.variant.json`](variants/example.variant.json) to a gitignored file (e.g. `variants/custom.json`) and generate:

```powershell
dotnet run --project src/Resume.Cli -- generate --variant ./variants/custom.json --formats pdf,docx --output ./out --filename stevan_freeborn_custom
```

## Container Build & Deployment

This project uses .NET's built-in SDK container publishing (`PublishContainer` targeting `mcr.microsoft.com/dotnet/aspnet:11.0-resolute-chiseled-extra` and `linux-arm64` by default).

Test locally on x64 Docker Desktop:

```powershell
dotnet publish src/Resume.Web/Resume.Web.csproj -c Release -t:PublishContainer /p:PublishContainer=true /p:RuntimeIdentifier=linux-x64 /p:ContainerRepository=resume-local /p:ContainerImageTags=test
docker run -d --rm -p 5100:8080 --name resume-local resume-local:test
```
