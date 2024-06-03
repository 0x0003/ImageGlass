﻿/*
ImageGlass Project - Image viewer for Windows
Copyright (C) 2010 - 2024 DUONG DIEU PHAP
Project homepage: https://imageglass.org

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/
using ImageGlass.Base;
using ImageGlass.Base.Photoing.Codecs;
using ImageGlass.Base.WinApi;
using WicNet;

namespace ImageGlass.UI;

public class IgTheme : IDisposable
{
    #region IDisposable Disposing

    private bool _isDisposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            // Free any other managed objects here.
            Settings.Dispose();
            ToolbarIcons.Dispose();

            _defaultIcon?.Dispose();
            _defaultIcon = null;
        }

        // Free any unmanaged objects here.
        _isDisposed = true;
    }

    public virtual void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~IgTheme()
    {
        Dispose(false);
    }

    #endregion


    private int _iconHeight = Const.TOOLBAR_ICON_HEIGHT;
    private Bitmap? _defaultIcon = null;


    /// <summary>
    /// Filename of theme configuration since v9.0.
    /// </summary>
    public static string CONFIG_FILE { get; } = "igtheme.json";


    /// <summary>
    /// Theme API version, to check compatibility.
    /// </summary>
    public string CONFIG_VERSION { get; } = "9";

    /// <summary>
    /// Gets the name of theme folder.
    /// </summary>
    public string FolderName { get; internal set; } = "";

    /// <summary>
    /// Gets the name of theme folder.
    /// </summary>
    public string FolderPath { get; internal set; } = "";

    /// <summary>
    /// Gets theme config file path (<see cref="CONFIG_FILE"/>).
    /// </summary>
    public string ConfigFilePath { get; internal set; } = "";

    /// <summary>
    /// Gets JSON model from config file (<see cref="CONFIG_FILE"/>).
    /// </summary>
    public IgThemeJsonModel? JsonModel { get; internal set; }

    /// <summary>
    /// Checks if this theme is valid.
    /// </summary>
    public bool IsValid { get; internal set; } = false;


    /// <summary>
    /// Theme information.
    /// </summary>
    public IgThemeInfo Info { get; internal set; } = new();

    /// <summary>
    /// Theme settings.
    /// </summary>
    public IgThemeSettings Settings { get; internal set; } = new();

    /// <summary>
    /// Theme colors
    /// </summary>
    public IgThemeColors Colors { get; internal set; } = new();

    /// <summary>
    /// Theme toolbar icons.
    /// </summary>
    public IgThemeToolbarIcons ToolbarIcons { get; internal set; } = new();

    /// <summary>
    /// Gets color palatte according to the <see cref="Settings"/><c>.IsDarkMode</c> value.
    /// </summary>
    public IColors ColorPalatte => BHelper.GetThemeColorPalatte(Settings.IsDarkMode);

    /// <summary>
    /// Gets the full path of left navigation button image.
    /// </summary>
    public string NavLeftImagePath
    {
        get
        {
            if (JsonModel == null) return string.Empty;

            if (JsonModel.Settings.TryGetValue(nameof(IgThemeSettings.NavButtonLeft), out var imgObj))
            {
                var imgName = imgObj.ToString();
                if (!string.IsNullOrWhiteSpace(imgName))
                {
                    return Path.Combine(FolderPath, imgName);
                }
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the full path of right navigation button image.
    /// </summary>
    public string NavRightImagePath
    {
        get
        {
            if (JsonModel == null) return string.Empty;

            if (JsonModel.Settings.TryGetValue(nameof(IgThemeSettings.NavButtonRight), out var imgObj))
            {
                var imgName = imgObj.ToString();
                if (!string.IsNullOrWhiteSpace(imgName))
                {
                    return Path.Combine(FolderPath, imgName);
                }
            }

            return string.Empty;
        }
    }


    /// <summary>
    /// Initializes theme pack and reads the theme config file.
    /// </summary>
    public IgTheme(
        string themeFolderPath = "",
        int? iconHeight = null)
    {
        _iconHeight = iconHeight ?? _iconHeight;

        // read theme config
        _ = ReadThemeConfig(themeFolderPath);
    }


    /// <summary>
    /// Loads theme config file into <see cref="JsonModel"/>, and validates it.
    /// </summary>
    /// <param name="themeFolderPath"></param>
    public bool ReadThemeConfig(string themeFolderPath)
    {
        // get full path of config file
        ConfigFilePath = Path.Combine(themeFolderPath, CONFIG_FILE);

        if (!File.Exists(ConfigFilePath))
        {
            IsValid = false;
            return IsValid;
        }

        // get theme folder name
        FolderName = Path.GetFileName(themeFolderPath);

        // get full path of theme folder
        FolderPath = Path.GetDirectoryName(ConfigFilePath) ?? string.Empty;

        try
        {
            //parse theme config file
            JsonModel = BHelper.ReadJson<IgThemeJsonModel>(ConfigFilePath);

            // import theme info
            Info = JsonModel.Info;
        }
        catch { }

        if (JsonModel == null)
        {
            IsValid = false;
            return IsValid;
        }

        // check required fields
        if (string.IsNullOrEmpty(JsonModel.Info.Name)
            || string.IsNullOrEmpty(JsonModel._Metadata.Version))
        {
            IsValid = false;
            return IsValid;
        }

        IsValid = true;
        return IsValid;
    }


    /// <summary>
    /// Loads theme <see cref="Settings"/> from <see cref="JsonModel"/>.
    /// </summary>
    public async Task LoadThemeSettingsAsync()
    {
        if (IsValid is false || JsonModel is null) return;

        // dispose the current values
        Settings.Dispose();


        await Parallel.ForEachAsync(JsonModel.Settings, async (item, _) =>
        {
            var value = (item.Value ?? "")?.ToString()?.Trim();
            if (string.IsNullOrEmpty(value)) return;

            var prop = Settings.GetType().GetProperty(item.Key);

            try
            {
                // property is WicBitmapSource
                if (prop?.PropertyType == typeof(WicBitmapSource))
                {
                    var data = await PhotoCodec.LoadAsync(Path.Combine(FolderPath, value), new()
                    {
                        Width = _iconHeight * 2,
                        Height = _iconHeight * 2,
                    });

                    prop.SetValue(Settings, data.Image);
                    return;
                }

                // property is Bitmap
                if (prop?.PropertyType == typeof(Bitmap))
                {
                    var bmp = await PhotoCodec.GetThumbnailAsync(Path.Combine(FolderPath, value), 256, 256);

                    prop.SetValue(Settings, bmp);
                    return;
                }

                // property is String
                if (prop?.PropertyType == typeof(String))
                {
                    prop.SetValue(Settings, value ?? string.Empty);
                    return;
                }


                // property is other types
                var typedValue = Convert.ChangeType(value, prop?.PropertyType ?? typeof(string));
                prop?.SetValue(Settings, typedValue);
            }
            catch { }
        });


        if (Settings.AppLogo is null)
        {
            Settings.AppLogo = Properties.Resources.DefaultLogo;
        }
    }


    /// <summary>
    /// Loads theme <see cref="Colors"/> from <see cref="JsonModel"/>.
    /// </summary>
    public void LoadThemeColors()
    {
        if (IsValid is false || JsonModel is null) return;

        // dispose the current values
        Colors = new IgThemeColors();
        var systemAccentColor = WinColorsApi.GetAccentColor(true);

        foreach (var item in JsonModel.Colors)
        {
            var value = (item.Value ?? "")?.ToString()?.Trim();
            if (string.IsNullOrEmpty(value))
                continue;

            var prop = Colors.GetType().GetProperty(item.Key);

            try
            {
                // property is Color
                if (prop?.PropertyType == typeof(Color))
                {
                    Color colorItem;
                    if (value.StartsWith(Const.THEME_SYSTEM_ACCENT_COLOR, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // example: accent:180
                        var valueArr = value.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        var accentAlpha = 255;

                        // adjust accent color alpha
                        if (valueArr.Length > 1)
                        {
                            _ = int.TryParse(valueArr[1], out accentAlpha);
                        }

                        colorItem = systemAccentColor.Blend(Color.White, 0.7f, accentAlpha);
                    }
                    else
                    {
                        colorItem = BHelper.ColorFromHex(value);
                    }

                    prop.SetValue(Colors, colorItem);
                    continue;
                }
            }
            catch { }
        }
    }


    /// <summary>
    /// Gets toolbar icon file path from icon property name.
    /// </summary>
    /// <param name="iconPropName">
    /// The name of icon property (without extension). E.g. <c>ActualSize</c>.
    /// </param>
    public string GetToolbarIconFilePath(string iconPropName)
    {
        if (JsonModel.ToolbarIcons.TryGetValue(iconPropName, out var iconFileName))
        {
            return Path.Combine(FolderPath, iconFileName);
        }

        return string.Empty;
    }


    /// <summary>
    /// Saves current settings as a new config file.
    /// </summary>
    /// <param name="filePath"></param>
    public async Task SaveConfigAsFileAsync(string filePath)
    {
        await BHelper.WriteJsonAsync(filePath, JsonModel);
    }


    /// <summary>
    /// Gets toolbar icon from theme property name or image file path.
    /// </summary>
    /// <param name="name">
    /// Theme pack's icon name or image file path.
    /// Example: <c>OpenFile</c>, or <c>.\Themes\Kobe\OpenFile.svg</c>
    /// </param>
    public async Task<Bitmap> GetToolbarIconAsync(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        // get icon from theme pack icon name
        var icon = ToolbarIcons.GetType().GetProperty(name ?? string.Empty)?.GetValue(ToolbarIcons);
        if (icon != null) return icon as Bitmap;


        // load icon from custom file
        try
        {
            var iconPath = GetToolbarIconFilePath(name);

            // load icon from full path
            icon = await PhotoCodec.GetThumbnailAsync(
                !string.IsNullOrEmpty(iconPath) ? iconPath : name,
                _iconHeight, _iconHeight);
        }
        catch
        {
            // set empty icon
            _defaultIcon ??= BHelper.CreateDefaultToolbarIcon(_iconHeight, Settings.IsDarkMode);
            icon = _defaultIcon;
        }

        return icon as Bitmap;
    }


    /// <summary>
    /// Gets toolbar icon from theme property name or image file path.
    /// </summary>
    /// <param name="name">
    /// Theme pack's icon name or image file path.
    /// Example: <c>OpenFile</c>, or <c>.\Themes\Kobe\OpenFile.svg</c>
    /// </param>
    public Bitmap GetToolbarIcon(string? name)
    {
        return BHelper.RunSync(() => GetToolbarIconAsync(name));
    }

}
