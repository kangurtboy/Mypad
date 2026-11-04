using System.Text.Json;
using System.Text.RegularExpressions;
using MyPad.Core.Interfaces;
using MyPad.Core.Models;

namespace MyPad.Infrastructure.Themes;

public class ThemeService : IThemeService
{
    private static readonly Regex HexColorRegex = new(
        @"^#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
        RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _customThemesDirectory;
    private readonly List<EditorTheme> _themes = new();
    private EditorTheme _currentTheme;

    public event EventHandler<EditorTheme>? ThemeChanged;

    public ThemeService(string? customThemesDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(customThemesDirectory))
        {
            _customThemesDirectory = customThemesDirectory;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _customThemesDirectory = Path.Combine(appData, "MyPad", "themes");
        }

        LoadBuiltInThemes();
        LoadCustomThemes();

        _currentTheme = _themes.First(t => t.Name == "Default Dark");
    }

    public IReadOnlyList<EditorTheme> GetAvailableEditorThemes() => _themes.AsReadOnly();

    public EditorTheme CurrentEditorTheme => _currentTheme;

    public void SetEditorTheme(string themeName)
    {
        var found = _themes.FirstOrDefault(t =>
            string.Equals(t.Name, themeName, StringComparison.OrdinalIgnoreCase));

        if (found is not null && found != _currentTheme)
        {
            _currentTheme = found;
            ThemeChanged?.Invoke(this, _currentTheme);
        }
    }

    public bool ValidateTheme(string jsonContent, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            errorMessage = "Theme content cannot be empty.";
            return false;
        }

        if (jsonContent.Length > 256 * 1024)
        {
            errorMessage = "Theme file size exceeds 256 KB limit.";
            return false;
        }

