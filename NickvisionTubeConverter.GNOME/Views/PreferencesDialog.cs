using NickvisionTubeConverter.GNOME.Helpers;
using NickvisionTubeConverter.Shared.Controllers;
using NickvisionTubeConverter.Shared.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using static Nickvision.GirExt.GtkExt;
using static NickvisionTubeConverter.Shared.Helpers.Gettext;

namespace NickvisionTubeConverter.GNOME.Views;

/// <summary>
/// The PreferencesDialog for the application
/// </summary>
public partial class PreferencesDialog : Adw.PreferencesWindow
{
    private readonly PreferencesViewController _controller;
    private readonly Adw.Application _application;

    [Gtk.Connect] private readonly Adw.ComboRow _themeRow;
    [Gtk.Connect] private readonly Adw.ComboRow _completedNotificationRow;
    [Gtk.Connect] private readonly Adw.ActionRow _backgroundRow;
    [Gtk.Connect] private readonly Gtk.Switch _backgroundSwitch;
    [Gtk.Connect] private readonly Gtk.SpinButton _maxNumberOfActiveDownloadsSpin;
    [Gtk.Connect] private readonly Gtk.Switch _overwriteSwitch;
    [Gtk.Connect] private readonly Gtk.Switch _limitCharactersSwitch;
    [Gtk.Connect] private readonly Gtk.SpinButton _speedLimitSpin;
    [Gtk.Connect] private readonly Adw.ExpanderRow _useAriaRow;
    [Gtk.Connect] private readonly Gtk.SpinButton _ariaMaxConnectionsPerServerSpin;
    [Gtk.Connect] private readonly Gtk.Button _ariaMaxConnectionsPerServerResetButton;
    [Gtk.Connect] private readonly Gtk.SpinButton _ariaMinSplitSizeSpin;
    [Gtk.Connect] private readonly Gtk.Button _ariaMinSplitSizeResetButton;
    [Gtk.Connect] private readonly Adw.EntryRow _subtitleLangsRow;
    [Gtk.Connect] private readonly Adw.EntryRow _cookiesRow;
    [Gtk.Connect] private readonly Gtk.Popover _cookiesPopover;
    [Gtk.Connect] private readonly Gtk.Button _chromeCookiesButton;
    [Gtk.Connect] private readonly Gtk.Button _firefoxCookiesButton;
    [Gtk.Connect] private readonly Gtk.Button _selectCookiesFileButton;
    [Gtk.Connect] private readonly Gtk.Button _unsetCookiesFileButton;
    [Gtk.Connect] private readonly Gtk.Switch _disallowConversionsSwitch;
    [Gtk.Connect] private readonly Adw.ExpanderRow _embedMetadataRow;
    [Gtk.Connect] private readonly Gtk.Switch _cropAudioThumbnailSwitch;
    [Gtk.Connect] private readonly Gtk.Switch _embedChaptersSwitch;

