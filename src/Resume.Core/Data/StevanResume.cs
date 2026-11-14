using Resume.Core.Models;

namespace Resume.Core.Data;

public static class StevanResume
{
  public static Uri RepositoryUrl { get; } = new("https://github.com/StevanFreeborn/resume.stevanfreeborn.com");

  public static ResumeDocument Default { get; } = new(
    Header: new ResumeHeader(
      Name: "Stevan Freeborn",
      Headline: "Senior Software Quality Engineer  •  Test Automation Architect  •  Quality Platform Engineer",
      Location: "Olathe, KS",
      Phone: "417-540-8870",
      Email: "stevan.freeborn@gmail.com",
      Links:
      [
        new ContactLink("linkedin.com/in/stevan-freeborn", new Uri("https://www.linkedin.com/in/stevan-freeborn")),
        new ContactLink("github.com/StevanFreeborn", new Uri("https://github.com/StevanFreeborn")),
      ]
    ),
    Summary: RichText.FromPlain(
      "Quality engineer who builds test automation from the ground up. For six years I have designed the " +
      "frameworks, pipelines, and standards that let Onspring ship an enterprise ASP.NET platform with " +
      "confidence. I test across UI, API, and database layers; write production C#, TypeScript, and Rust; " +
      "and build the practical tooling—CLIs, agent skills, and MCP servers—that lets AI agents work reliably " +
      "against real systems. Before software, I worked as a food scientist under USDA and FDA regulations " +
      "after earning a degree in biochemistry, training me early in strict specifications, traceability, " +
      "and empirical testing."
    ),
    Skills:
    [
      new SkillGroup("Languages", ["C#", "TypeScript", "SQL", "Rust", "Python", "Go"]),
      new SkillGroup("Testing", [
        "Playwright (Screenplay pattern)",
        "xUnit",
        "Moq",
        "Vitest",
        "UI, API, integration, and regression automation",
        "flaky-test and race-condition diagnosis",
      ]),
      new SkillGroup(".NET & Web", ["ASP.NET Core", ".NET Aspire", "Razor", "Roslyn analyzers", "REST APIs", "Vue", "React"]),
      new SkillGroup("Data", ["PostgreSQL", "relational design and SQL", "SQLite", "Prisma", "test-data management"]),
      new SkillGroup("CI/CD & Infrastructure", [
        "GitHub Actions",
        "multi-environment release validation",
        "Docker",
        "cross-platform release pipelines",
        "Codecov",
        "semantic-release",
      ]),
      new SkillGroup("AI", [
        "Agent Skills",
        "Model Context Protocol",
        "LLM APIs (Anthropic, Gemini)",
        "Microsoft.Extensions.AI",
        "GitHub Copilot",
        "Claude Code",
        "Gemini",
      ]),
      new SkillGroup("Practices", [
        "Test architecture",
        "release-readiness standards",
        "quality metrics",
        "test planning",
        "root-cause analysis",
        "Six Sigma",
      ]),
    ],
    Experience:
    [
      new WorkExperience(
        Title: "Senior Quality & Reliability Engineer",
        Company: "Onspring",
        DateRange: "Aug 2020 – Present",
        Location: "Olathe, KS",
        Subtitle: "Enterprise GRC SaaS platform (ASP.NET backend, Vue front end)",
        Bullets:
        [
          RichText.FromPlain(
            "Built the end-to-end UI test framework from scratch in TypeScript and Playwright, using the " +
            "Screenplay pattern. It now runs 725+ tests across 87 spec files against dynamic, permission-driven workflows."
          ),
          RichText.FromPlain(
            "Built the backend's unit and integration test framework in C# with xUnit and Moq. Integration tests " +
            "verify relational database state and data integrity, and the framework sets the patterns developers follow."
          ),
          RichText.FromPlain(
            "Introduced Vitest unit and component tests for the Vue front end."
          ),
          RichText.FromPlain(
            "Wired the suites into GitHub Actions: acceptance runs against seven environments, lint and format " +
            "gates, and automated cleanup that keeps runs repeatable."
          ),
          RichText.FromPlain(
            "Built a Dockerized, authenticated portal that publishes every run's Playwright reports and traces, " +
            "so engineers can diagnose flaky tests, race conditions, and environment drift."
          ),
          RichText.FromPlain(
            "Write regression test plans and testing standards. Work with product and development during design " +
            "so features arrive testable."
          ),
          RichText.FromPlain(
            "Validate new features against functional, performance, and reliability requirements."
          ),
          RichText.FromPlain(
            "Built a cross-platform .NET CLI for the Onspring API and contributed to its SDKs in .NET, Python, " +
            "JavaScript, Go, and Rust."
          ),
          RichText.FromPlain(
            "Use AI tools daily to draft tests, analyze coverage, and debug failures. Check AI-generated tests " +
            "against the system's actual behavior and reject any that are non-deterministic or wrong before they reach CI."
          ),
        ]
      ),
      new WorkExperience(
        Title: "Food Scientist",
        Company: "Ajinomoto Foods North America, Inc.",
        DateRange: "Jun 2019 – Aug 2020",
        Location: "Joplin & Carthage, MO",
        Subtitle: "Returned after a brief construction project management role",
        Bullets:
        [
          RichText.FromPlain("Resumed product testing, regulatory compliance, and process development work described below."),
        ]
      ),
      new WorkExperience(
        Title: "Project Manager",
        Company: "Mid-Land Enterprises",
        DateRange: "Feb 2019 – May 2019",
        Location: "Joplin, MO",
        Subtitle: null,
        Bullets:
        [
          RichText.FromPlain(
            "Estimated, planned, and managed industrial and commercial construction projects from bid to closeout, " +
            "including budgets and billing."
          ),
        ]
      ),
      new WorkExperience(
        Title: "Food Scientist",
        Company: "Ajinomoto Foods North America, Inc.",
        DateRange: "Jan 2016 – Feb 2019",
        Location: "Joplin & Carthage, MO",
        Subtitle: null,
        Bullets:
        [
          RichText.FromPlain(
            "Planned and ran production trials for new and revised products, and reported results that guided " +
            "launch decisions."
          ),
          RichText.FromPlain(
            "Wrote product controls that kept products within customer specifications and USDA and FDA regulations."
          ),
          RichText.FromPlain(
            "Kept formula and process-control records, including ingredient and nutrition data, accurate and traceable."
          ),
          RichText.FromPlain(
            "Designed manufacturing processes with engineering and production teams, and trained operations and QA staff."
          ),
        ]
      ),
    ],
    Projects:
    [
      new ProjectItem(
        Name: "onx",
        Qualifier: "Rust",
        Url: new Uri("https://github.com/StevanFreeborn/onx"),
        Description: RichText.FromPlain(
          "Agent-friendly CLI for the Onspring API. Emits structured JSON for machines, includes 206 unit tests, " +
          "and ships binaries for six OS and architecture targets through GitHub Actions."
        )
      ),
      new ProjectItem(
        Name: "onx-skill",
        Qualifier: null,
        Url: new Uri("https://github.com/StevanFreeborn/onx-skill"),
        Description: RichText.FromPlain(
          "Agent Skill, built to the open Agent Skills specification, that teaches AI agents to operate Onspring " +
          "through onx. CI validates it against the spec; a test checks its CLI reference against onx's help output " +
          "to catch documentation drift."
        )
      ),
      new ProjectItem(
        Name: "Aspire End-to-End Testing",
        Qualifier: "C#",
        Url: new Uri("https://github.com/StevanFreeborn/aspire-end-to-end-testing"),
        Description: RichText.FromPlain(
          "Uses .NET Aspire, Playwright, and xUnit to start and test a full stack (React, Go, PostgreSQL) from a " +
          "single test run."
        )
      ),
      new ProjectItem(
        Name: "Onspring MCP Server",
        Qualifier: null,
        Url: new Uri("https://github.com/StevanFreeborn/onx-mcp-server"),
        Description: RichText.FromPlain(
          "Model Context Protocol server that gives AI assistants access to Onspring apps, records, reports, and files."
        )
      ),
      new ProjectItem(
        Name: "AsyncSyncFlagAnalyzer",
        Qualifier: "C#, NuGet",
        Url: new Uri("https://www.nuget.org/packages/StevanFreeborn.AsyncSyncFlagAnalyzer"),
        Description: RichText.FromPlain(
          "Roslyn analyzer and code fix that enforces a safe sync/async pattern in legacy codebases."
        )
      ),
    ],
    Education:
    [
      new EducationItem("B.S., Biochemistry", "Missouri Southern State University"),
      new EducationItem("Software Engineering", "Boot.dev (2026)  •  freeCodeCamp (2021–2022)"),
      new EducationItem("Food Science", "Rutgers University (2016)"),
    ],
    Certifications:
    [
      "Six Sigma Green Belt",
      "Foundational C# with Microsoft",
      "Quality Assurance",
      "Relational Database",
    ],
    Theme: new ResumeTheme()
  );
}