        try
        {
            var theme = JsonSerializer.Deserialize<EditorTheme>(jsonContent, JsonOptions);
            if (theme is null)
            {
                errorMessage = "Could not parse JSON as an EditorTheme.";
                return false;
            }

            if (theme.SchemaVersion != 1)
            {
                errorMessage = $"Unsupported schema version: {theme.SchemaVersion}. Expected version: 1.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(theme.Name))
            {
                errorMessage = "Theme name is required.";
                return false;
            }

            if (theme.Appearance != "light" && theme.Appearance != "dark")
            {
                errorMessage = "Appearance must be 'light' or 'dark'.";
                return false;
            }

            if (theme.Colors is null)
            {
                errorMessage = "Colors section is required.";
                return false;
            }

            var colors = new[]
            {
                ("background", theme.Colors.Background),
                ("foreground", theme.Colors.Foreground),
                ("selection", theme.Colors.Selection),
                ("cursor", theme.Colors.Cursor),
                ("currentLine", theme.Colors.CurrentLine),
                ("lineNumber", theme.Colors.LineNumber),
                ("keyword", theme.Colors.Keyword),
                ("string", theme.Colors.String),
                ("comment", theme.Colors.Comment),
                ("heading", theme.Colors.Heading),
                ("link", theme.Colors.Link)
            };

            foreach (var (name, val) in colors)
            {
                if (string.IsNullOrWhiteSpace(val) || !HexColorRegex.IsMatch(val))
                {
                    errorMessage = $"Color '{name}' has invalid hex format: '{val}'. Expected #RRGGBB or #AARRGGBB.";
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"JSON parsing error: {ex.Message}";
            return false;
        }
    }

    public EditorTheme ImportTheme(string jsonContent)
    {
        if (!ValidateTheme(jsonContent, out var error))
        {
            throw new ArgumentException($"Invalid theme JSON: {error}");
        }

        var theme = JsonSerializer.Deserialize<EditorTheme>(jsonContent, JsonOptions)!;
        theme.IsBuiltIn = false;

        // Save to custom themes directory
        if (!Directory.Exists(_customThemesDirectory))
        {
            Directory.CreateDirectory(_customThemesDirectory);
        }

        string safeFileName = Regex.Replace(theme.Name, @"[^a-zA-Z0-9_\-]", "_") + ".json";
        string filePath = Path.Combine(_customThemesDirectory, safeFileName);
        File.WriteAllText(filePath, jsonContent);

        // Replace or add in list
        int existingIdx = _themes.FindIndex(t => string.Equals(t.Name, theme.Name, StringComparison.OrdinalIgnoreCase));
        if (existingIdx >= 0)
        {
            _themes[existingIdx] = theme;
        }
        else
        {
            _themes.Add(theme);
        }

        SetEditorTheme(theme.Name);
        return theme;
    }

    public string ExportTheme(EditorTheme theme)
    {
        if (theme is null) throw new ArgumentNullException(nameof(theme));
        return JsonSerializer.Serialize(theme, JsonOptions);
    }

    public static double CalculateContrastRatio(string hexColor1, string hexColor2)
    {
        double l1 = GetRelativeLuminance(hexColor1);
        double l2 = GetRelativeLuminance(hexColor2);

        double brighter = Math.Max(l1, l2);
        double darker = Math.Min(l1, l2);

        return (brighter + 0.05) / (darker + 0.05);
    }

    private static double GetRelativeLuminance(string hex)
    {
        if (hex.StartsWith("#")) hex = hex[1..];
        if (hex.Length == 8) hex = hex[2..]; // skip alpha

        int r = Convert.ToInt32(hex[..2], 16);
        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = Convert.ToInt32(hex.Substring(4, 2), 16);

        double rs = r / 255.0;
        double gs = g / 255.0;
        double bs = b / 255.0;

        double rL = rs <= 0.03928 ? rs / 12.92 : Math.Pow((rs + 0.055) / 1.055, 2.4);
        double gL = gs <= 0.03928 ? gs / 12.92 : Math.Pow((gs + 0.055) / 1.055, 2.4);
        double bL = bs <= 0.03928 ? bs / 12.92 : Math.Pow((bs + 0.055) / 1.055, 2.4);

        return 0.2126 * rL + 0.7152 * gL + 0.0722 * bL;
    }

    private void LoadBuiltInThemes()
    {
        // Dark Themes
        _themes.Add(new EditorTheme
        {
            Name = "Default Dark",
            Appearance = "dark",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#1e1e1e",
                Foreground = "#d4d4d4",
                Selection = "#264f78",
                Cursor = "#aeafad",
                CurrentLine = "#282828",
                LineNumber = "#858585",
                Keyword = "#569cd6",
                String = "#ce9178",
                Comment = "#6a9955",
                Heading = "#4ec9b0",
                Link = "#3794ff"
            }
        });

        _themes.Add(new EditorTheme
        {
            Name = "One Dark",
            Appearance = "dark",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#282c34",
                Foreground = "#abb2bf",
                Selection = "#3e4451",
                Cursor = "#528bff",
                CurrentLine = "#2c313a",
                LineNumber = "#4b5263",
                Keyword = "#c678dd",
                String = "#98c379",
                Comment = "#5c6370",
                Heading = "#e06c75",
                Link = "#61afef"
            }
        });

        _themes.Add(new EditorTheme
        {
            Name = "Dracula",
            Appearance = "dark",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#282a36",
                Foreground = "#f8f8f2",
                Selection = "#44475a",
                Cursor = "#f8f8f0",
                CurrentLine = "#343746",
                LineNumber = "#6272a4",
                Keyword = "#ff79c6",
                String = "#f1fa8c",
                Comment = "#6272a4",
                Heading = "#bd93f9",
                Link = "#8be9fd"
            }
        });

