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
using ImageGlass.Base.PhotoBox;
using ImageGlass.Settings;
using System.Dynamic;

namespace ImageGlass;

public partial class FrmSettings : WebForm
{

    public FrmSettings()
    {
        InitializeComponent();
    }


    // Protected / override methods
    #region Protected / override methods

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (DesignMode) return;

        Web2.PageName = "settings";
        Text = Config.Language[$"{nameof(FrmMain)}.{nameof(FrmMain.MnuSettings)}"];
        CloseFormHotkey = Keys.Escape;

        // load window placement from settings
        WindowSettings.SetPlacementToWindow(this, WindowSettings.GetFrmSettingsPlacementFromConfig());
    }


    protected override void OnRequestUpdatingTheme(RequestUpdatingThemeEventArgs e)
    {
        base.OnRequestUpdatingTheme(e);

        // set app logo on titlebar
        _ = Config.UpdateFormIcon(this);
    }


    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        // save placement setting
        var wp = WindowSettings.GetPlacementFromWindow(this);
        WindowSettings.SetFrmSettingsPlacementConfig(wp);
    }


    protected override async Task OnWeb2ReadyAsync()
    {
        await base.OnWeb2ReadyAsync();

        var htmlFilePath = App.StartUpDir(Dir.WebUI, $"{nameof(FrmSettings)}.html");
        Web2.Source = new Uri(htmlFilePath);
    }


    protected override async Task OnWeb2NavigationCompleted()
    {
        await base.OnWeb2NavigationCompleted();

        // update the setting data
        await WebUI.UpdateSvgIconsAsync();
        WebUI.UpdateLangListJson();
        WebUI.UpdateThemeListJson();


        // prepare data for app settings UI
        var configJsonObj = Config.PrepareJsonSettingsObject(useAbsoluteFileUrl: true);
        var startupDir = App.StartUpDir();
        var configDir = App.ConfigDir(PathType.Dir);
        var userConfigFilePath = App.ConfigDir(PathType.File, Source.UserFilename);
        var defaultThemeDir = App.ConfigDir(PathType.Dir, Dir.Themes, Const.DEFAULT_THEME);
        var builtInToolbarBtns = Config.ConvertToolbarButtonsToExpandoObjList(Local.BuiltInToolbarItems);


        var pageSettingObj = new ExpandoObject();
        _ = pageSettingObj.TryAdd("startUpDir", startupDir);
        _ = pageSettingObj.TryAdd("configDir", configDir);
        _ = pageSettingObj.TryAdd("userConfigFilePath", userConfigFilePath);
        _ = pageSettingObj.TryAdd("FILE_MACRO", Const.FILE_MACRO);
        _ = pageSettingObj.TryAdd("enums", WebUI.Enums);
        _ = pageSettingObj.TryAdd("defaultThemeDir", defaultThemeDir);
        _ = pageSettingObj.TryAdd("defaultImageInfoTags", Config.DefaultImageInfoTags);
        _ = pageSettingObj.TryAdd("toolList", Config.Tools);
        _ = pageSettingObj.TryAdd("langList", WebUI.LangList);
        _ = pageSettingObj.TryAdd("themeList", WebUI.ThemeList);
        _ = pageSettingObj.TryAdd("icons", WebUI.SvgIcons);
        _ = pageSettingObj.TryAdd("builtInToolbarButtons", builtInToolbarBtns);
        _ = pageSettingObj.TryAdd("config", configJsonObj);
        var pageSettingStr = BHelper.ToJson(pageSettingObj);

        var script = @$"
            window._pageSettings = {pageSettingStr};

            window._page.loadSettings();
            window._page.loadLanguage();
            window._page.setActiveMenu('{Config.LastOpenedSetting}');
        ";

        await Web2.ExecuteScriptAsync(script);

    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "<Pending>")]
    protected override async Task OnWeb2MessageReceivedAsync(Web2MessageReceivedEventArgs e)
    {
        await base.OnWeb2MessageReceivedAsync(e);

        // General events
        #region General events
        if (e.Name.Equals("BtnOK", StringComparison.Ordinal))
        {
            ApplySettings(e.Data);
            Close();
        }
        else if (e.Name.Equals("BtnApply", StringComparison.Ordinal))
        {
            ApplySettings(e.Data);
        }
        else if (e.Name.Equals("BtnCancel", StringComparison.Ordinal))
        {
            Close();
        }
        else if (e.Name.Equals("LnkHelp", StringComparison.Ordinal))
        {
            _ = BHelper.OpenUrlAsync("https://imageglass.org/docs", $"from_setting_{Config.LastOpenedSetting}");
        }
        #endregion // General events


        // Sidebar
        #region Sidebar
        // sidebar tab changed
        else if (e.Name.Equals("Sidebar_Changed", StringComparison.Ordinal))
        {
            Config.LastOpenedSetting = e.Data;
        }
        #endregion // Sidebar


        // Tab General
        #region Tab General
        else if (e.Name.Equals("Lnk_StartupDir", StringComparison.Ordinal))
        {
            BHelper.OpenFilePath(e.Data);
        }
        else if (e.Name.Equals("Lnk_ConfigDir", StringComparison.Ordinal))
        {
            BHelper.OpenFilePath(e.Data);
        }
        else if (e.Name.Equals("Lnk_UserConfigFile", StringComparison.Ordinal))
        {
            _ = OpenUserConfigFileAsync(e.Data);
        }
        #endregion // Tab General


        // Tab Image
        #region Tab Image
        else if (e.Name.Equals("Btn_BrowseColorProfile", StringComparison.Ordinal))
        {
            var profileFilePath = SelectColorProfileFile();
            profileFilePath = profileFilePath.Replace("\\", "\\\\");

            if (!String.IsNullOrEmpty(profileFilePath))
            {
                Web2.PostWeb2Message(e.Name, $"\"{profileFilePath}\"");
            }
        }
        else if (e.Name.Equals("Lnk_CustomColorProfile", StringComparison.Ordinal))
        {
            BHelper.OpenFilePath(e.Data);
        }
        #endregion // Tab Image


        // Tab Toolbar
        #region Tab Toolbar
        else if (e.Name.Equals("Btn_ResetToolbarButtons", StringComparison.Ordinal))
        {
            var json = BHelper.ToJson(Local.DefaultToolbarItemIds);
            Web2.PostWeb2Message(e.Name, json);
        }
        else if (e.Name.Equals("Btn_AddCustomToolbarButton_ValidateJson_Create", StringComparison.Ordinal)
            || e.Name.Equals("Btn_AddCustomToolbarButton_ValidateJson_Edit", StringComparison.Ordinal))
        {
            var isCreate = e.Name.Equals("Btn_AddCustomToolbarButton_ValidateJson_Create", StringComparison.Ordinal);
            var isValid = true;

            try
            {
                // try parsing the json
                var btn = BHelper.ParseJson<ToolbarItemModel>(e.Data);

                if (btn.Type == ToolbarItemModelType.Button)
                {
                    var langPath = $"{nameof(FrmSettings)}.Toolbar._Errors";
                    if (string.IsNullOrWhiteSpace(btn.Id))
                    {
                        throw new ArgumentException(Config.Language[$"{langPath}._ButtonIdRequired"], nameof(btn.Id));
                    }

                    if (isCreate
                        && Config.ToolbarButtons.Any(i => i.Id.Equals(btn.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new ArgumentException(string.Format(Config.Language[$"{langPath}._ButtonIdDuplicated"], btn.Id), nameof(btn.Id));
                    }

                    if (string.IsNullOrEmpty(btn.OnClick.Executable))
                    {
                        throw new ArgumentException(Config.Language[$"{langPath}._ButtonExecutableRequired"], nameof(btn.OnClick.Executable));
                    }
                }
            }
            catch (Exception ex)
            {
                _ = Config.ShowError(this, title: Config.Language["_._Error"], heading: ex.Message);
                isValid = false;
            }

            Web2.PostWeb2Message(e.Name, BHelper.ToJson(isValid));
        }
        #endregion // Tab Toolbar


        // Tab File type associations
        #region Tab ile type associations
        else if (e.Name.Equals("Btn_OpenExtIconFolder", StringComparison.Ordinal))
        {
            var extIconDir = App.ConfigDir(PathType.Dir, Dir.ExtIcons);
            BHelper.OpenFolderPath(extIconDir);
        }
        else if (e.Name.Equals("Btn_MakeDefaultViewer", StringComparison.Ordinal))
        {
            FrmMain.IG_SetDefaultPhotoViewer();
        }
        else if (e.Name.Equals("Btn_RemoveDefaultViewer", StringComparison.Ordinal))
        {
            FrmMain.IG_RemoveDefaultPhotoViewer();
        }
        else if (e.Name.Equals("Lnk_OpenDefaultAppsSetting", StringComparison.Ordinal))
        {
            _ = BHelper.OpenUrlAsync("ms-settings:defaultapps?registeredAppUser=ImageGlass");
        }
        else if (e.Name.Equals("Btn_ResetFileFormats", StringComparison.Ordinal))
        {
            Web2.PostWeb2Message("Btn_ResetFileFormats", $"\"{Const.IMAGE_FORMATS}\"");
        }
        #endregion // Tab File type associations


        // Tab Language
        #region Tab Language
        else if (e.Name.Equals("Btn_RefreshLanguageList", StringComparison.Ordinal))
        {
            WebUI.UpdateLangListJson(true);
            var langListJson = BHelper.ToJson(WebUI.LangList);

            Web2.PostWeb2Message(e.Name, langListJson);
        }
        else if (e.Name.Equals("Lnk_InstallLanguage", StringComparison.Ordinal))
        {
            _ = InstallLanguagePackAsync();
        }
        else if (e.Name.Equals("Lnk_ExportLanguage", StringComparison.Ordinal))
        {
            _ = FrmSettings.ExportLanguagePackAsync(e.Data);
        }
        #endregion // Tab Language


        // Tab Appearance
        #region Tab Appearance
        else if (e.Name.Equals("Btn_BackgroundColor", StringComparison.Ordinal)
            || e.Name.Equals("Btn_SlideshowBackgroundColor", StringComparison.Ordinal))
        {
            var currentColor = BHelper.ColorFromHex(e.Data);
            var newColor = OpenColorPicker(currentColor);
            var hexColor = string.Empty;

            if (newColor != null)
            {
                hexColor = newColor.Value.ToHex();
            }

            Web2.PostWeb2Message(e.Name, $"\"{hexColor}\"");
        }
        else if (e.Name.Equals("Btn_InstallTheme", StringComparison.Ordinal))
        {
            _ = InstallThemeAsync();
        }
        else if (e.Name.Equals("Btn_RefreshThemeList", StringComparison.Ordinal))
        {
            WebUI.UpdateThemeListJson(true);
            var themeListJson = BHelper.ToJson(WebUI.ThemeList);

            Web2.PostWeb2Message("Btn_RefreshThemeList", themeListJson);
        }
        else if (e.Name.Equals("Btn_OpenThemeFolder", StringComparison.Ordinal))
        {
            var themeDir = App.ConfigDir(PathType.Dir, Dir.Themes);
            BHelper.OpenFolderPath(themeDir);
        }
        else if (e.Name.Equals("Delete_Theme_Pack", StringComparison.Ordinal))
        {
            _ = UninstallThemeAsync(e.Data);
        }
        #endregion // Tab Appearance


        // Global
        #region Global
        // open file picker
        else if (e.Name.Equals("OpenFilePicker", StringComparison.Ordinal))
        {
            var filePaths = OpenFilePickerJson(e.Data);
            Web2.PostWeb2Message("OpenFilePicker", filePaths);
        }

        // open hotkey picker
        else if (e.Name.Equals("OpenHotkeyPicker", StringComparison.Ordinal))
        {
            var hotkey = OpenHotkeyPickerJson();
            Web2.PostWeb2Message("OpenHotkeyPicker", $"\"{hotkey}\"");
        }
        #endregion // Global

    }


    #endregion // Protected / override methods


    // Private methods
    #region Private methods

    private void ApplySettings(string dataJson)
    {
        var dict = BHelper.ParseJson<ExpandoObject>(dataJson)
            .ToDictionary(i => i.Key, i => i.Value.ToString() ?? string.Empty);


        var requests = UpdateRequests.None;
        var reloadImg = false;
        var reloadImgList = false;
        var updateSlideshow = false;
        var updateToolbarAlignment = false;
        var updateToolbarIcons = false;
        var updateToolbarButtons = false;
        var updateGallery = false;
        var updateLanguage = false;
        var updateAppearance = false;
        var updateTheme = false;
        var updateLayout = false;


        // Tab General
        #region Tab General

        _ = Config.SetFromJson(dict, nameof(Config.ShowWelcomeImage));
        _ = Config.SetFromJson(dict, nameof(Config.ShouldOpenLastSeenImage));

        if (Config.SetFromJson(dict, nameof(Config.EnableRealTimeFileUpdate)).Done)
        {
            Local.FrmMain.IG_SetRealTimeFileUpdate(Config.EnableRealTimeFileUpdate);
        }
        _ = Config.SetFromJson(dict, nameof(Config.ShouldAutoOpenNewAddedImage));

        _ = Config.SetFromJson(dict, nameof(Config.AutoUpdate));
        _ = Config.SetFromJson(dict, nameof(Config.EnableMultiInstances));
        if (Config.SetFromJson(dict, nameof(Config.ShowAppIcon)).Done)
        {
            _ = Config.UpdateFormIcon(this);
            _ = Config.UpdateFormIcon(Local.FrmMain);
        }
        _ = Config.SetFromJson(dict, nameof(Config.InAppMessageDuration));

        if (Config.SetFromJson(dict, nameof(Config.ImageInfoTags)).Done)
        {
            Local.FrmMain.LoadImageInfo();
        }

        #endregion // Tab General


        // Tab Image
        #region Tab Image

        // Image loading
        if (Config.SetFromJson(dict, nameof(Config.ImageLoadingOrder)).Done) { reloadImgList = true; }
        if (Config.SetFromJson(dict, nameof(Config.ImageLoadingOrderType)).Done) { reloadImgList = true; }
        if (Config.SetFromJson(dict, nameof(Config.ShouldUseExplorerSortOrder)).Done) { reloadImgList = true; }
        if (Config.SetFromJson(dict, nameof(Config.EnableRecursiveLoading)).Done) { reloadImgList = true; }
        if (Config.SetFromJson(dict, nameof(Config.ShouldGroupImagesByDirectory)).Done) { reloadImgList = true; }
        if (Config.SetFromJson(dict, nameof(Config.ShouldLoadHiddenImages)).Done) { reloadImgList = true; }

        _ = Config.SetFromJson(dict, nameof(Config.EnableLoopBackNavigation));
        _ = Config.SetFromJson(dict, nameof(Config.ShowImagePreview));
        _ = Config.SetFromJson(dict, nameof(Config.EnableImageTransition));

        if (Config.SetFromJson(dict, nameof(Config.UseEmbeddedThumbnailRawFormats)).Done) { reloadImg = true; }
        if (Config.SetFromJson(dict, nameof(Config.UseEmbeddedThumbnailOtherFormats)).Done) { reloadImg = true; }
        if (Config.SetFromJson(dict, nameof(Config.EmbeddedThumbnailMinWidth)).Done) { reloadImg = true; }
        if (Config.SetFromJson(dict, nameof(Config.EmbeddedThumbnailMinHeight)).Done) { reloadImg = true; }


        // Image booster
        if (Config.SetFromJson(dict, nameof(Config.ImageBoosterCacheCount)).Done)
        {
            Local.Images.MaxQueue = Config.ImageBoosterCacheCount;
        }
        if (Config.SetFromJson(dict, nameof(Config.ImageBoosterCacheMaxDimension)).Done)
        {
            Local.Images.MaxImageDimensionToCache = Config.ImageBoosterCacheMaxDimension;
        }
        if (Config.SetFromJson(dict, nameof(Config.ImageBoosterCacheMaxFileSizeInMb)).Done)
        {
            Local.Images.MaxFileSizeInMbToCache = Config.ImageBoosterCacheMaxFileSizeInMb;
        }


        // Color manmagement
        if (Config.SetFromJson(dict, nameof(Config.ShouldUseColorProfileForAll)).Done) { reloadImg = true; }
        if (Config.SetFromJson(dict, nameof(Config.ColorProfile)).Done) { reloadImg = true; }

        #endregion // Tab Image


        // Tab Slideshow
        #region Tab Slideshow

        _ = Config.SetFromJson(dict, nameof(Config.HideMainWindowInSlideshow));
        if (Config.SetFromJson(dict, nameof(Config.ShowSlideshowCountdown)).Done) { updateSlideshow = true; }
        if (Config.SetFromJson(dict, nameof(Config.EnableLoopSlideshow)).Done) { updateSlideshow = true; }
        if (Config.SetFromJson(dict, nameof(Config.EnableFullscreenSlideshow)).Done) { updateSlideshow = true; }
        if (Config.SetFromJson(dict, nameof(Config.UseRandomIntervalForSlideshow)).Done) { updateSlideshow = true; }
        if (Config.SetFromJson(dict, nameof(Config.SlideshowInterval)).Done) { updateSlideshow = true; }
        if (Config.SetFromJson(dict, nameof(Config.SlideshowIntervalTo)).Done) { updateSlideshow = true; }
        if (Config.SetFromJson(dict, nameof(Config.SlideshowBackgroundColor)).Done) { updateSlideshow = true; }

        if (Config.SetFromJson(dict, nameof(Config.SlideshowImagesToNotifySound)).Done) { updateSlideshow = true; }

        #endregion // Tab Slideshow


        // Tab Edit
        #region Tab Edit

        _ = Config.SetFromJson(dict, nameof(Config.ShowDeleteConfirmation));
        _ = Config.SetFromJson(dict, nameof(Config.ShowSaveOverrideConfirmation));
        _ = Config.SetFromJson(dict, nameof(Config.ShouldPreserveModifiedDate));
        _ = Config.SetFromJson(dict, nameof(Config.ImageEditQuality));
        _ = Config.SetFromJson(dict, nameof(Config.AfterEditingAction));
        _ = Config.SetFromJson(dict, nameof(Config.EnableCopyMultipleFiles));
        _ = Config.SetFromJson(dict, nameof(Config.EnableCutMultipleFiles));
        _ = Config.SetFromJson(dict, nameof(Config.EditApps));

        #endregion // Tab Edit


        // Tab Viewer
        #region Tab Viewer

        if (Config.SetFromJson(dict, nameof(Config.CenterWindowFit)).Done)
        {
            Local.FrmMain.FitWindowToImage();
        }

        if (Config.SetFromJson(dict, nameof(Config.ShowCheckerboardOnlyImageRegion)).Done)
        {
            Local.FrmMain.IG_ToggleCheckerboard(Config.ShowCheckerboard);
        }
        if (Config.SetFromJson(dict, nameof(Config.EnableNavigationButtons)).Done)
        {
            Local.FrmMain.PicMain.NavDisplay = Config.EnableNavigationButtons
                ? NavButtonDisplay.Both
                : NavButtonDisplay.None;
        }
        _ = Config.SetFromJson(dict, nameof(Config.UseWebview2ForSvg));
        if (Config.SetFromJson(dict, nameof(Config.PanSpeed)).Done)
        {
            Local.FrmMain.PicMain.PanDistance = Config.PanSpeed;
        }
        if (Config.SetFromJson(dict, nameof(Config.ImageInterpolationScaleDown)).Done)
        {
            Local.FrmMain.PicMain.InterpolationScaleDown = Config.ImageInterpolationScaleDown;
        }
        if (Config.SetFromJson(dict, nameof(Config.ImageInterpolationScaleUp)).Done)
        {
            Local.FrmMain.PicMain.InterpolationScaleUp = Config.ImageInterpolationScaleUp;
        }
        if (Config.SetFromJson(dict, nameof(Config.ZoomSpeed)).Done)
        {
            Local.FrmMain.PicMain.ZoomSpeed = Config.ZoomSpeed;
        }
        if (Config.SetFromJson(dict, nameof(Config.ZoomLevels)).Done)
        {
            Local.FrmMain.PicMain.ZoomLevels = Config.ZoomLevels;
        }

        #endregion // Tab Viewer


        // Tab Toolbar
        #region Tab Toolbar

        _ = Config.SetFromJson(dict, nameof(Config.HideToolbarInFullscreen));
        if (Config.SetFromJson(dict, nameof(Config.EnableCenterToolbar)).Done) { updateToolbarAlignment = true; }
        if (Config.SetFromJson(dict, nameof(Config.ToolbarIconHeight)).Done) { updateToolbarIcons = true; }
        if (Config.SetFromJson(dict, nameof(Config.ToolbarButtons)).Done) { updateToolbarButtons = true; }

        #endregion // Tab Toolbar


        // Tab Gallery
        #region Tab Gallery

        _ = Config.SetFromJson(dict, nameof(Config.HideGalleryInFullscreen));
        if (Config.SetFromJson(dict, nameof(Config.ShowGalleryScrollbars)).Done) { updateGallery = true; }
        if (Config.SetFromJson(dict, nameof(Config.ShowGalleryFileName)).Done) { updateGallery = true; }
        if (Config.SetFromJson(dict, nameof(Config.ThumbnailSize)).Done) { updateGallery = true; }
        if (Config.SetFromJson(dict, nameof(Config.GalleryCacheSizeInMb)).Done) { updateGallery = true; }
        if (Config.SetFromJson(dict, nameof(Config.GalleryColumns)).Done) { updateGallery = true; }

        #endregion // Tab Gallery


        // Tab Layout
        #region Tab Layout
        if (Config.SetFromJson(dict, nameof(Config.Layout)).Done) { updateLayout = true; }

        #endregion // Tab Layout


        // Tab Mouse & Keyboard
        #region Tab Mouse & Keyboard

        _ = Config.SetFromJson(dict, nameof(Config.MouseWheelActions));

        #endregion // Tab Mouse & Keyboard


        // Tab File type associations
        #region Tab File type associations

        _ = Config.SetFromJson(dict, nameof(Config.FileFormats));

        #endregion // Tab File type associations


        // Tab Tools
        #region Tab Tools

        if (Config.SetFromJson(dict, nameof(Config.Tools)).Done)
        {
            // update hotkeys list
            FrmMain.CurrentMenuHotkeys = Config.GetAllHotkeys(FrmMain.CurrentMenuHotkeys);

            // update extenal tools menu
            Local.FrmMain.LoadExternalTools();
        }

        #endregion // Tab Tools


        // Tab Language
        #region Tab Language

        if (Config.SetFromJson(dict, nameof(Config.Language)).Done)
        {
            updateLanguage = true;
        }

        #endregion // Tab Language


        // Tab Appearance
        #region Tab Appearance

        if (Config.SetFromJson(dict, nameof(Config.WindowBackdrop)).Done) { updateAppearance = true; }
        if (Config.SetFromJson(dict, nameof(Config.BackgroundColor)).Done) { updateAppearance = true; }
        if (Config.SetFromJson(dict, nameof(Config.DarkTheme)).Done) { updateTheme = true; }
        if (Config.SetFromJson(dict, nameof(Config.LightTheme)).Done) { updateTheme = true; }

        #endregion // Tab Appearance


        if (reloadImg) requests |= UpdateRequests.ReloadImage;
        if (reloadImgList) requests |= UpdateRequests.ReloadImageList;
        if (updateSlideshow) requests |= UpdateRequests.Slideshow;
        if (updateToolbarAlignment) requests |= UpdateRequests.ToolbarAlignment;
        if (updateToolbarIcons) requests |= UpdateRequests.ToolbarIcons;
        if (updateToolbarButtons) requests |= UpdateRequests.ToolbarButtons;
        if (updateGallery) requests |= UpdateRequests.Gallery;
        if (updateLanguage) requests |= UpdateRequests.Language;
        if (updateAppearance) requests |= UpdateRequests.Appearance;
        if (updateTheme) requests |= UpdateRequests.Theme;
        if (updateLayout) requests |= UpdateRequests.Layout;

        Local.UpdateFrmMain(requests, (e) =>
        {
            if (e.HasFlag(UpdateRequests.Theme))
            {
                // load the new value of Background color setting when theme is changed
                var bgColorHex = Config.BackgroundColor.ToHex();
                _ = Web2.ExecuteScriptAsync($"""
                    _page.loadBackgroundColorConfig('{bgColorHex}');
                """);
            }
        });
    }


    private async Task InstallLanguagePackAsync()
    {
        using var o = new OpenFileDialog()
        {
            Filter = "ImageGlass language pack (*.iglang.json)|*.iglang.json",
            CheckFileExists = true,
            RestoreDirectory = true,
            Multiselect = true,
        };

        if (o.ShowDialog() != DialogResult.OK)
        {
            Web2.PostWeb2Message("Lnk_InstallLanguage", "null");
            return;
        }

        var filePathsArgs = string.Join(" ", o.FileNames.Select(f => $"\"{f}\""));
        var result = await Config.RunIgcmd(
            $"{IgCommands.INSTALL_LANGUAGES} {IgCommands.SHOW_UI} {filePathsArgs}",
            true);

        if (result == IgExitCode.Done)
        {
            WebUI.UpdateLangListJson(true);
            var langListJson = BHelper.ToJson(WebUI.LangList);

            Web2.PostWeb2Message("Lnk_InstallLanguage", langListJson);
        }
    }


    private static async Task ExportLanguagePackAsync(string langFileName)
    {
        using var o = new SaveFileDialog()
        {
            Filter = "ImageGlass language pack (*.iglang.json)|*.iglang.json",
            AddExtension = true,
            OverwritePrompt = true,
            RestoreDirectory = true,
            FileName = langFileName,
        };

        if (o.ShowDialog() != DialogResult.OK) return;

        var lang = new IgLang(langFileName, App.StartUpDir(Dir.Language));
        await lang.SaveAsFileAsync(o.FileName);
    }


    private static async Task OpenUserConfigFileAsync(string filePath)
    {
        var result = await BHelper.RunExeCmd($"\"{filePath}\"", "", false);

        if (result == IgExitCode.Error)
        {
            result = await BHelper.RunExeCmd("notepad", $"\"{filePath}\"", false);
        }
    }


    private static string SelectColorProfileFile()
    {
        using var o = new OpenFileDialog()
        {
            Filter = "Color profile|*.icc;*.icm;|All files|*.*",
            CheckFileExists = true,
            RestoreDirectory = true,
        };

        if (o.ShowDialog() != DialogResult.OK) return string.Empty;

        return o.FileName;
    }


    private async Task InstallThemeAsync()
    {
        using var o = new OpenFileDialog()
        {
            Filter = "ImageGlass theme pack (*.igtheme)|*.igtheme",
            CheckFileExists = true,
            RestoreDirectory = true,
            Multiselect = true,
        };

        if (o.ShowDialog() != DialogResult.OK)
        {
            Web2.PostWeb2Message("Btn_InstallTheme", "null");
            return;
        }

        var filePathsArgs = string.Join(" ", o.FileNames.Select(f => $"\"{f}\""));
        var result = await Config.RunIgcmd(
            $"{IgCommands.INSTALL_THEMES} {IgCommands.SHOW_UI} {filePathsArgs}",
            true);

        if (result == IgExitCode.Done)
        {
            WebUI.UpdateThemeListJson(true);
            var themeListJson = BHelper.ToJson(WebUI.ThemeList);

            Web2.PostWeb2Message("Btn_InstallTheme", themeListJson);
        }
    }


    private async Task UninstallThemeAsync(string themeDirPath)
    {
        var result = await Config.RunIgcmd(
            $"{IgCommands.UNINSTALL_THEME} {IgCommands.SHOW_UI} \"{themeDirPath}\"",
            true);

        if (result == IgExitCode.Done)
        {
            WebUI.UpdateThemeListJson(true);
            var themeListJson = BHelper.ToJson(WebUI.ThemeList);

            Web2.PostWeb2Message("Delete_Theme_Pack", themeListJson);
        }
    }


    private static Color? OpenColorPicker(Color? defaultColor = null)
    {
        using var cd = new ModernColorDialog()
        {
            StartPosition = FormStartPosition.CenterParent,
            ColorValue = defaultColor ?? Color.White,
        };

        if (cd.ShowDialog() == DialogResult.OK)
        {
            return cd.ColorValue;
        }

        return defaultColor;
    }


    /// <summary>
    /// Open file picker.
    /// </summary>
    /// <param name="jsonOptions">Options in JSON: <c>{ filter?: string, multiple?: boolean }</c></param>
    /// <returns>File paths array or null in JSON</returns>
    private static string OpenFilePickerJson(string jsonOptions)
    {
        var options = BHelper.ParseJson<ExpandoObject>(jsonOptions)
                .ToDictionary(i => i.Key, i => i.Value.ToString() ?? string.Empty);

        _ = options.TryGetValue("filter", out var filter);
        _ = options.TryGetValue("multiple", out var multiple);

        filter ??= "All files (*.*)|*.*";
        _ = bool.TryParse(multiple, out var multiSelect);


        using var o = new OpenFileDialog()
        {
            Filter = filter,
            CheckFileExists = true,
            RestoreDirectory = true,
            Multiselect = multiSelect,
        };

        if (o.ShowDialog() == DialogResult.OK)
        {
            return BHelper.ToJson(o.FileNames);
        }

        return "null";
    }


    /// <summary>
    /// Open hotkey picker, returns <c>string.Empty</c> if user cancels the dialog or does not press any key
    /// </summary>
    private string OpenHotkeyPickerJson()
    {
        using var frm = new FrmHotkeyPicker()
        {
            StartPosition = FormStartPosition.CenterParent,
            TopMost = TopMost,
        };

        if (frm.ShowDialog() == DialogResult.OK)
        {
            return frm.HotkeyValue.ToString();
        }

        return string.Empty;
    }

    #endregion // Private methods

}
