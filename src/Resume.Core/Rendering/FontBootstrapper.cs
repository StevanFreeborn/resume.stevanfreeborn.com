using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace Resume.Core.Rendering;

internal static class FontBootstrapper
{
  private static readonly Lock s_lock = new();
  private static bool s_initialized;

  public static void EnsureInitialized()
  {
    if (s_initialized)
    {
      return;
    }

    lock (s_lock)
    {
      if (s_initialized)
      {
        return;
      }

      QuestPDF.Settings.License = LicenseType.Community;

      var assembly = typeof(FontBootstrapper).Assembly;
      var fontResources = assembly.GetManifestResourceNames()
        .Where(static n => n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
        .OrderBy(static n => n, StringComparer.Ordinal);

      var loaded = 0;

      foreach (var resourceName in fontResources)
      {
        using var stream = assembly.GetManifestResourceStream(resourceName)
          ?? throw new InvalidOperationException($"Could not load embedded font resource '{resourceName}'.");
        FontManager.RegisterFontFromStream(stream);
        loaded++;
      }

      if (loaded is 0)
      {
        throw new InvalidOperationException("No embedded TrueType fonts were found in Resume.Core.");
      }

      s_initialized = true;
    }
  }
}