        _themes.Add(new EditorTheme
        {
            Name = "Nord",
            Appearance = "dark",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#2e3440",
                Foreground = "#d8dee9",
                Selection = "#434c5e",
                Cursor = "#d8dee9",
                CurrentLine = "#3b4252",
                LineNumber = "#4c566a",
                Keyword = "#81a1c1",
                String = "#a3be8c",
                Comment = "#616e88",
                Heading = "#88c0d0",
                Link = "#8fbcbb"
            }
        });

        _themes.Add(new EditorTheme
        {
            Name = "Catppuccin Mocha",
            Appearance = "dark",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#1e1e2e",
                Foreground = "#cdd6f4",
                Selection = "#45475a",
                Cursor = "#f5e0dc",
                CurrentLine = "#313244",
                LineNumber = "#6c7086",
                Keyword = "#cba6f7",
                String = "#a6e3a1",
                Comment = "#585b70",
                Heading = "#89b4fa",
                Link = "#74c7ec"
            }
        });

        _themes.Add(new EditorTheme
        {
            Name = "Monokai",
            Appearance = "dark",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#272822",
                Foreground = "#f8f8f2",
                Selection = "#49483e",
                Cursor = "#f8f8f0",
                CurrentLine = "#3e3d32",
                LineNumber = "#75715e",
                Keyword = "#f92672",
                String = "#e6db74",
                Comment = "#75715e",
                Heading = "#a6e22e",
                Link = "#66d9ef"
            }
        });

        _themes.Add(new EditorTheme
        {
            Name = "Gruvbox Dark",
            Appearance = "dark",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#282828",
                Foreground = "#ebdbb2",
                Selection = "#504945",
                Cursor = "#ebdbb2",
                CurrentLine = "#3c3836",
                LineNumber = "#7c6f64",
                Keyword = "#fb4934",
                String = "#b8bb26",
                Comment = "#928374",
                Heading = "#fabd2f",
                Link = "#83a598"
            }
        });

        // Light Themes
        _themes.Add(new EditorTheme
        {
            Name = "Default Light",
            Appearance = "light",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#ffffff",
                Foreground = "#1e1e1e",
                Selection = "#add6ff",
                Cursor = "#000000",
                CurrentLine = "#f3f3f3",
                LineNumber = "#888888",
                Keyword = "#0000ff",
                String = "#a31515",
                Comment = "#008000",
                Heading = "#005a9e",
                Link = "#0066cc"
            }
        });

        _themes.Add(new EditorTheme
        {
            Name = "GitHub Light",
            Appearance = "light",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#ffffff",
                Foreground = "#24292e",
                Selection = "#b3d7ff",
                Cursor = "#24292e",
                CurrentLine = "#f6f8fa",
                LineNumber = "#959da5",
                Keyword = "#d73a49",
                String = "#032f62",
                Comment = "#6a737d",
                Heading = "#005cc5",
                Link = "#0366d6"
            }
        });

        _themes.Add(new EditorTheme
        {
            Name = "Solarized Light",
            Appearance = "light",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#fdf6e3",
                Foreground = "#657b83",
                Selection = "#eee8d5",
                Cursor = "#586e75",
                CurrentLine = "#eee8d5",
                LineNumber = "#93a1a1",
                Keyword = "#859900",
                String = "#2aa198",
                Comment = "#93a1a1",
                Heading = "#cb4b16",
                Link = "#268bd2"
            }
        });

        _themes.Add(new EditorTheme
        {
            Name = "Catppuccin Latte",
            Appearance = "light",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#eff1f5",
                Foreground = "#4c4f69",
                Selection = "#acb0be",
                Cursor = "#dc8a78",
                CurrentLine = "#e6e9ef",
                LineNumber = "#9ca0b0",
                Keyword = "#8839ef",
                String = "#40a02b",
                Comment = "#9ca0b0",
                Heading = "#1e66f5",
                Link = "#04a5e5"
            }
        });

        _themes.Add(new EditorTheme
        {
            Name = "Gruvbox Light",
            Appearance = "light",
            IsBuiltIn = true,
            Colors = new EditorThemeColors
            {
                Background = "#fbf1c7",
                Foreground = "#3c3836",
                Selection = "#d5c4a1",
                Cursor = "#3c3836",
                CurrentLine = "#ebdbb2",
                LineNumber = "#928374",
                Keyword = "#9d0006",
                String = "#79740e",
                Comment = "#928374",
                Heading = "#b57614",
                Link = "#076678"
            }
        });
    }

    private void LoadCustomThemes()
    {
        if (!Directory.Exists(_customThemesDirectory)) return;

        foreach (var file in Directory.GetFiles(_customThemesDirectory, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(file);
                if (ValidateTheme(json, out _))
                {
                    var theme = JsonSerializer.Deserialize<EditorTheme>(json, JsonOptions);
                    if (theme != null)
                    {
                        theme.IsBuiltIn = false;
                        _themes.Add(theme);
                    }
                }
            }
            catch
            {
                // Skip invalid custom theme file
            }
        }
    }
}