    private PreferencesDialog(Gtk.Builder builder, PreferencesViewController controller, Adw.Application application, Gtk.Window parent) : base(builder.GetPointer("_root"), false)
    {
        //Window Settings
        _controller = controller;
        _application = application;
        SetTransientFor(parent);
        SetIconName(_controller.AppInfo.ID);
        //Build UI
        builder.Connect(this);
        _themeRow.OnNotify += (sender, e) =>
        {
            if (e.Pspec.GetName() == "selected-item")
            {
                OnThemeChanged();
            }
        };
        _subtitleLangsRow.OnApply += SubtitleLangsChanged;
        _chromeCookiesButton.OnClicked += async (sender, e) => await LaunchChromeCookiesExtensionAsync();
        _firefoxCookiesButton.OnClicked += async (sender, e) => await LaunchFirefoxCookiesExtensionAsync();
        _selectCookiesFileButton.OnClicked += async (sender, e) => await SelectCookiesFileAsync();
        _unsetCookiesFileButton.OnClicked += UnsetCookiesFile;
        OnHide += Hide;
        //Load Config
        _themeRow.SetSelected((uint)_controller.Theme);
        _completedNotificationRow.SetSelected((uint)_controller.CompletedNotificationPreference);
        _backgroundRow.SetVisible(File.Exists("/.flatpak-info") || !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("SNAP")));
        _backgroundSwitch.SetActive(_controller.RunInBackground);
        _maxNumberOfActiveDownloadsSpin.SetValue(_controller.MaxNumberOfActiveDownloads);
        _overwriteSwitch.SetActive(_controller.OverwriteExistingFiles);
        _limitCharactersSwitch.SetActive(_controller.LimitCharacters);
        _speedLimitSpin.SetValue(_controller.SpeedLimit);
        _useAriaRow.SetEnableExpansion(_controller.UseAria);
        _ariaMaxConnectionsPerServerSpin.SetValue(_controller.AriaMaxConnectionsPerServer);
        _ariaMaxConnectionsPerServerResetButton.OnClicked += (sender, e) => _ariaMaxConnectionsPerServerSpin.SetValue(16);
        _ariaMinSplitSizeSpin.SetValue(_controller.AriaMinSplitSize);
        _ariaMinSplitSizeResetButton.OnClicked += (sender, e) => _ariaMinSplitSizeSpin.SetValue(20);
        _subtitleLangsRow.SetText(_controller.SubtitleLangs);
        if (File.Exists(_controller.CookiesPath))
        {
            _cookiesRow.SetText(_controller.CookiesPath);
        }
        _disallowConversionsSwitch.SetActive(_controller.DisallowConversions);
        _embedMetadataRow.SetEnableExpansion(_controller.EmbedMetadata);
        _cropAudioThumbnailSwitch.SetActive(_controller.CropAudioThumbnails);
        _embedChaptersSwitch.SetActive(_controller.EmbedChapters);
    }

    /// <summary>
    /// Constructs a PreferencesDialog
    /// </summary>
    /// <param name="controller">PreferencesViewController</param>
    /// <param name="application">Adw.Application</param>
    /// <param name="parent">Gtk.Window</param>
    public PreferencesDialog(PreferencesViewController controller, Adw.Application application, Gtk.Window parent) : this(Builder.FromFile("preferences_dialog.ui"), controller, application, parent)
    {
    }

    /// <summary>
    /// Occurs when the dialog is hidden
    /// </summary>
    /// <param name="sender">Gtk.Widget</param>
    /// <param name="e">EventArgs</param>
    private void Hide(Gtk.Widget sender, EventArgs e)
    {
        _controller.CompletedNotificationPreference = (NotificationPreference)_completedNotificationRow.GetSelected();
        _controller.RunInBackground = _backgroundSwitch.GetActive();
        _controller.MaxNumberOfActiveDownloads = (int)_maxNumberOfActiveDownloadsSpin.GetValue();
        _controller.OverwriteExistingFiles = _overwriteSwitch.GetActive();
        _controller.LimitCharacters = _limitCharactersSwitch.GetActive();
        _controller.SpeedLimit = (uint)_speedLimitSpin.GetValue();
        _controller.UseAria = _useAriaRow.GetEnableExpansion();
        _controller.AriaMaxConnectionsPerServer = (int)_ariaMaxConnectionsPerServerSpin.GetValue();
        _controller.AriaMinSplitSize = (int)_ariaMinSplitSizeSpin.GetValue();
        _controller.DisallowConversions = _disallowConversionsSwitch.GetActive();
        _controller.EmbedMetadata = _embedMetadataRow.GetEnableExpansion();
        _controller.CropAudioThumbnails = _cropAudioThumbnailSwitch.GetActive();
        _controller.EmbedChapters = _embedChaptersSwitch.GetActive();
        _controller.SaveConfiguration();
        Destroy();
    }

    /// <summary>
    /// Occurs when the theme selection is changed
    /// </summary>
    private void OnThemeChanged()
    {
        _controller.Theme = (Theme)_themeRow.GetSelected();
        _application.StyleManager!.ColorScheme = _controller.Theme switch
        {
            Theme.System => Adw.ColorScheme.PreferLight,
            Theme.Light => Adw.ColorScheme.ForceLight,
            Theme.Dark => Adw.ColorScheme.ForceDark,
            _ => Adw.ColorScheme.PreferLight
        };
    }

    /// <summary>
    /// Occurs when the subtitle langs row is applied
    /// </summary>
    /// <param name="sender">Adw.EntryRow</param>
    /// <param name="e">EventArgs</param>
    private void SubtitleLangsChanged(Adw.EntryRow sender, EventArgs e)
    {
        _subtitleLangsRow.SetTitle(_("Subtitle Languages (Comma-Separated)"));
        _subtitleLangsRow.RemoveCssClass("error");
        var valid = _controller.ValidateSubtitleLangs(_subtitleLangsRow.GetText());
        if(valid)
        {
            _controller.SubtitleLangs = _subtitleLangsRow.GetText();
            _controller.SaveConfiguration();
        }
        else
        {
            _subtitleLangsRow.SetTitle(_("Subtitle Languages (Invalid)"));
            _subtitleLangsRow.AddCssClass("error");
        }
    }

    /// <summary>
    /// Occurs when a button to select cookies file is clicked
    /// </summary>
    private async Task SelectCookiesFileAsync()
    {
        var filterTxt = Gtk.FileFilter.New();
        filterTxt.SetName("TXT (*.txt)");
        filterTxt.AddPattern("*.txt");
        filterTxt.AddPattern("*.TXT");
        var fileDialog = Gtk.FileDialog.New();
        fileDialog.SetTitle(_("Select Cookies File"));
        var filters = Gio.ListStore.New(Gtk.FileFilter.GetGType());
        filters.Append(filterTxt);
        fileDialog.SetFilters(filters);
        try
        {
            var file = await fileDialog.OpenAsync(this);
            _controller.CookiesPath = file.GetPath();
            _cookiesRow.SetText(file.GetPath());
        }
        catch { }
    }

    /// <summary>
    /// Occurs when a button to clear cookies file is clicked
    /// </summary>
    /// <param name="sender">Gtk.Button</param>
    /// <param name="e">EventArgs</param>
    private void UnsetCookiesFile(Gtk.Button sender, EventArgs e)
    {
        _controller.CookiesPath = "";
        _cookiesRow.SetText("");
    }

    /// <summary>
    /// Occurs when a button to open chrome's cookies extension is clicked
    /// </summary>
    private async Task LaunchChromeCookiesExtensionAsync()
    {
        _cookiesPopover.Popdown();
        var uriLauncher = Gtk.UriLauncher.New("https://chrome.google.com/webstore/detail/get-cookiestxt-locally/cclelndahbckbenkjhflpdbgdldlbecc");
        try
        {
            await uriLauncher.LaunchAsync(this);
        }
        catch { }
    }

    /// <summary>
    /// Occurs when a button to open firefox's cookies extension is clicked
    /// </summary>
    private async Task LaunchFirefoxCookiesExtensionAsync()
    {
        _cookiesPopover.Popdown();
        var uriLauncher = Gtk.UriLauncher.New("https://addons.mozilla.org/en-US/firefox/addon/cookies-txt/");
        try
        {
            await uriLauncher.LaunchAsync(this);
        }
        catch { }
    }
}
