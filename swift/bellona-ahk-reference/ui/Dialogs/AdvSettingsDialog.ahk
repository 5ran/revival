#Requires AutoHotkey v2.0

GetAdvSettingsGui() {
    global APPEARANCE, MAIN, SETTINGS
    static hwnd := 0

    if (hwnd && WinExist("ahk_id " hwnd)) {
        WinActivate("ahk_id " hwnd)
        return
    }

    Accent      := APPEARANCE["accent_color"]
    BgColor     := APPEARANCE["bg_color"]
    TextColor   := APPEARANCE["text_color"]

    GuiShowOpts := "w400 h420 x900 y100"

    mg := Gui("+AlwaysOnTop +Border")
    mg.BackColor := "0x" BgColor
    mg.Title := "Advanced Settings"
    mg.SetFont(, "Segoe UI")

    button.DefaultTextColor := "0x" TextColor
    button.DefaultBg := "0x" Accent

    MainTab := mg.AddTab3("x0 y0 w400 h420 c" Accent, ["Macro", "Auto Totem"])
    MainTab.SetFont("bold")

    MainTab.UseTab(1)
    mg.AddGroupBox("x10 y25 w380 h200 c" TextColor, "Casting").SetFont("s9 bold")

    mg.AddText("x20 y50 w100 h20 c" TextColor, "Cast Mode").SetFont("s10")
    CastMode := mg.AddDDL("x270 y50 w100", ["Perfect", "Short", "Custom"])
    CastModeHelp := mg.AddText("x190 y50 w50 h20 c" Accent, "What?")
    CastModeHelp.SetFont("underline")
    CastModeHelp.OnEvent("Click", (*) => InfoPopup.Show("Cast Mode", "Chooses the target power level where the macro releases the cast. Perfect uses a fixed high threshold for a full cast, Short uses a low threshold for a quick cast, and Custom uses your own Cast Power Threshold value."))

    mg.AddText("x20 y75 w150 h20 c" TextColor, "Cast Power Threshold").SetFont("s10")
    CastPowerThreshold := mg.AddEdit("x270 y75 w100 h20")
    CastPowerThresholdHelp := mg.AddText("x190 y75 w50 h20 c" Accent, "What?")
    CastPowerThresholdHelp.SetFont("underline")
    CastPowerThresholdHelp.OnEvent("Click", (*) => InfoPopup.Show("Cast Power Threshold", "Used only in Custom cast mode. The macro holds left click until the cast power bar reaches this percentage, then releases. Higher values cast farther, lower values cast sooner."))

    mg.AddText("x20 y100 w150 h20 c" TextColor, "Cast Timeout").SetFont("s10")
    CastTimeout := mg.AddEdit("x270 y100 w100 h20")
    CastTimeoutHelp := mg.AddText("x190 y100 w50 h20 c" Accent, "What?")
    CastTimeoutHelp.SetFont("underline")
    CastTimeoutHelp.OnEvent("Click", (*) => InfoPopup.Show("Cast Timeout", "How long the macro waits before giving up on a cast attempt. It is used while waiting for the cast bar to appear and also while waiting for the fishing UI to appear after release. If the timeout is hit, the macro either retries or stops based on Cast on Timeout."))

    mg.AddText("x20 y125 w150 h20 c" TextColor, "Pre-Cast Delay").SetFont("s10")
    PreCastDelay := mg.AddEdit("x270 y125 w100 h20")
    PreCastDelayHelp := mg.AddText("x190 y125 w50 h20 c" Accent, "What?")
    PreCastDelayHelp.SetFont("underline")
    PreCastDelayHelp.OnEvent("Click", (*) => InfoPopup.Show("Pre-Cast Delay", "Extra wait at the start of each cycle before the macro begins holding left click to cast. Increase it if the game needs a short moment before casting is reliable."))

    mg.AddText("x20 y150 w150 h20 c" TextColor, "Post-Cast Delay").SetFont("s10")
    PostCastDelay := mg.AddEdit("x270 y150 w100 h20")
    PostCastDelayHelp := mg.AddText("x190 y150 w50 h20 c" Accent, "What?")
    PostCastDelayHelp.SetFont("underline")
    PostCastDelayHelp.OnEvent("Click", (*) => InfoPopup.Show("Post-Cast Delay", "Wait after releasing the cast before the macro starts the shake phase. Increase it if the game needs extra time between cast release and the hook or shake stage."))

    Border(mg, 20, 180, 350, 1)

    mg.AddText("x40 y193 w100 h20 c" TextColor, "Cast on Timeout").SetFont("s10")
    CastOnTimeout := mg.AddCheckbox("x20 y193 h20 w20")

    SaveCastBtn := button(mg, "Save", 270, 190, {w: 100, h: 23, bg: BgColor, fontSize: 10})

    mg.AddGroupBox("x10 y230 w380 h170 c" TextColor, "Fishing").SetFont("s9 bold")

    mg.AddText("x20 y255 w130 h20 c" TextColor, "Fishing Action Delay").SetFont("s10")
    FishingActionDelay := mg.AddEdit("x270 y255 w100 h20")
    FishingActionDelayHelp := mg.AddText("x190 y255 w50 h20 c" Accent, "What?")
    FishingActionDelayHelp.SetFont("underline")
    FishingActionDelayHelp.OnEvent("Click", (*) => InfoPopup.Show("Fishing Action Delay", "Minimum time between left-click down and up changes while balancing the fish bar. Increase it if rapid hold and release spam causes missed inputs or unstable tracking."))

    mg.AddText("x20 y280 w130 h20 c" TextColor, "Fishing End Grace").SetFont("s10")
    FishingEndGrace := mg.AddEdit("x270 y280 w100 h20")
    FishingEndGraceHelp := mg.AddText("x190 y280 w50 h20 c" Accent, "What?")
    FishingEndGraceHelp.SetFont("underline")
    FishingEndGraceHelp.OnEvent("Click", (*) => InfoPopup.Show("Fishing End Grace", "When the reel UI disappears, the macro waits this long before deciding the catch is finished. Increase it if the fishing UI flickers or briefly disappears before the catch really ends."))

    mg.AddText("x20 y305 w140 h20 c" TextColor, "Completion Threshold").SetFont("s10")
    CompletionThreshold := mg.AddEdit("x270 y305 w100 h20")
    CompletionThresholdHelp := mg.AddText("x190 y305 w50 h20 c" Accent, "What?")
    CompletionThresholdHelp.SetFont("underline")
    CompletionThresholdHelp.OnEvent("Click", (*) => InfoPopup.Show("Completion Threshold", "Progress percentage where the macro considers the catch complete and exits the fishing phase. Slightly below 100% can finish faster if the game visually reaches full before the bar is mathematically perfect."))

    mg.AddText("x20 y330 w130 h20 c" TextColor, "Shake Interval").SetFont("s10")
    ShakeInterval := mg.AddEdit("x270 y330 w100 h20")
    ShakeIntervalHelp := mg.AddText("x190 y330 w50 h20 c" Accent, "What?")
    ShakeIntervalHelp.SetFont("underline")
    ShakeIntervalHelp.OnEvent("Click", (*) => InfoPopup.Show("Shake Interval", "How often the macro sends Enter during the shake phase while waiting for the fishing UI to appear. Lower values shake more aggressively, higher values shake less often."))

    SaveFishBtn := button(mg, "Save", 270, 365, {w: 100, h: 23, bg: BgColor, fontSize: 10})

    MainTab.UseTab(2)
    mg.AddGroupBox("x10 y25 w380 h150 c" TextColor, "Settings").SetFont("s9 bold")

    mg.AddText("x20 y45 w80 h20 c" TextColor, "Totems").SetFont("s10")
    TotemDdl := mg.AddDropDownList("x270 y45 w100 h100")
    TotemDdlCheckBtn := mg.AddText("x190 y45 w60 h20 c" Accent, "Check")
    TotemDdlCheckBtn.SetFont("underline")
    TotemDdlCheckBtn.OnEvent("Click", (*) => RefreshTotemDdl("", true))

    mg.AddText("x20 y70 w80 h20 c" TextColor, "Use Mode").SetFont("s10")
    UseModeDdl := mg.AddDDL("x270 y70 w100 h100", ["On Expire", "Interval"])
    UseModeHelp := mg.AddText("x190 y70 w60 h20 c" Accent, "What?")
    UseModeHelp.SetFont("underline")
    UseModeDdl.Choose(1)

    mg.AddText("x20 y95 w80 h20 c" TextColor, "Inverval (sec)").SetFont("s10")
    TotemInterval := mg.AddEdit("x270 y95 w100 h20", "15")
    TotemIntervalHelp := mg.AddText("x190 y95 w60 h20 c" Accent, "What?")
    TotemIntervalHelp.SetFont("underline")

    Border(mg, 20, 125, 350, 1)

    AutoTotemEnabled := mg.AddCheckbox("x20 y140 h20 w20")
    mg.AddText("x40 y141 w60 h20 c" TextColor, "Enable").SetFont("s10")

    SaveTotemBtn := button(mg, "Save", 270, 138, {w: 100, h: 23, bg: BgColor, fontSize: 10})

    mg.AddGroupBox("x10 y180 w380 h110 c" TextColor, "Delays").SetFont("s9 bold")

    mg.AddText("x20 y200 w150 h20 c" TextColor, "Post Catch Delay").SetFont("s10")
    TotemPostCatchDelay := mg.AddEdit("x270 y200 w100 h20")
    TotemPostCatchDelayHelp := mg.AddText("x190 y200 w50 h20 c" Accent, "What?")
    TotemPostCatchDelayHelp.SetFont("underline")
    TotemPostCatchDelayHelp.OnEvent("Click", (*) => InfoPopup.Show("Post Catch Delay", "Wait after a catch finishes before the next cast or any totem use. The game can briefly ignore inputs right after a catch, so a short delay avoids silent misses and failed cast starts."))

    mg.AddText("x20 y225 w150 h20 c" TextColor, "Rod Equip Delay").SetFont("s10")
    TotemPostEquipDelay := mg.AddEdit("x270 y225 w100 h20")
    TotemPostEquipDelayHelp := mg.AddText("x190 y225 w50 h20 c" Accent, "What?")
    TotemPostEquipDelayHelp.SetFont("underline")
    TotemPostEquipDelayHelp.OnEvent("Click", (*) => InfoPopup.Show("Rod Equip Delay", "Wait after re-equipping the rod (slot 1) before normal fishing resumes. Increase it if the rod needs a short settle period before the next cast."))

    SaveDelaysBtn := button(mg, "Save", 270, 255, {w: 100, h: 23, bg: BgColor, fontSize: 10})

    MainTab.Choose(2)

    ApplyCastMode(*) {
        switch CastMode.Text {
            case "Perfect":
                CastPowerThreshold.Value := "96%"
                CastPowerThreshold.Enabled := false
            case "Short":
                CastPowerThreshold.Value := "25%"
                CastPowerThreshold.Enabled := false
            case "Custom":
                CastPowerThreshold.Value := MAIN["cast_power_custom"] "%"
                CastPowerThreshold.Enabled := true
        }
    }

    CastMode.OnEvent("Change", ApplyCastMode)

    ApplyUseMode(*) {
        TotemInterval.Enabled := (UseModeDdl.Value = 2)
    }

    UseModeDdl.OnEvent("Change", ApplyUseMode)

    LoadAdvFields() {
        switch MAIN["cast_mode"] {
            case "short":  CastMode.Choose(2)
            case "custom": CastMode.Choose(3)
            default:       CastMode.Choose(1)
        }
        ApplyCastMode()

        if (MAIN["cast_mode"] = "custom")
            CastPowerThreshold.Value := MAIN["cast_power_custom"] "%"

        CastTimeout.Value := MAIN["cast_timeout_ms"] / 1000
        PreCastDelay.Value := MAIN["pre_cast_delay_ms"]
        PostCastDelay.Value := MAIN["post_cast_delay_ms"]
        CastOnTimeout.Value := MAIN["cast_on_timeout"]

        FishingActionDelay.Value := MAIN["fishing_action_delay_ms"]
        FishingEndGrace.Value := MAIN["fishing_end_grace_ms"]
        CompletionThreshold.Value := Format("{:.1f}", MAIN["completion_threshold"]) "%"
        ShakeInterval.Value := MAIN["shake_interval_ms"]

        AutoTotemEnabled.Value := MAIN["auto_totem_enabled"]
        UseModeDdl.Choose(MAIN["auto_totem_mode"] = "interval" ? 2 : 1)
        TotemInterval.Value := MAIN["auto_totem_interval_sec"]
        TotemPostCatchDelay.Value := MAIN["post_catch_delay_ms"]
        TotemPostEquipDelay.Value := MAIN["post_totem_delay_ms"]
        ApplyUseMode()
    }

    LoadFallbackTotemDdl(preferredName := "") {
        fallbackName := preferredName != "" ? preferredName : MAIN["auto_totem_name"]
        if (fallbackName = "")
            fallbackName := "Aurora Totem"

        try TotemDdl.Delete()
        TotemDdl.Add([fallbackName])
        TotemDdl.Choose(1)
    }

    RefreshTotemDdl(preferredName := "", interactive := false) {
        currentName := preferredName != "" ? preferredName : TotemDdl.Text
        if (currentName = "No Totems found" || currentName = "")
            currentName := ""

        if !EnsureRobloxReady(interactive, true) {
            LoadFallbackTotemDdl(currentName)
            return
        }

        totems := GetHotbarTotems()

        try TotemDdl.Delete()

        if (totems.Length = 0) {
            TotemDdl.Add(["No Totems found"])
            TotemDdl.Choose(1)
            return
        }

        TotemDdl.Add(totems)

        if (currentName != "") {
            try ControlChooseString(currentName, TotemDdl)
            catch
                TotemDdl.Choose(1)
        } else {
            TotemDdl.Choose(1)
        }
    }

    SaveTotemSettings(*) {
        rawInterval := Trim(TotemInterval.Value)
        previousInterval := MAIN["auto_totem_interval_sec"]

        if !RegExMatch(rawInterval, "^\d+$") || (rawInterval + 0) < 1 {
            TotemInterval.Value := previousInterval
            MsgBox("Interval must be a whole number greater than 0.", "Invalid Value")
            return
        }

        selectedTotem := (TotemDdl.Text = "Aurora Totem") ? "Aurora Totem" : ""
        selectedMode := (UseModeDdl.Value = 2) ? "interval" : "expire"
        intervalSec := rawInterval + 0

        MAIN["auto_totem_enabled"] := AutoTotemEnabled.Value
        SETTINGS["main"]["auto_totem_enabled"] := AutoTotemEnabled.Value

        MAIN["auto_totem_name"] := selectedTotem
        SETTINGS["main"]["auto_totem_name"] := selectedTotem

        MAIN["auto_totem_mode"] := selectedMode
        SETTINGS["main"]["auto_totem_mode"] := selectedMode

        MAIN["auto_totem_interval_sec"] := intervalSec
        SETTINGS["main"]["auto_totem_interval_sec"] := intervalSec

        SaveSettingsFile()
        if (SETTINGS["last_config"] != "" && FileExist(CONFIGS_DIR "\" SETTINGS["last_config"] ".json"))
            SaveConfig(SETTINGS["last_config"])

        RefreshTotemDdl(selectedTotem)
        SaveTotemBtn.ctrl.Value := "Saved!"
        SetTimer(RevertTotemBtn, -1500)
    }

    SaveTotemDelaySettings(*) {
        rawPostCatch := Trim(TotemPostCatchDelay.Value)
        if !IsInteger(rawPostCatch) || (rawPostCatch + 0) < 0 {
            TotemPostCatchDelay.Value := MAIN["post_catch_delay_ms"]
            MsgBox("Post Catch Delay must be a non-negative whole number (ms).", "Invalid Value")
            return
        }

        rawPostEquip := Trim(TotemPostEquipDelay.Value)
        if !IsInteger(rawPostEquip) || (rawPostEquip + 0) < 0 {
            TotemPostEquipDelay.Value := MAIN["post_totem_delay_ms"]
            MsgBox("Rod Equip Delay must be a non-negative whole number (ms).", "Invalid Value")
            return
        }

        postCatchMs := rawPostCatch + 0
        postEquipMs := rawPostEquip + 0

        MAIN["post_catch_delay_ms"] := postCatchMs
        SETTINGS["main"]["post_catch_delay_ms"] := postCatchMs

        MAIN["post_totem_delay_ms"] := postEquipMs
        SETTINGS["main"]["post_totem_delay_ms"] := postEquipMs

        SaveSettingsFile()
        if (SETTINGS["last_config"] != "" && FileExist(CONFIGS_DIR "\" SETTINGS["last_config"] ".json"))
            SaveConfig(SETTINGS["last_config"])

        SaveDelaysBtn.ctrl.Value := "Saved!"
        SetTimer(RevertDelaysBtn, -1500)
    }

    SaveCastSettings(*) {
        modeMap := Map(1, "perfect", 2, "short", 3, "custom")
        MAIN["cast_mode"] := modeMap[CastMode.Value]
        SETTINGS["main"]["cast_mode"] := MAIN["cast_mode"]

        if (CastMode.Text = "Custom") {
            raw := RegExReplace(CastPowerThreshold.Value, "%")
            if (IsNumber(raw)) {
                v := Max(1.0, Min(100.0, raw + 0.0))
                MAIN["cast_power_custom"] := v
                SETTINGS["main"]["cast_power_custom"] := v
            }
        }

        raw := Trim(CastTimeout.Value)
        if (IsNumber(raw) && raw + 0 >= 0) {
            v := Round(raw * 1000)
            MAIN["cast_timeout_ms"] := v
            SETTINGS["main"]["cast_timeout_ms"] := v
        }

        for key, ctrl in Map(
            "pre_cast_delay_ms", PreCastDelay,
            "post_cast_delay_ms", PostCastDelay)
        {
            raw := Trim(ctrl.Value)
            if (IsInteger(raw) && raw + 0 >= 0) {
                MAIN[key] := raw + 0
                SETTINGS["main"][key] := raw + 0
            }
        }

        MAIN["cast_on_timeout"] := CastOnTimeout.Value
        SETTINGS["main"]["cast_on_timeout"] := CastOnTimeout.Value

        SaveSettingsFile()
        if (SETTINGS["last_config"] != "" && FileExist(CONFIGS_DIR "\" SETTINGS["last_config"] ".json"))
            SaveConfig(SETTINGS["last_config"])
        LoadAdvFields()
        SaveCastBtn.ctrl.Value := "Saved!"
        SetTimer(RevertCastBtn, -1500)
    }

    RevertCastBtn(*) {
        try SaveCastBtn.ctrl.Value := "Save"
    }

    SaveFishSettings(*) {
        for key, ctrl in Map(
            "fishing_action_delay_ms", FishingActionDelay,
            "fishing_end_grace_ms", FishingEndGrace,
            "shake_interval_ms", ShakeInterval)
        {
            raw := Trim(ctrl.Value)
            if (IsInteger(raw) && raw + 0 >= 0) {
                MAIN[key] := raw + 0
                SETTINGS["main"][key] := raw + 0
            }
        }

        raw := Trim(RegExReplace(CompletionThreshold.Value, "%"))
        if (IsNumber(raw)) {
            v := Max(0.0, Min(100.0, raw + 0.0))
            MAIN["completion_threshold"] := v
            SETTINGS["main"]["completion_threshold"] := v
        }

        SaveSettingsFile()
        if (SETTINGS["last_config"] != "" && FileExist(CONFIGS_DIR "\" SETTINGS["last_config"] ".json"))
            SaveConfig(SETTINGS["last_config"])
        LoadAdvFields()
        SaveFishBtn.ctrl.Value := "Saved!"
        SetTimer(RevertFishBtn, -1500)
    }

    RevertFishBtn(*) {
        try SaveFishBtn.ctrl.Value := "Save"
    }

    RevertTotemBtn(*) {
        try SaveTotemBtn.ctrl.Value := "Save"
    }

    RevertDelaysBtn(*) {
        try SaveDelaysBtn.ctrl.Value := "Save"
    }

    LoadAdvFields()
    RefreshTotemDdl(MAIN["auto_totem_name"])

    SaveCastBtn.OnEvent("Click", SaveCastSettings)
    SaveFishBtn.OnEvent("Click", SaveFishSettings)
    SaveTotemBtn.OnEvent("Click", SaveTotemSettings)
    SaveDelaysBtn.OnEvent("Click", SaveTotemDelaySettings)

    mg.Show(GuiShowOpts)
    hwnd := mg.Hwnd
}
