using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenMacroSwift.Desktop.Models;
using OpenMacroSwift.Desktop.Services;
using Client.Services.Fishing;
using Wpf.Ui.Controls;

namespace OpenMacroSwift.Desktop.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    private readonly IMacroIpcClient ipcClient;
    private HotbarRodReader? rodReader;
    private readonly DispatcherTimer statusTimer;
    private readonly DispatcherTimer rodTimer;
    private bool refreshInFlight;
    private int statusPollCounter;

    public ShellViewModel(IMacroIpcClient ipcClient)
    {
        this.ipcClient = ipcClient;

        NavigationItems =
        [
            new("General", "General", SymbolRegular.Home32),
            new("Fishing", "Fishing", SymbolRegular.Target24),
            new("Addons", "Addons", SymbolRegular.PuzzlePiece24),
            new("Automation", "Automation", SymbolRegular.Bot24),
            new("Account", "Account", SymbolRegular.Person32),
            new("Settings", "Settings", SymbolRegular.Settings32),
        ];

        AddonModules =
        [
            new("Auto Aquarium", "Cycles aquarium actions while the main macro is running.", SymbolRegular.Apps24),
            new("Auto Totem", "Handles selected totem and cycle preferences.", SymbolRegular.WeatherSunny28),
            new("Auto Sovereign Recharge", "Keeps sovereign charge inside the configured range.", SymbolRegular.Power24),
            new("Hunt Detect", "Detects configured hunts and sends Discord alerts.", SymbolRegular.Radar20),
        ];

        AutomationModules =
        [
            new("Auto Angler", "Automates angler flow and cursor sampling.", SymbolRegular.NavigationPlay20),
            new("Enchant", "Searches, applies, and validates enchant targets.", SymbolRegular.Wand24),
            new("Appraise", "Runs appraise loops with mutation and size filters.", SymbolRegular.Sparkle24),
            new("Treasure Appraise", "Appraises treasure by multi and click delay rules.", SymbolRegular.Trophy28),
        ];

        HuntTargets =
        [
            new("Sovereign Surge"),
            new("Sovereign Storm"),
            new("Sovereign Reckoning"),
            new("Wisp Haunt"),
            new("Soul Scourge"),
            new("Styx Angler"),
            new("Storm Flood"),
            new("Tidecrasher Archon"),
            new("Megalodon"),
            new("Ancient Megalodon"),
            new("Phantom Megalodon"),
            new("Solar Chorus"),
            new("Helios Sunray"),
            new("Olympian Devil"),
            new("Kerauno Wyrm"),
            new("War Surge"),
            new("Legionnaire Lamprey"),
            new("Kraken"),
            new("Ancient Kraken"),
            new("Leviathan"),
            new("Profane Leviathan"),
            new("Skeletal Leviathan"),
            new("Beluga"),
            new("Narwhal"),
            new("Magician Narwhal"),
            new("Mosslurker"),
            new("Dreadfin"),
            new("Megamouth Shark"),
            new("Orca Migration"),
            new("Whale Migration"),
            new("Humpback Whale"),
            new("Great White Shark"),
            new("Great Hammerhead Shark"),
            new("Whale Shark"),
            new("Plesiosaur"),
            new("Pliosaur"),
            new("Ancestral Pliosaur"),
            new("Reef Titan"),
            new("Colossus Reef Titan"),
            new("Omnithal"),
            new("Awakened Omnithal"),
            new("Goldwraith"),
            new("Ancient Goldwraith"),
            new("Colossal Ancient Dragon"),
            new("Colossal Blue Dragon"),
            new("Colossal Ethereal Dragon"),
            new("Scylla"),
            new("Mossjaw"),
            new("Elder Mossjaw"),
            new("Flower Guardian"),
            new("Rotbloom"),
            new("Toxic Guardian"),
            new("Ashclaw"),
            new("Frostwyrm"),
            new("Wyvern"),
            new("Baby Bloop Fish"),
            new("Bloop Fish"),
            new("Sunken Chests"),
            new("Earthquake"),
        ];

        foreach (HuntTarget target in HuntTargets)
        {
            target.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(HuntTarget.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedHuntTargets));
                }
            };
        }

        TrackerModes = ["Predict", "Spam", "Hybrid"];
        CastingModes = ["Normal", "Perfect Cast"];
        RodSlots = ["1", "2", "3", "4"];
        TotemOptions = ["None", "Clearcast Totem", "Tempest Totem", "Windset Totem", "Aurora Totem", "Smokescreen Totem", "Eclipse Totem", "Starfall Totem"];
        EnchantTargets = ["Sea Overlord", "Blessed Song", "Resonance", "Abyssal", "Hasty", "Quality", "Clever", "Controlled", "Storming", "Swift", "Tryhard"];
        Mutations = ["None", "Albino", "Darkened", "Negative", "Glossy", "Lunar", "Translucent", "Electric", "Hexed", "Silver", "Frozen", "Mosaic", "Scorched", "Amber", "Abyssal", "Coral", "Decayed", "Poisoned", "Fossilized", "Vined", "Crimson", "Honey", "Midas", "Boreal", "Fallen", "Greedy", "Spirit", "Mourned", "Mythical", "Shrouded"];

        SelectedNavigationItem = NavigationItems[0];
        SelectedAddonModule = AddonModules[0];
        SelectedAutomationModule = AutomationModules[0];
        statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        statusTimer.Tick += async (_, _) => await RefreshStatusAsync();
        statusTimer.Start();

        rodTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        rodTimer.Tick += (_, _) => RefreshRodStatus();
        rodTimer.Start();
    }

    public ObservableCollection<NavigationItem> NavigationItems { get; }
    public ObservableCollection<MacroModule> AddonModules { get; }
    public ObservableCollection<MacroModule> AutomationModules { get; }
    public ObservableCollection<HuntTarget> HuntTargets { get; }
    public ObservableCollection<string> ActivityFeed { get; } = ["System ready", "Awaiting macro start"];
    public ObservableCollection<string> TrackerModes { get; }
    public ObservableCollection<string> CastingModes { get; }
    public ObservableCollection<string> RodSlots { get; }
    public ObservableCollection<string> TotemOptions { get; }
    public ObservableCollection<string> EnchantTargets { get; }
    public ObservableCollection<string> Mutations { get; }
    [ObservableProperty]
    private string currentRodText = "---";

    [ObservableProperty]
    private string equippedStateText = "No";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGeneralVisible))]
    [NotifyPropertyChangedFor(nameof(IsFishingVisible))]
    [NotifyPropertyChangedFor(nameof(IsAddonsVisible))]
    [NotifyPropertyChangedFor(nameof(IsAutomationVisible))]
    [NotifyPropertyChangedFor(nameof(IsHuntDetectVisible))]
    [NotifyPropertyChangedFor(nameof(IsAccountVisible))]
    [NotifyPropertyChangedFor(nameof(IsSettingsVisible))]
    private string currentPage = "General";

    [ObservableProperty]
    private NavigationItem? selectedNavigationItem;

    [ObservableProperty]
    private MacroModule? selectedAddonModule;

    [ObservableProperty]
    private MacroModule? selectedAutomationModule;

    [ObservableProperty]
    private bool isRunning;

    [ObservableProperty]
    private string statusText = "Stopped";

    [ObservableProperty]
    private string runtimeText = "00:00:00";

    [ObservableProperty]
    private string trackingPhaseText = "---";

    [ObservableProperty]
    private string updateStatusText = "Up to date";

    [ObservableProperty]
    private string offsetsText = "Offsets ---";

    [ObservableProperty]
    private string hotkeyText = "F3";

    [ObservableProperty]
    private bool isRebindingHotkey;

    private string committedHotkeyText = "F3";

    [ObservableProperty]
    private string selectedTrackerMode = "Predict";

    [ObservableProperty]
    private string selectedCastingMode = "Normal";

    [ObservableProperty]
    private string selectedRodSlot = "1";

    [ObservableProperty]
    private string equippedRodText = "---";

    [ObservableProperty]
    private string progressText = "---";

    [ObservableProperty]
    private string inputStatusText = "Released";

    [ObservableProperty]
    private string lostText = "0";

    [ObservableProperty]
    private string successRateText = "100%";

    [ObservableProperty]
    private bool isMasterlineEquipped = true;

    [ObservableProperty]
    private string masterlineRodNamesText = "Wind Elemental | Nates Blade | Eardrum";

    [ObservableProperty]
    private bool isMasterlineRodSelected;

    [ObservableProperty]
    private bool autoAquariumEnabled;

    [ObservableProperty]
    private bool autoTotemEnabled;

    [ObservableProperty]
    private bool autoSovereignRechargeEnabled;

    [ObservableProperty]
    private bool huntDetectEnabled;

    [ObservableProperty]
    private string autoAquariumCycleDelayMinutes = "65";

    [ObservableProperty]
    private string sovereignMinPercent = "45";

    [ObservableProperty]
    private string sovereignMaxPercent = "92";

    [ObservableProperty]
    private string sovereignStatusText = "Idle";

    [ObservableProperty]
    private bool useShinyTotem;

    [ObservableProperty]
    private bool useSparklingTotem;

    [ObservableProperty]
    private bool useMutationTotem;

    [ObservableProperty]
    private bool stayDay;

    [ObservableProperty]
    private bool stayNight;

    [ObservableProperty]
    private string totemStatusText = "Waiting for cycle window.";

    [ObservableProperty]
    private bool autoAnglerEnabled;

    [ObservableProperty]
    private bool autoEnchantEnabled;

    [ObservableProperty]
    private bool autoAppraiseEnabled;

    [ObservableProperty]
    private bool autoTreasureEnabled;

    [ObservableProperty]
    private string currentFishText = "---";

    [ObservableProperty]
    private string autoAnglerStatusText = "Idle";

    [ObservableProperty]
    private bool isEnchantGamepassMode = true;

    [ObservableProperty]
    private string targetSearchText = string.Empty;

    [ObservableProperty]
    private string selectedTargetEnchant = "Sea Overlord";

    [ObservableProperty]
    private string newEnchantText = string.Empty;

    [ObservableProperty]
    private string currentEnchantText = "---";

    [ObservableProperty]
    private string enchantStatusText = "Waiting for rod.";

    [ObservableProperty]
    private bool isAppraiseGamepassMode = true;

    [ObservableProperty]
    private double gamepassSpeed = 0.6;

    [ObservableProperty]
    private bool requireShiny;

    [ObservableProperty]
    private bool requireSparkling;

    [ObservableProperty]
    private bool requireTiny;

    [ObservableProperty]
    private bool requireSmall;

    [ObservableProperty]
    private bool requireBig;

    [ObservableProperty]
    private bool requireGiant;

    [ObservableProperty]
    private string mutationSearchText = string.Empty;

    [ObservableProperty]
    private string selectedMutation = "None";

    [ObservableProperty]
    private string newMutationText = string.Empty;

    [ObservableProperty]
    private string appraiseHeldFishText = "---";

    [ObservableProperty]
    private string appraiseStatusText = "---";

    [ObservableProperty]
    private string treasureClickDelaySeconds = "0.25";

    [ObservableProperty]
    private string treasureMinimumMulti = "0.00";

    [ObservableProperty]
    private string treasureStatusText = "Idle";

    [ObservableProperty]
    private string huntSearchText = string.Empty;

    [ObservableProperty]
    private string newHuntTargetText = string.Empty;

    [ObservableProperty]
    private string discordWebhook = string.Empty;

    [ObservableProperty]
    private string username = "ethan";

    [ObservableProperty]
    private string displayName = "OpenMacro User";

    [ObservableProperty]
    private string currentPassword = string.Empty;

    [ObservableProperty]
    private string newPassword = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private string selectedTotem = "Aurora Totem";

    [ObservableProperty]
    private string themePreset = "Dark";

    [ObservableProperty]
    private string accentColor = "#A9CFCB";

    [ObservableProperty]
    private string backgroundColor = "#0E0E0E";

    [ObservableProperty]
    private string surfaceColor = "#161616";

    [ObservableProperty]
    private string borderColor = "#2A2A2A";

    [ObservableProperty]
    private string textColor = "#F0F0F0";

    private bool isApplyingStatus;

    public bool IsGeneralVisible => CurrentPage == "General";
    public bool IsFishingVisible => CurrentPage == "Fishing";
    public bool IsAddonsVisible => CurrentPage == "Addons";
    public bool IsAutomationVisible => CurrentPage == "Automation";
    public bool IsHuntDetectVisible => CurrentPage == "HuntDetect";
    public bool IsAccountVisible => CurrentPage == "Account";
    public bool IsSettingsVisible => CurrentPage == "Settings";

    public IEnumerable<HuntTarget> FilteredHuntTargets =>
        string.IsNullOrWhiteSpace(HuntSearchText)
            ? HuntTargets
            : HuntTargets.Where(t => t.Name.Contains(HuntSearchText, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<HuntTarget> SelectedHuntTargets => HuntTargets.Where(t => t.IsSelected);

    public bool IsAutoAnglerSelected => SelectedAutomationModule?.Title == "Auto Angler";
    public bool IsEnchantSelected => SelectedAutomationModule?.Title == "Enchant";
    public bool IsAppraiseSelected => SelectedAutomationModule?.Title == "Appraise";
    public bool IsTreasureSelected => SelectedAutomationModule?.Title == "Treasure Appraise";

    public bool IsAutoAquariumSelected => SelectedAddonModule?.Title == "Auto Aquarium";
    public bool IsAutoTotemSelected => SelectedAddonModule?.Title == "Auto Totem";
    public bool IsAutoSovereignRechargeSelected => SelectedAddonModule?.Title == "Auto Sovereign Recharge";
    public bool IsHuntDetectAddonSelected => SelectedAddonModule?.Title == "Hunt Detect";

    partial void OnSelectedNavigationItemChanged(NavigationItem? value)
    {
        if (value is null) return;
        CurrentPage = value.PageKey;

        foreach (NavigationItem item in NavigationItems)
        {
            item.IsSelected = item == value;
        }
    }

    partial void OnSelectedAddonModuleChanged(MacroModule? value)
    {
        OnPropertyChanged(nameof(IsAutoAquariumSelected));
        OnPropertyChanged(nameof(IsAutoTotemSelected));
        OnPropertyChanged(nameof(IsAutoSovereignRechargeSelected));
        OnPropertyChanged(nameof(IsHuntDetectAddonSelected));
    }

    partial void OnSelectedAutomationModuleChanged(MacroModule? value)
    {
        OnPropertyChanged(nameof(IsAutoAnglerSelected));
        OnPropertyChanged(nameof(IsEnchantSelected));
        OnPropertyChanged(nameof(IsAppraiseSelected));
        OnPropertyChanged(nameof(IsTreasureSelected));
    }

    partial void OnHuntSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredHuntTargets));
        QueueSetting("HuntSearchText", value);
    }

    partial void OnSelectedTrackerModeChanged(string value)
    {
        QueueSetting("TrackingMode", value);
    }

    partial void OnSelectedCastingModeChanged(string value)
    {
        QueueSetting("CastingMode", value);
    }

    partial void OnSelectedRodSlotChanged(string value)
    {
        QueueSetting("RodSlot", value);
    }

    partial void OnAutoAquariumEnabledChanged(bool value) => QueueSetting("AutoAquariumEnabled", value);
    partial void OnAutoAquariumCycleDelayMinutesChanged(string value) => QueueSetting("AutoAquariumCycleDelayMinutes", value);
    partial void OnAutoTotemEnabledChanged(bool value) => QueueSetting("AutoTotemEnabled", value);
    partial void OnSelectedTotemChanged(string value) => QueueSetting("SelectedTotem", value);
    partial void OnUseShinyTotemChanged(bool value) => QueueSetting("UseShinyTotem", value);
    partial void OnUseSparklingTotemChanged(bool value) => QueueSetting("UseSparklingTotem", value);
    partial void OnUseMutationTotemChanged(bool value) => QueueSetting("UseMutationTotem", value);
    partial void OnStayDayChanged(bool value) => QueueSetting("StayDay", value);
    partial void OnStayNightChanged(bool value) => QueueSetting("StayNight", value);
    partial void OnAutoSovereignRechargeEnabledChanged(bool value) => QueueSetting("AutoSovereignRechargeEnabled", value);
    partial void OnSovereignMinPercentChanged(string value) => QueueSetting("SovereignMinPercent", value);
    partial void OnSovereignMaxPercentChanged(string value) => QueueSetting("SovereignMaxPercent", value);
    partial void OnHuntDetectEnabledChanged(bool value) => QueueSetting("HuntDetectEnabled", value);
    partial void OnDiscordWebhookChanged(string value) => QueueSetting("DiscordWebhook", value);
    partial void OnAutoAnglerEnabledChanged(bool value) => QueueSetting("AutoAnglerEnabled", value);
    partial void OnAutoEnchantEnabledChanged(bool value) => QueueSetting("AutoEnchantEnabled", value);
    partial void OnAutoAppraiseEnabledChanged(bool value) => QueueSetting("AutoAppraiseEnabled", value);
    partial void OnAutoTreasureEnabledChanged(bool value) => QueueSetting("AutoTreasureEnabled", value);
    partial void OnIsEnchantGamepassModeChanged(bool value) => QueueSetting("EnchantMode", value ? "Gamepass" : "Normal");
    partial void OnTargetSearchTextChanged(string value) => QueueSetting("TargetSearchText", value);
    partial void OnSelectedTargetEnchantChanged(string value) => QueueSetting("SelectedTargetEnchant", value);
    partial void OnNewEnchantTextChanged(string value) => QueueSetting("NewEnchantText", value);
    partial void OnIsAppraiseGamepassModeChanged(bool value) => QueueSetting("AppraiseMode", value ? "Gamepass" : "Normal");
    partial void OnGamepassSpeedChanged(double value) => QueueSetting("GamepassSpeed", value);
    partial void OnRequireShinyChanged(bool value) => QueueSetting("RequireShiny", value);
    partial void OnRequireSparklingChanged(bool value) => QueueSetting("RequireSparkling", value);
    partial void OnRequireTinyChanged(bool value) => QueueSetting("RequireTiny", value);
    partial void OnRequireSmallChanged(bool value) => QueueSetting("RequireSmall", value);
    partial void OnRequireBigChanged(bool value) => QueueSetting("RequireBig", value);
    partial void OnRequireGiantChanged(bool value) => QueueSetting("RequireGiant", value);
    partial void OnMutationSearchTextChanged(string value) => QueueSetting("MutationSearchText", value);
    partial void OnSelectedMutationChanged(string value) => QueueSetting("SelectedMutation", value);
    partial void OnNewMutationTextChanged(string value) => QueueSetting("NewMutationText", value);
    partial void OnTreasureClickDelaySecondsChanged(string value) => QueueSetting("TreasureClickDelaySeconds", value);
    partial void OnTreasureMinimumMultiChanged(string value) => QueueSetting("TreasureMinimumMulti", value);
    partial void OnUsernameChanged(string value) => QueueSetting("Username", value);
    partial void OnDisplayNameChanged(string value) => QueueSetting("DisplayName", value);
    partial void OnThemePresetChanged(string value) => QueueSetting("ThemePreset", value);
    partial void OnAccentColorChanged(string value) => QueueSetting("AccentColor", value);
    partial void OnBackgroundColorChanged(string value) => QueueSetting("BackgroundColor", value);
    partial void OnSurfaceColorChanged(string value) => QueueSetting("SurfaceColor", value);
    partial void OnBorderColorChanged(string value) => QueueSetting("BorderColor", value);
    partial void OnTextColorChanged(string value) => QueueSetting("TextColor", value);

    [RelayCommand]
    private async Task StartMacroAsync()
    {
        PushActivity("StartMacro requested");
        await SendCommandAndApplyStatusAsync("StartMacro");
        PushActivity("Macro started");
    }

    [RelayCommand]
    private async Task StopMacroAsync()
    {
        PushActivity("StopMacro requested");
        await SendCommandAndApplyStatusAsync("StopMacro");
        PushActivity("Macro stopped");
    }

    [RelayCommand]
    public async Task ToggleMacroAsync()
    {
        PushActivity("ToggleMacro requested");
        if (IsRunning)
        {
            await StopMacroAsync();
        }
        else
        {
            await StartMacroAsync();
        }
    }

    public async Task ToggleMacroFromHotkeyAsync()
    {
        PushActivity("Hotkey toggle requested");
        if (IsRunning)
        {
            await DrainStopUntilStoppedAsync();
            return;
        }

        await StartMacroAsync();
    }

    private async Task DrainStopUntilStoppedAsync()
    {
        PushActivity("Hotkey drain-stop start");
        const int stopDrainIntervalMs = 90;
        const int stopDrainMaxMs = 5000;

        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(stopDrainMaxMs);
        do
        {
            await StopMacroAsync();
            if (!IsRunning)
            {
                PushActivity("Hotkey drain-stop completed");
                return;
            }

            await Task.Delay(stopDrainIntervalMs);
        } while (DateTimeOffset.UtcNow < deadline);

        await StopMacroAsync();
        PushActivity("Hotkey drain-stop completed");
    }

    [RelayCommand]
    private void RebindHotkey()
    {
        if (IsRebindingHotkey)
        {
            return;
        }

        committedHotkeyText = HotkeyText;
        IsRebindingHotkey = true;
        HotkeyText = "Press key...";
        PushActivity("Hotkey capture started");
    }

    public void CancelHotkeyRebind()
    {
        if (!IsRebindingHotkey)
        {
            return;
        }

        IsRebindingHotkey = false;
        HotkeyText = committedHotkeyText;
        PushActivity("Hotkey capture cancelled");
    }

    public async Task CompleteHotkeyRebindAsync(string hotkey)
    {
        if (!IsRebindingHotkey || string.IsNullOrWhiteSpace(hotkey))
        {
            return;
        }

        IsRebindingHotkey = false;
        committedHotkeyText = hotkey;
        HotkeyText = hotkey;

        JsonDocument? response = await ipcClient.SendAsync("UpdateSetting", new { Key = "Hotkey", Value = hotkey });
        if (response is not null)
        {
            ApplyStatus(response.RootElement);
        }

        PushActivity($"Hotkey rebound to {hotkey}");
    }

    [RelayCommand]
    private void SelectPage(NavigationItem item)
    {
        SelectedNavigationItem = item;
    }

    [RelayCommand]
    private void SelectAddon(MacroModule module)
    {
        SelectedAddonModule = module;
    }

    [RelayCommand]
    private void SelectAutomation(MacroModule module)
    {
        SelectedAutomationModule = module;
    }

    [RelayCommand]
    private void ToggleHuntTarget(HuntTarget target)
    {
        target.IsSelected = !target.IsSelected;
        OnPropertyChanged(nameof(SelectedHuntTargets));
        QueueSetting("SelectedHuntTargets", string.Join("|", SelectedHuntTargets.Select(t => t.Name)));
    }

    [RelayCommand]
    private void ClearHunts()
    {
        foreach (HuntTarget target in HuntTargets)
        {
            target.IsSelected = false;
        }
        OnPropertyChanged(nameof(SelectedHuntTargets));
        QueueSetting("SelectedHuntTargets", string.Empty);
    }

    [RelayCommand]
    private void AddHuntTarget()
    {
        string name = NewHuntTargetText.Trim();
        if (name.Length == 0 || HuntTargets.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var target = new HuntTarget(name);
        target.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(HuntTarget.IsSelected))
            {
                OnPropertyChanged(nameof(SelectedHuntTargets));
            }
        };
        HuntTargets.Add(target);
        NewHuntTargetText = string.Empty;
        OnPropertyChanged(nameof(FilteredHuntTargets));
        QueueSetting("HuntTargets", string.Join("|", HuntTargets.Select(t => t.Name)));
    }

    [RelayCommand]
    private void AddEnchant()
    {
        string value = NewEnchantText.Trim();
        if (value.Length == 0 || EnchantTargets.Contains(value)) return;
        EnchantTargets.Add(value);
        SelectedTargetEnchant = value;
        NewEnchantText = string.Empty;
        QueueSetting("EnchantTargets", string.Join("|", EnchantTargets));
    }

    [RelayCommand]
    private void AddMutation()
    {
        string value = NewMutationText.Trim();
        if (value.Length == 0 || Mutations.Contains(value)) return;
        Mutations.Add(value);
        SelectedMutation = value;
        NewMutationText = string.Empty;
        QueueSetting("Mutations", string.Join("|", Mutations));
    }

    [RelayCommand]
    private void UseCursorPosition()
    {
        AutoAnglerStatusText = "Cursor position captured.";
        QueueSetting("AutoAnglerCursorMode", "Captured");
    }

    [RelayCommand]
    private void SaveAccount()
    {
        QueueSetting("Username", Username);
        QueueSetting("DisplayName", DisplayName);
        PushActivity("Account settings saved");
    }

    public async Task InitializeAsync()
    {
        await ipcClient.EnsureCoreStartedAsync();
        await RefreshStatusAsync();
    }

    public void Shutdown()
    {
        statusTimer.Stop();
        rodTimer.Stop();
        rodReader?.Dispose();
    }

    private async Task RefreshStatusAsync()
    {
        if (refreshInFlight)
        {
            return;
        }

        refreshInFlight = true;
        try
        {
            JsonDocument? response = await ipcClient.SendAsync("GetStatus");
            if (response is not null)
            {
                ApplyStatus(response.RootElement);
            }

            statusPollCounter++;
            if (statusPollCounter >= 1)
            {
                statusPollCounter = 0;
                JsonDocument? stats = await ipcClient.SendAsync("GetStats");
                if (stats is not null)
                {
                    ApplyStats(stats.RootElement);
                }
            }
        }
        finally
        {
            refreshInFlight = false;
        }
    }

    private async Task SendCommandAndApplyStatusAsync(string command)
    {
        PushActivity($"IPC {command}");
        JsonDocument? response = await ipcClient.SendAsync(command);
        if (response is not null)
        {
            ApplyStatus(response.RootElement);
        }
        else
        {
            PushActivity($"IPC {command} returned no response");
        }
    }

    private async Task UpdateSettingAsync(string key, string value)
    {
        await ipcClient.SendAsync("UpdateSetting", new { Key = key, Value = value });
    }

    private void QueueSetting(string key, string value)
    {
        if (isApplyingStatus || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _ = UpdateSettingAsync(key, value);
    }

    private void QueueSetting(string key, bool value) => QueueSetting(key, value.ToString());
    private void QueueSetting(string key, double value) => QueueSetting(key, value.ToString(CultureInfo.InvariantCulture));

    private void ApplyStatus(JsonElement root)
    {
        isApplyingStatus = true;
        try
        {
            if (root.TryGetProperty("running", out JsonElement running) && running.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                IsRunning = running.GetBoolean();
            }

            if (root.TryGetProperty("status", out JsonElement status))
            {
                StatusText = status.GetString() ?? StatusText;
            }

            if (root.TryGetProperty("runtime", out JsonElement runtime))
            {
                RuntimeText = runtime.GetString() ?? RuntimeText;
            }

            if (root.TryGetProperty("hotkey", out JsonElement hotkey) && !IsRebindingHotkey)
            {
                HotkeyText = hotkey.GetString() ?? HotkeyText;
                committedHotkeyText = HotkeyText;
            }

            if (root.TryGetProperty("updateStatus", out JsonElement update))
            {
                UpdateStatusText = update.GetString() ?? UpdateStatusText;
            }

            if (root.TryGetProperty("offsetsLoaded", out JsonElement offsetsLoaded))
            {
                bool loaded = offsetsLoaded.ValueKind == JsonValueKind.True;
                string version = root.TryGetProperty("offsetsVersion", out JsonElement offsetsVersion)
                    ? offsetsVersion.GetString() ?? string.Empty
                    : string.Empty;
                int count = root.TryGetProperty("offsetsCount", out JsonElement offsetsCount) && offsetsCount.TryGetInt32(out int parsedCount)
                    ? parsedCount
                    : 0;
                OffsetsText = loaded
                    ? $"Offsets {(string.IsNullOrWhiteSpace(version) ? $"{count} keys" : version)}"
                    : "Offsets missing";
            }

            ApplyString(root, "TrackingMode", value => SelectedTrackerMode = value);
            ApplyString(root, "CastingMode", value => SelectedCastingMode = value);
            ApplyString(root, "RodSlot", value => SelectedRodSlot = value);
            ApplyString(root, "EquippedRod", value => EquippedRodText = value);
            ApplyString(root, "RodEquippedState", value => IsMasterlineEquipped = string.Equals(value, "Equipped", StringComparison.OrdinalIgnoreCase));
            ApplyString(root, "phase", value => TrackingPhaseText = NormalizeTrackingPhase(value));
            ApplyString(root, "Phase", value => TrackingPhaseText = NormalizeTrackingPhase(value));
            ApplyString(root, "InputStatus", value => InputStatusText = NormalizeInputState(value));
            ApplyString(root, "Progress", value => ProgressText = value);
            ApplyString(root, "SuccessRate", value => SuccessRateText = value);
            ApplyString(root, "Lost", value => LostText = value);
            ApplyString(root, "CurrentFish", value => CurrentFishText = value);
            ApplyString(root, "AutoAnglerStatus", value => AutoAnglerStatusText = value);
            ApplyString(root, "CurrentEnchant", value => CurrentEnchantText = value);
            ApplyString(root, "EnchantStatus", value => EnchantStatusText = value);
            ApplyString(root, "AppraiseHeldFish", value => AppraiseHeldFishText = value);
            ApplyString(root, "AppraiseStatus", value => AppraiseStatusText = value);
            ApplyString(root, "TreasureStatus", value => TreasureStatusText = value);
            ApplyString(root, "SovereignStatus", value => SovereignStatusText = value);
            ApplyString(root, "TotemStatus", value => TotemStatusText = value);
            ApplyString(root, "SelectedTotem", value => SelectedTotem = value);
            ApplyString(root, "AutoAquariumCycleDelayMinutes", value => AutoAquariumCycleDelayMinutes = value);
            ApplyString(root, "SovereignMinPercent", value => SovereignMinPercent = value);
            ApplyString(root, "SovereignMaxPercent", value => SovereignMaxPercent = value);
            ApplyString(root, "DiscordWebhook", value => DiscordWebhook = value);
            ApplyString(root, "TargetSearchText", value => TargetSearchText = value);
            ApplyString(root, "SelectedTargetEnchant", value => SelectedTargetEnchant = value);
            ApplyString(root, "NewEnchantText", value => NewEnchantText = value);
            ApplyString(root, "MutationSearchText", value => MutationSearchText = value);
            ApplyString(root, "SelectedMutation", value => SelectedMutation = value);
            ApplyString(root, "NewMutationText", value => NewMutationText = value);
            ApplyString(root, "TreasureClickDelaySeconds", value => TreasureClickDelaySeconds = value);
            ApplyString(root, "TreasureMinimumMulti", value => TreasureMinimumMulti = value);
            ApplyString(root, "Username", value => Username = value);
            ApplyString(root, "DisplayName", value => DisplayName = value);
            ApplyString(root, "ThemePreset", value => ThemePreset = value);
            ApplyString(root, "AccentColor", value => AccentColor = value);
            ApplyString(root, "BackgroundColor", value => BackgroundColor = value);
            ApplyString(root, "SurfaceColor", value => SurfaceColor = value);
            ApplyString(root, "BorderColor", value => BorderColor = value);
            ApplyString(root, "TextColor", value => TextColor = value);
            ApplyDelimitedCollection(root, "EnchantTargets", EnchantTargets);
            ApplyDelimitedCollection(root, "Mutations", Mutations);
            ApplyString(root, "SelectedHuntTargets", ApplySelectedHunts);
            ApplyDelimitedHuntTargets(root, "HuntTargets");

            ApplyBool(root, "AutoAquariumEnabled", value => AutoAquariumEnabled = value);
            ApplyBool(root, "AutoTotemEnabled", value => AutoTotemEnabled = value);
            ApplyBool(root, "AutoSovereignRechargeEnabled", value => AutoSovereignRechargeEnabled = value);
            ApplyBool(root, "HuntDetectEnabled", value => HuntDetectEnabled = value);
            ApplyBool(root, "UseShinyTotem", value => UseShinyTotem = value);
            ApplyBool(root, "UseSparklingTotem", value => UseSparklingTotem = value);
            ApplyBool(root, "UseMutationTotem", value => UseMutationTotem = value);
            ApplyBool(root, "StayDay", value => StayDay = value);
            ApplyBool(root, "StayNight", value => StayNight = value);
            ApplyBool(root, "AutoAnglerEnabled", value => AutoAnglerEnabled = value);
            ApplyBool(root, "AutoEnchantEnabled", value => AutoEnchantEnabled = value);
            ApplyBool(root, "AutoAppraiseEnabled", value => AutoAppraiseEnabled = value);
            ApplyBool(root, "AutoTreasureEnabled", value => AutoTreasureEnabled = value);
            ApplyString(root, "EnchantMode", value => IsEnchantGamepassMode = string.Equals(value, "Gamepass", StringComparison.OrdinalIgnoreCase));
            ApplyString(root, "AppraiseMode", value => IsAppraiseGamepassMode = string.Equals(value, "Gamepass", StringComparison.OrdinalIgnoreCase));
            ApplyBool(root, "RequireShiny", value => RequireShiny = value);
            ApplyBool(root, "RequireSparkling", value => RequireSparkling = value);
            ApplyBool(root, "RequireTiny", value => RequireTiny = value);
            ApplyBool(root, "RequireSmall", value => RequireSmall = value);
            ApplyBool(root, "RequireBig", value => RequireBig = value);
            ApplyBool(root, "RequireGiant", value => RequireGiant = value);

            ApplyDouble(root, "GamepassSpeed", value => GamepassSpeed = value);
        }
        finally
        {
            isApplyingStatus = false;
        }
    }

    private void ApplyStats(JsonElement root)
    {
        isApplyingStatus = true;
        try
        {
            ApplyString(root, "successRate", value => SuccessRateText = value);
            ApplyString(root, "lost", value => LostText = value);
            ApplyString(root, "currentFish", value => CurrentFishText = value);
            ApplyString(root, "phase", value => TrackingPhaseText = NormalizeTrackingPhase(value));
            ApplyString(root, "equippedRod", value => EquippedRodText = value);
            ApplyString(root, "rodEquippedState", value => IsMasterlineEquipped = string.Equals(value, "Equipped", StringComparison.OrdinalIgnoreCase));
            ApplyString(root, "currentEnchant", value => CurrentEnchantText = value);
            ApplyString(root, "appraiseStatus", value => AppraiseStatusText = value);
            ApplyString(root, "appraiseHeldFish", value => AppraiseHeldFishText = value);
            ApplyString(root, "treasureStatus", value => TreasureStatusText = value);
            ApplyString(root, "sovereignStatus", value => SovereignStatusText = value);
            ApplyString(root, "totemStatus", value => TotemStatusText = value);
        }
        finally
        {
            isApplyingStatus = false;
        }
    }

    private static string NormalizeTrackingPhase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "---";
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "FISHING" => "TRACKING",
            "CASTED" => "CASTING",
            "CASTING" => "CASTING",
            "SHAKE" => "SHAKE",
            "TRACKING" => "TRACKING",
            _ => value.Trim().ToUpperInvariant(),
        };
    }

    private static string NormalizeInputState(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Released";
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "HOLDING" or "HELD" or "DOWN" or "PRESSING" => "Held",
            "RELEASED" or "UP" or "IDLE" => "Released",
            _ => value.Trim(),
        };
    }

    private void PushActivity(string message)
    {
        ActivityFeed.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        while (ActivityFeed.Count > 8)
        {
            ActivityFeed.RemoveAt(ActivityFeed.Count - 1);
        }
    }

    private void RefreshRodStatus()
    {
        try
        {
            rodReader ??= new HotbarRodReader();
            var snapshot = rodReader.GetSnapshot();
            if (!string.IsNullOrWhiteSpace(snapshot.RodName))
            {
                CurrentRodText = snapshot.RodName;
            }

            var masterlineSelected = snapshot.RodName.Contains("masterline", StringComparison.OrdinalIgnoreCase);
            IsMasterlineRodSelected = masterlineSelected;
            EquippedStateText = snapshot.IsEquipped ? "Yes" : "No";
            EquippedRodText = snapshot.RodName;

            if (masterlineSelected)
            {
                try
                {
                    var names = rodReader.GetMasterlineOverlayRodNames();
                    MasterlineRodNamesText = names.Count == 0 ? "---" : string.Join(" | ", names);
                }
                catch
                {
                    // Keep the previous names on temporary read failures.
                }
            }
        }
        catch
        {
            // Preserve the last known good values to avoid UI flicker on brief read failures.
        }
    }

    private void ApplySelectedHunts(string value)
    {
        var selected = new HashSet<string>(
            value.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);

        foreach (HuntTarget target in HuntTargets)
        {
            target.IsSelected = selected.Contains(target.Name);
        }

        OnPropertyChanged(nameof(SelectedHuntTargets));
    }

    private void ApplyDelimitedHuntTargets(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out JsonElement value))
        {
            return;
        }

        string? text = value.GetString();
        if (text is null)
        {
            return;
        }

        foreach (string name in text.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (HuntTargets.Any(target => string.Equals(target.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var target = new HuntTarget(name);
            target.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(HuntTarget.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedHuntTargets));
                }
            };
            HuntTargets.Add(target);
        }

        OnPropertyChanged(nameof(FilteredHuntTargets));
    }

    private static void ApplyDelimitedCollection(JsonElement root, string key, ObservableCollection<string> collection)
    {
        if (!root.TryGetProperty(key, out JsonElement value))
        {
            return;
        }

        string? text = value.GetString();
        if (text is null)
        {
            return;
        }

        collection.Clear();
        foreach (string item in text.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!collection.Contains(item))
            {
                collection.Add(item);
            }
        }
    }

    private static void ApplyString(JsonElement root, string key, Action<string> setter)
    {
        if (!root.TryGetProperty(key, out JsonElement value))
        {
            return;
        }

        string? text = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };

        if (text is not null)
        {
            setter(text);
        }
    }

    private static void ApplyBool(JsonElement root, string key, Action<bool> setter)
    {
        if (!root.TryGetProperty(key, out JsonElement value))
        {
            return;
        }

        bool result;
        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            result = value.GetBoolean();
        }
        else if (bool.TryParse(value.GetString(), out bool parsed))
        {
            result = parsed;
        }
        else
        {
            return;
        }

        setter(result);
    }

    private static void ApplyDouble(JsonElement root, string key, Action<double> setter)
    {
        if (!root.TryGetProperty(key, out JsonElement value))
        {
            return;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number))
        {
            setter(number);
            return;
        }

        if (double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
        {
            setter(parsed);
        }
    }

}

