using MyPad.Core.Models;
using MyPad.Infrastructure.Themes;

namespace MyPad.Infrastructure.Tests;

[TestClass]
public class ThemeServiceTests
{
    private string _testThemesDir = null!;
    private ThemeService _themeService = null!;

    [TestInitialize]
    public void Setup()
    {
        _testThemesDir = Path.Combine(Path.GetTempPath(), "MyPad_Test_Themes_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testThemesDir);
        _themeService = new ThemeService(_testThemesDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testThemesDir))
        {
            try { Directory.Delete(_testThemesDir, true); } catch { }
        }
    }

    [TestMethod]
    public void BuiltInThemes_IncludesRequiredCatalog()
    {
        var themes = _themeService.GetAvailableEditorThemes();
        Assert.IsTrue(themes.Count >= 12);

        var names = themes.Select(t => t.Name).ToList();
        CollectionAssert.Contains(names, "Default Dark");
        CollectionAssert.Contains(names, "Dracula");
        CollectionAssert.Contains(names, "Nord");
        CollectionAssert.Contains(names, "One Dark");
        CollectionAssert.Contains(names, "Monokai");
        CollectionAssert.Contains(names, "Catppuccin Mocha");
        CollectionAssert.Contains(names, "Default Light");
        CollectionAssert.Contains(names, "GitHub Light");
        CollectionAssert.Contains(names, "Solarized Light");
    }

    [TestMethod]
    public void ValidateTheme_ValidJson_ReturnsTrue()
    {
        string validJson = """
        {
            "schemaVersion": 1,
            "name": "Custom Sunset",
            "appearance": "dark",
            "colors": {
                "background": "#1a1a2e",
                "foreground": "#e94560",
                "selection": "#16213e",
                "cursor": "#0f3460",
                "currentLine": "#16213e",
                "lineNumber": "#53354a",
                "keyword": "#e94560",
                "string": "#f8b500",
                "comment": "#903749",
                "heading": "#e94560",
                "link": "#4ecca3"
            }
        }
        """;

        bool isValid = _themeService.ValidateTheme(validJson, out string? error);
        Assert.IsTrue(isValid, error);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void ValidateTheme_InvalidHex_ReturnsFalse()
    {
        string invalidHexJson = """
        {
            "schemaVersion": 1,
            "name": "Broken Hex",
            "appearance": "dark",
            "colors": {
                "background": "NOT_A_HEX",
                "foreground": "#ffffff",
                "selection": "#16213e",
                "cursor": "#0f3460",
                "currentLine": "#16213e",
                "lineNumber": "#53354a",
                "keyword": "#e94560",
                "string": "#f8b500",
                "comment": "#903749",
                "heading": "#e94560",
                "link": "#4ecca3"
            }
        }
        """;

        bool isValid = _themeService.ValidateTheme(invalidHexJson, out string? error);
        Assert.IsFalse(isValid);
        Assert.IsNotNull(error);
        StringAssert.Contains(error, "invalid hex format");
    }

    [TestMethod]
    public void ValidateTheme_UnsupportedVersion_ReturnsFalse()
    {
        string invalidVersionJson = """
        {
            "schemaVersion": 99,
            "name": "Future Theme",
            "appearance": "dark",
            "colors": {}
        }
        """;

        bool isValid = _themeService.ValidateTheme(invalidVersionJson, out string? error);
        Assert.IsFalse(isValid);
        StringAssert.Contains(error, "Unsupported schema version");
    }

    [TestMethod]
    public void ImportTheme_SavesAndSelectsTheme()
    {
        string validJson = """
        {
            "schemaVersion": 1,
            "name": "My Custom Palette",
            "appearance": "dark",
            "colors": {
                "background": "#111111",
                "foreground": "#eeeeee",
                "selection": "#333333",
                "cursor": "#ffffff",
                "currentLine": "#222222",
                "lineNumber": "#555555",
                "keyword": "#ff007f",
                "string": "#00ff7f",
                "comment": "#777777",
                "heading": "#7f00ff",
                "link": "#007fff"
            }
        }
        """;

        var imported = _themeService.ImportTheme(validJson);
        Assert.AreEqual("My Custom Palette", imported.Name);
        Assert.AreEqual("My Custom Palette", _themeService.CurrentEditorTheme.Name);

        var themes = _themeService.GetAvailableEditorThemes();
        Assert.IsTrue(themes.Any(t => t.Name == "My Custom Palette"));
    }

    [TestMethod]
    public void CalculateContrastRatio_BlackAndWhite_ReturnsApproximately21()
    {
        double ratio = ThemeService.CalculateContrastRatio("#000000", "#ffffff");
        Assert.IsTrue(ratio > 20.0);
    }
}
