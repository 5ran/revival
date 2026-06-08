#Requires AutoHotkey v2.0

CreateFishingMacro() {
    return {
        phase: "OFF",
        powerPercent: "",
        progressPercent: "",
        isHolding: false,
        isHoldingRight: false,
        castThreshold: 96.0,
        castWaitTimeoutMs: 15000,
        fishingEndGraceMs: 100,
        castStartedAt: 0,
        castReleasedAt: 0,
        castBarSeen: false,
        fishingLostAt: 0,
        doneAt: 0,
        completionReached: false,
        outcomeResolved: false,
        fishCaughtCount: 0,
        fishLostCount: 0,
        shakingIntervalMs: 25,
        lastShakedAt: 0,
        lastActionAt: 0,
        lastRightActionAt: 0,
        ActivatedUiNav: false,
        cycleEnabled: false,
        bellonaLeftCompletionReached: false,
        bellonaRightCompletionReached: false,
        totemState: "IDLE",
        totemRetryCount: 0,
        totemWaitStartedAt: 0,
        lastTotemSuccessAt: 0,
        lastTotemAttemptAt: 0,
        totemPending: false,
        totemBlockedUntilCatchEnd: false,
        totemNightCovered: false,
        totemNeedsRodReequip: false,
        totemNeedsSettleDelay: false
    }
}

ResolveCastThreshold() {
    global MAIN
    switch MAIN["cast_mode"] {
        case "short":  return 28.0
        case "custom": return Max(1.0, Min(100.0, MAIN["cast_power_custom"] + 0.0))
        default:       return 96.0
    }
}

InitializeCastCycle() {
    global Macro, MAIN

    if (!Macro.ActivatedUiNav) {
        SendInput("\")
        Macro.ActivatedUiNav := true
        Sleep(50)
    }

    Macro.powerPercent := ""
    Macro.progressPercent := ""
    Macro.isHolding := false
    Macro.isHoldingRight := false
    Macro.castStartedAt := A_TickCount
    Macro.castReleasedAt := 0
    Macro.castBarSeen := false
    Macro.fishingLostAt := 0
    Macro.doneAt := 0
    Macro.completionReached := false
    Macro.outcomeResolved := false
    Macro.bellonaLeftCompletionReached := false
    Macro.bellonaRightCompletionReached := false
    Macro.lastShakedAt := 0
    Macro.lastActionAt := 0
    Macro.lastRightActionAt := 0
    Macro.castThreshold := ResolveCastThreshold()
    Macro.castWaitTimeoutMs := MAIN["cast_timeout_ms"]
    Macro.fishingEndGraceMs := MAIN["fishing_end_grace_ms"]
    Macro.shakingIntervalMs := MAIN["shake_interval_ms"]
    Macro.phase := "CASTING"

    UpdateMacroStatus("CASTING", "---", "---")
}

CreateFishingControllerForCurrentRod() {
    global ROD

    rodText := GetHotbarRodDisplayText()
    if (rodText = "")
        rodText := ROD

    if (IsBellonaRodText(rodText))
        return BellonaController()

    if (IsPinionRodText(rodText))
        return PinionController()

    if (IsTranquilityRodText(rodText))
        return TranquilityController()

    return FishingController()
}

MacroLoop() {
    global Macro

    if (UpdateAutoTotem()) {
        UpdateMacroStatus(GetMacroDisplayStatus(), "---", "---")
        return
    }

    switch Macro.phase {
        case "CASTING":
            UpdateCastingPhase()
        case "CASTED":
            UpdateCastedPhase()
        case "SHAKE":
            UpdateShakePhase()
        case "FISHING":
            UpdateFishingPhase()
        case "DONE":
            if (Macro.cycleEnabled && IsPostCatchDelayElapsed())
                StartMacroCycle()
            else if (!Macro.cycleEnabled)
                StopMacroCycle("OFF")
        case "OFF":
    }

    UpdateMacroStatus(
        GetMacroDisplayStatus(),
        (Macro.powerPercent = "" ? "---" : Macro.powerPercent "%"),
        (Macro.progressPercent = "" ? "---" : Macro.progressPercent "%")
    )
}

StartMacroCycle() {
    global Macro, Controller, ROD

    if (Macro.phase = "OFF") {
        Macro.totemNightCovered := false
        Macro.totemPending := false
        Macro.totemBlockedUntilCatchEnd := false
    }

    Controller := CreateFishingControllerForCurrentRod()
    ReleaseAllFishingMouse(true)
    Controller.Reset()
    InitializeCastCycle()
}

StopMacroCycle(nextPhase := "OFF") {
    global Macro, Controller

    finalProgress := Macro.progressPercent

    ReleaseAllFishingMouse(true)
    Controller.Reset()

    Macro.powerPercent := ""
    Macro.isHolding := false
    Macro.isHoldingRight := false
    Macro.castStartedAt := 0
    Macro.castReleasedAt := 0
    Macro.castBarSeen := false
    Macro.progressPercent := ""
    Macro.fishingLostAt := 0
    Macro.doneAt := (nextPhase = "DONE") ? A_TickCount : 0
    Macro.completionReached := false
    Macro.outcomeResolved := false
    Macro.bellonaLeftCompletionReached := false
    Macro.bellonaRightCompletionReached := false
    Macro.lastShakedAt := 0
    Macro.lastActionAt := 0
    Macro.lastRightActionAt := 0
    Macro.phase := nextPhase

    if (nextPhase = "DONE")
        Macro.totemBlockedUntilCatchEnd := false
    else if (nextPhase = "OFF") {
        if (Macro.totemState != "IDLE" && Macro.totemNeedsRodReequip)
            SelectHotbarSlot("1")
        ResetAutoTotemControl()
        Macro.totemNightCovered := false
    }

    UpdateMacroStatus(
        GetMacroDisplayStatus(),
        "---",
        (nextPhase = "DONE" && finalProgress != "" ? finalProgress "%" : "---")
    )
}

GetMacroDisplayStatus() {
    global Macro
    return (Macro.totemState != "IDLE") ? Macro.totemState : Macro.phase
}

GetPostCatchDelayMs() {
    global MAIN
    return Max(0, MAIN["post_catch_delay_ms"] + 0)
}

IsPostCatchDelayElapsed() {
    global Macro
    return !Macro.doneAt || (A_TickCount - Macro.doneAt) >= GetPostCatchDelayMs()
}

ResetAutoTotemControl() {
    global Macro

    Macro.totemState := "IDLE"
    Macro.totemRetryCount := 0
    Macro.totemWaitStartedAt := 0
    Macro.totemPending := false
    Macro.totemBlockedUntilCatchEnd := false
    Macro.totemNeedsRodReequip := false
    Macro.totemNeedsSettleDelay := false
}

IsAutoTotemRuntimeEnabled() {
    global MAIN
    return MAIN["auto_totem_enabled"] && (MAIN["auto_totem_name"] = "Aurora Totem")
}

GetAutoTotemIntervalMs() {
    global MAIN
    return Max(1, MAIN["auto_totem_interval_sec"] + 0) * 1000
}

GetAutoTotemSettleMs() {
    return GetPostCatchDelayMs()
}

GetAutoTotemPostEquipMs() {
    global MAIN
    return Max(0, MAIN["post_totem_delay_ms"] + 0)
}

IsAutoTotemBoundary() {
    global Macro
    return (Macro.phase = "CASTING" && !IsAnyFishingMouseHeld() && !Macro.castBarSeen)
}

IsAutoTotemDue() {
    global MAIN, Macro

    if !IsAutoTotemRuntimeEnabled()
        return false

    if (MAIN["auto_totem_mode"] = "interval") {
        referenceAt := Macro.lastTotemSuccessAt
        if (Macro.lastTotemAttemptAt > referenceAt)
            referenceAt := Macro.lastTotemAttemptAt

        return (!referenceAt || (A_TickCount - referenceAt) >= GetAutoTotemIntervalMs())
    }

    if (Macro.totemNightCovered) {
        cycleText := StrLower(GetWorldStatusText("4_cycle"))
        if (cycleText = "" || InStr(cycleText, "night"))
            return false

        Macro.totemNightCovered := false
        AutoTotemDebugLog("night coverage expired; cycle left night")
        AutoTotemDebugProbe("expire probe")
    }

    return true
}

UpdateAutoTotem() {
    global Macro, Controller

    if !IsAutoTotemRuntimeEnabled() {
        if (Macro.totemState != "IDLE" || Macro.totemPending || Macro.totemBlockedUntilCatchEnd) {
            ReleaseAllFishingMouse(true)
            Controller.Reset()
            if (Macro.totemState != "IDLE" && Macro.totemNeedsRodReequip)
                SelectHotbarSlot("1")
            AutoTotemDebugLog("auto totem runtime disabled, clearing control")
            ResetAutoTotemControl()
        }
        return false
    }

    if (Macro.totemState != "IDLE") {
        Macro.powerPercent := ""
        Macro.progressPercent := ""
        ReleaseAllFishingMouse(true)
        Controller.Reset()
        UpdateAutoTotemState()
        return true
    }

    if !Macro.cycleEnabled
        return false

    if (Macro.totemPending && IsAutoTotemBoundary()) {
        AutoTotemDebugLog("pending auto totem resumed at safe boundary")
        BeginAutoTotemWorkflow()
        return true
    }

    if (Macro.totemBlockedUntilCatchEnd)
        return false

    if (IsAutoTotemDue()) {
        if (IsAutoTotemBoundary()) {
            AutoTotemDebugLog("auto totem due at safe boundary")
            BeginAutoTotemWorkflow()
            return true
        }

        if !Macro.totemPending {
            AutoTotemDebugLog("auto totem due during active cycle, deferring to boundary")
            if (Macro.phase != "OFF")
                Macro.totemNeedsSettleDelay := true
        }

        Macro.totemPending := true
    }

    return false
}

BeginAutoTotemWorkflow() {
    global Macro, Controller

    Macro.powerPercent := ""
    Macro.progressPercent := ""
    Macro.totemPending := false
    Macro.totemRetryCount := 0
    Macro.totemWaitStartedAt := 0
    Macro.lastTotemAttemptAt := A_TickCount
    Macro.totemNeedsRodReequip := false

    ReleaseAllFishingMouse(true)
    Controller.Reset()
    AutoTotemDebugLog("begin auto totem workflow")
    if (Macro.totemNeedsSettleDelay) {
        Macro.totemState := "TOTEM_SETTLE"
        Macro.totemWaitStartedAt := A_TickCount
        AutoTotemDebugLog("waiting post-catch input lock before totem use")
        return
    }

    RunAutoTotemWorkflowStep()
}

RunAutoTotemWorkflowStep() {
    global Macro

    AutoTotemDebugProbe("workflow branch probe")

    if (IsAuroraActive()) {
        AutoTotemDebugLog("aurora already active, completing workflow")
        CompleteAutoTotemWorkflow(true)
        return
    }

    if (IsNightCycle()) {
        AutoTotemDebugLog("night detected, using aurora totem")
        if (!TryUseAutoTotemItem("Aurora Totem")) {
            CompleteAutoTotemWorkflow(false)
            return
        }

        Macro.totemState := "TOTEM_WAIT_AURORA"
        Macro.totemWaitStartedAt := A_TickCount
        return
    }

    AutoTotemDebugLog("night not detected, using sundial totem")
    if (!TryUseAutoTotemItem("Sundial Totem")) {
        CompleteAutoTotemWorkflow(false)
        return
    }

    Macro.totemState := "TOTEM_WAIT_NIGHT"
    Macro.totemWaitStartedAt := A_TickCount
}

UpdateAutoTotemState() {
    global Macro

    if (IsAuroraActive()) {
        AutoTotemDebugLog("aurora detected while waiting")
        CompleteAutoTotemWorkflow(true)
        return
    }

    switch Macro.totemState {
        case "TOTEM_SETTLE":
            if ((A_TickCount - Macro.totemWaitStartedAt) < GetAutoTotemSettleMs())
                return

            Macro.totemNeedsSettleDelay := false
            Macro.totemWaitStartedAt := 0
            AutoTotemDebugLog("post-catch input lock cleared")
            RunAutoTotemWorkflowStep()
            return

        case "TOTEM_WAIT_NIGHT":
            if (IsNightCycle()) {
                Macro.totemRetryCount := 0
                AutoTotemDebugLog("night detected after sundial, using aurora totem")
                AutoTotemDebugProbe("night detected probe")

                if (!TryUseAutoTotemItem("Aurora Totem")) {
                    CompleteAutoTotemWorkflow(false)
                    return
                }

                Macro.totemState := "TOTEM_WAIT_AURORA"
                Macro.totemWaitStartedAt := A_TickCount
                return
            }

            if ((A_TickCount - Macro.totemWaitStartedAt) < GetAutoTotemWaitMs())
                return

            if (Macro.totemRetryCount >= 1) {
                AutoTotemDebugLog("night wait timed out after retry, failing workflow")
                AutoTotemDebugProbe("night timeout final probe")
                CompleteAutoTotemWorkflow(false)
                return
            }

            AutoTotemDebugLog("night wait timed out, retrying sundial use")
            AutoTotemDebugProbe("night timeout retry probe")
            if (!TryUseAutoTotemItem("Sundial Totem")) {
                CompleteAutoTotemWorkflow(false)
                return
            }

            Macro.totemRetryCount += 1
            Macro.totemWaitStartedAt := A_TickCount

        case "TOTEM_WAIT_AURORA":
            if ((A_TickCount - Macro.totemWaitStartedAt) < GetAutoTotemWaitMs())
                return

            if (Macro.totemRetryCount >= 1) {
                AutoTotemDebugLog("aurora wait timed out after retry, failing workflow")
                AutoTotemDebugProbe("aurora timeout final probe")
                CompleteAutoTotemWorkflow(false)
                return
            }

            AutoTotemDebugLog("aurora wait timed out, retrying aurora use")
            AutoTotemDebugProbe("aurora timeout retry probe")
            if (!TryUseAutoTotemItem("Aurora Totem")) {
                CompleteAutoTotemWorkflow(false)
                return
            }

            Macro.totemRetryCount += 1
            Macro.totemWaitStartedAt := A_TickCount
    }
}

TryUseAutoTotemItem(itemName) {
    global Macro

    if !TryUseHotbarItem(itemName)
        return false

    Macro.totemNeedsRodReequip := true
    return true
}

CompleteAutoTotemWorkflow(success := false) {
    global Macro, MAIN

    needsRodReequip := Macro.totemNeedsRodReequip
    AutoTotemDebugLog("complete auto totem workflow success=" success " reEquip=" needsRodReequip)

    if (success) {
        Macro.lastTotemSuccessAt := A_TickCount
        Macro.totemNightCovered := true
        AutoTotemDebugLog("marked current night as covered")
    }

    ResetAutoTotemControl()

    if (needsRodReequip) {
        EnsureRodEquipped()
        postEquipMs := GetAutoTotemPostEquipMs()
        if (postEquipMs > 0)
            Sleep(postEquipMs)
    }

    if (!success && MAIN["auto_totem_mode"] = "expire")
        Macro.totemBlockedUntilCatchEnd := true

    if (Macro.cycleEnabled && Macro.phase = "CASTING") {
        AutoTotemDebugLog("reinitializing cast cycle after auto totem")
        InitializeCastCycle()
    }
}

UpdateCastingPhase() {
    global Macro, MAIN

    Macro.progressPercent := ""

    if (MAIN["pre_cast_delay_ms"] > 0 && (A_TickCount - Macro.castStartedAt) < MAIN["pre_cast_delay_ms"])
        return

    HoldMouse()

    if (!Macro.castStartedAt)
        Macro.castStartedAt := A_TickCount

    resolved := ResolvePowerBarPath()
    if (!resolved.bar) {
        Macro.powerPercent := "---"

        if ((A_TickCount - Macro.castStartedAt) >= Macro.castWaitTimeoutMs)
            MAIN["cast_on_timeout"] ? StartMacroCycle() : StopMacroCycle("OFF")

        return
    }

    Macro.castBarSeen := true

    percent := ReadPowerBarPercent(resolved.bar)
    Macro.powerPercent := Format("{:.1f}", percent)

    if (percent >= Macro.castThreshold) {
        ReleaseMouse()
        Macro.castReleasedAt := A_TickCount
        Macro.phase := "CASTED"
        return
    }

    if ((A_TickCount - Macro.castStartedAt) >= Macro.castWaitTimeoutMs)
        MAIN["cast_on_timeout"] ? StartMacroCycle() : StopMacroCycle("OFF")
}

UpdateCastedPhase() {
    global Macro, MAIN

    Macro.powerPercent := ""
    Macro.progressPercent := ""
    ReleaseMouse()

    if (!Macro.castReleasedAt)
        Macro.castReleasedAt := A_TickCount

    if ((A_TickCount - Macro.castReleasedAt) < MAIN["post_cast_delay_ms"])
        return

    Macro.lastShakedAt := 0
    Macro.phase := "SHAKE"
}

UpdateShakePhase() {
    global Macro, Controller

    Macro.powerPercent := ""
    Macro.progressPercent := ""
    ReleaseAllFishingMouse(true)

    if (Controller.HasActiveContext()) {
        Macro.lastShakedAt := 0
        Macro.fishingLostAt := 0
        Macro.phase := "FISHING"
        return
    }

    if (!Macro.lastShakedAt || (A_TickCount - Macro.lastShakedAt) >= Macro.shakingIntervalMs) {
        SendInput("{Enter}")
        Macro.lastShakedAt := A_TickCount
    }

    if (Macro.castReleasedAt && (A_TickCount - Macro.castReleasedAt) >= Macro.castWaitTimeoutMs)
        StartMacroCycle()
}

UpdateFishingPhase() {
    global Macro, Controller, MAIN

    Macro.powerPercent := ""

    progress := Controller.GetProgressPercent()
    Macro.progressPercent := (progress = "" ? "" : Round(progress))

    Controller.UpdateCompletionState(MAIN["completion_threshold"] + 0.0)

    if (Controller.HasReelGui()) {
        Macro.fishingLostAt := 0

        if (Controller.HasActiveContext())
            Controller.Update()
        else
            Controller.Reset()
        return
    }

    ReleaseAllFishingMouse(true)
    Controller.Reset()

    if (!Macro.fishingLostAt)
        Macro.fishingLostAt := A_TickCount

    if ((A_TickCount - Macro.fishingLostAt) >= Macro.fishingEndGraceMs) {
        if (!Macro.outcomeResolved) {
            Macro.outcomeResolved := true
            if (Controller.IsCatchSuccessful())
                Macro.fishCaughtCount += 1
            else
                Macro.fishLostCount += 1
        }
        StopMacroCycle("DONE")
    }
}

HoldMouse() {
    HoldMouseButton("LButton")
}

ReleaseMouse(force := false) {
    ReleaseMouseButton("LButton", force)
}

HoldRightMouse() {
    HoldMouseButton("RButton")
}

ReleaseRightMouse(force := false) {
    ReleaseMouseButton("RButton", force)
}

ReleaseAllFishingMouse(force := false) {
    ReleaseMouseButton("LButton", force)
    ReleaseMouseButton("RButton", force)
}

IsAnyFishingMouseHeld() {
    global Macro
    return Macro.isHolding || Macro.isHoldingRight
}

HoldMouseButton(button) {
    global Macro, MAIN

    isRight := (button = "RButton")
    if (isRight ? Macro.isHoldingRight : Macro.isHolding)
        return

    delay := MAIN["fishing_action_delay_ms"] + 0
    lastActionAt := isRight ? Macro.lastRightActionAt : Macro.lastActionAt
    if (delay > 0 && lastActionAt && (A_TickCount - lastActionAt) < delay)
        return

    Send("{" button " down}")
    if (isRight) {
        Macro.isHoldingRight := true
        Macro.lastRightActionAt := A_TickCount
    } else {
        Macro.isHolding := true
        Macro.lastActionAt := A_TickCount
    }
}

ReleaseMouseButton(button, force := false) {
    global Macro, MAIN

    isRight := (button = "RButton")
    if (!(isRight ? Macro.isHoldingRight : Macro.isHolding))
        return

    delay := MAIN["fishing_action_delay_ms"] + 0
    lastActionAt := isRight ? Macro.lastRightActionAt : Macro.lastActionAt
    if (!force && delay > 0 && lastActionAt && (A_TickCount - lastActionAt) < delay)
        return

    Send("{" button " up}")
    if (isRight) {
        Macro.isHoldingRight := false
        Macro.lastRightActionAt := A_TickCount
    } else {
        Macro.isHolding := false
        Macro.lastActionAt := A_TickCount
    }
}

ReadFramePosition(frameAddr) {
    global OFFSETS

    base := OFFSETS["FramePositionX"] + 0
    scaleX := ReadFloat(frameAddr + base + 0x0)
    offsetX := ReadInt(frameAddr + base + 0x4)

    return {
        X: scaleX,
        XOffset: offsetX
    }
}

ReadFrameSize(frameAddr) {
    global OFFSETS

    base := OFFSETS["FrameSizeX"] + 0
    scaleX := ReadFloat(frameAddr + base + 0x0)
    offsetX := ReadInt(frameAddr + base + 0x4)

    return {
        X: scaleX,
        XOffset: offsetX
    }
}

GetReelGui() {
    playerGui := FindPlayerGui()
    if (!playerGui)
        return 0

    return FindChildByName(playerGui, "reel")
}

GetTranquilityGui() {
    playerGui := FindPlayerGui()
    if (!playerGui)
        return 0

    return FindChildByName(playerGui, "TranquilityRodRhythmGame")
}

GetTranquilityRoot(gui := 0) {
    gui := gui ? gui : GetTranquilityGui()
    return gui ? FindChildByName(gui, "RhythmGame") : 0
}

GetTranquilityLaneContainer(root := 0) {
    root := root ? root : GetTranquilityRoot()
    return root ? FindChildByName(root, "LaneContainer") : 0
}

GetTranquilityLane(index, container := 0) {
    container := container ? container : GetTranquilityLaneContainer()
    return container ? FindChildByName(container, "Lane" index) : 0
}

GetTranquilityHealthFill(root := 0) {
    root := root ? root : GetTranquilityRoot()
    if (!root)
        return 0

    healthBar := FindChildByName(root, "HealthBar")
    return healthBar ? FindChildByName(healthBar, "Fill") : 0
}

ReadTranquilityProgressPercent(root := 0) {
    fill := GetTranquilityHealthFill(root)
    if (!fill)
        return ""

    return ReadProgressBarPercent(fill)
}

ReadGuiObjectVisible(instanceAddr) {
    global OFFSETS

    if (!instanceAddr)
        return false

    className := ReadClassName(instanceAddr)
    if (className = "TextLabel" && OFFSETS.Has("TextLabelVisible"))
        return ReadByte(instanceAddr + (OFFSETS["TextLabelVisible"] + 0)) ? true : false

    if OFFSETS.Has("FrameVisible")
        return ReadByte(instanceAddr + (OFFSETS["FrameVisible"] + 0)) ? true : false

    return true
}

IsReasonableGuiScale(value) {
    return value > -5.0 && value < 5.0
}

GetTranquilityLaneKey(index, root := 0, lane := 0) {
    static fallbackKeys := Map(1, "A", 2, "S", 3, "D", 4, "F")

    root := root ? root : GetTranquilityRoot()
    label := root ? FindChildByName(root, "KeyLabel" index) : 0
    if (!label && lane)
        label := FindChildByName(lane, "KeyLabel")

    if (label) {
        keyText := Trim(ReadGuiText(label))
        if (StrLen(keyText) = 1)
            return StrUpper(keyText)
    }

    return fallbackKeys.Has(index) ? fallbackKeys[index] : ""
}

BuildReelContext(reelGui) {
    if (!reelGui)
        return 0

    barFrame := FindChildByName(reelGui, "bar")
    if (!barFrame)
        return 0

    progressFrame := FindChildByName(barFrame, "progress")
    progressBar := progressFrame ? FindChildByName(progressFrame, "bar") : 0
    barPos := ReadFramePosition(barFrame)

    return {
        reel: reelGui,
        bar: barFrame,
        fish: FindChildByName(barFrame, "fish"),
        playerbar: FindChildByName(barFrame, "playerbar"),
        progress: progressFrame,
        progressBar: progressBar,
        barX: barPos.X
    }
}

GetReelBarContext(reelGui := 0) {
    reelGui := reelGui ? reelGui : GetReelGui()
    return BuildReelContext(reelGui)
}

GetReelContexts() {
    playerGui := FindPlayerGui()
    contexts := []
    if (!playerGui)
        return contexts

    for child in ReadChildren(playerGui) {
        if (ReadInstanceName(child) != "reel" || ReadClassName(child) != "ScreenGui")
            continue

        ctx := BuildReelContext(child)
        if (ctx)
            contexts.Push(ctx)
    }

    SortReelContextsByBarX(contexts)
    return contexts
}

SortReelContextsByBarX(contexts) {
    i := 1
    while (i <= contexts.Length) {
        j := i + 1
        while (j <= contexts.Length) {
            if (contexts[j].barX < contexts[i].barX) {
                tmp := contexts[i]
                contexts[i] := contexts[j]
                contexts[j] := tmp
            }
            j += 1
        }
        i += 1
    }
}

HasActiveFishingContext() {
    ctx := GetReelBarContext()
    return (ctx && ctx.fish && ctx.playerbar) ? true : false
}

GetReelProgressContext(reelGui := 0) {
    ctx := GetReelBarContext(reelGui)
    return (ctx && ctx.progressBar) ? ctx : 0
}

ReadProgressBarPercent(frameAddr) {
    size := ReadFrameSize(frameAddr)
    return Max(0.0, Min(100.0, size.X * 100.0))
}

GetFishingCompletionPercent(ctx := 0) {
    if (!ctx)
        ctx := GetReelProgressContext()

    if (!ctx || !ctx.progressBar)
        return ""

    return ReadProgressBarPercent(ctx.progressBar)
}

IsFishingCompletionReached(threshold := 99.7) {
    percent := GetFishingCompletionPercent()
    return (percent != "" && percent >= threshold)
}

IsIndicatorSafe(ctx := 0) {
    if (!ctx)
        ctx := GetReelBarContext()

    if (!ctx || !ctx.playerbar || !ctx.fish)
        return ""

    playerbarPos := ReadFramePosition(ctx.playerbar)
    playerbarSize := ReadFrameSize(ctx.playerbar)
    fishPos := ReadFramePosition(ctx.fish)
    fishSize := ReadFrameSize(ctx.fish)

    fishCenter := fishPos.X + (fishSize.X / 2)

    halfWidth := playerbarSize.X / 2
    safeZoneLeft := playerbarPos.X - halfWidth
    safeZoneRight := playerbarPos.X + halfWidth

    return (fishCenter >= safeZoneLeft && fishCenter <= safeZoneRight)
}

ReadPlayerbarWidth(ctx := 0) {
    if (!ctx)
        ctx := GetReelBarContext()

    if (!ctx || !ctx.playerbar)
        return ""

    playerbarSize := ReadFrameSize(ctx.playerbar)
    return (playerbarSize.X > 0.0 && playerbarSize.X <= 1.0) ? playerbarSize.X : ""
}

ResolvePowerBarPath() {
    workspace := GetWorkspaceRoot()
    if (!workspace)
        return { bar: 0 }

    localPlayer := GetLocalPlayer()
    if (!localPlayer)
        return { bar: 0 }

    playerName := ReadInstanceName(localPlayer)
    if (playerName = "" || playerName = "<null>")
        return { bar: 0 }

    character := FindChildByName(workspace, playerName)
    if (!character)
        return { bar: 0 }

    rootPart := FindChildByName(character, "HumanoidRootPart")
    if (!rootPart)
        return { bar: 0 }

    powerGui := FindChildByName(rootPart, "power")
    if (!powerGui)
        return { bar: 0 }

    bar := FindDescendantFrameByName(powerGui, "bar")
    if (!bar)
        return { bar: 0 }

    return { bar: bar }
}

ReadPowerBarPercent(instanceAddr) {
    global OFFSETS

    base := OFFSETS["FrameSizeX"] + 0
    scaleY := ReadFloat(instanceAddr + base + 0x8)
    percent := scaleY * 100.0

    return Max(0.0, Min(100.0, percent))
}

FindDescendantFrameByName(rootAddr, targetName) {
    queue := [rootAddr]
    index := 1

    while (index <= queue.Length) {
        current := queue[index]
        index += 1

        if (ReadInstanceName(current) = targetName && ReadClassName(current) = "Frame")
            return current

        for childPtr in ReadChildren(current)
            queue.Push(childPtr)
    }

    return 0
}

ReadNotePosition(frameAddr) {
    global OFFSETS
    base := OFFSETS["FramePositionX"] + 0
    return {
        sx: ReadFloat(frameAddr + base + 0x0),
        ox: ReadInt(frameAddr + base + 0x4),
        sy: ReadFloat(frameAddr + base + 0x8),
        oy: ReadInt(frameAddr + base + 0xC)
    }
}

GetNoteContainer(ctx := 0) {
    barFrame := ctx ? ctx.bar : 0
    if (!barFrame) {
        reelGui := GetReelGui()
        if (!reelGui)
            return 0
        barFrame := FindChildByName(reelGui, "bar")
    }

    if (!barFrame)
        return 0

    return FindChildByName(barFrame, "noteContainer")
}

; Returns the nearest airborne note to the bar's current X, or "" if none.
; Notes are always the priority — fish tracking only happens during gaps
; between notes. Picking the nearest one keeps the bar from abandoning a
; close note for a more distant one.
GetActiveNoteTarget(playerbarX, ctx := 0) {
    noteContainer := GetNoteContainer(ctx)
    if (!noteContainer)
        return ""

    best := ""
    bestDist := 99999.0

    for noteName in ["note1", "note2"] {
        noteAddr := FindChildByName(noteContainer, noteName)
        if (!noteAddr)
            continue
        pos := ReadNotePosition(noteAddr)
        if (pos.sy > 0.55 || pos.sy < -30)
            continue

        dist := Abs(pos.sx - playerbarX)
        if (dist < bestDist) {
            bestDist := dist
            best := { sx: pos.sx, sy: pos.sy }
        }
    }

    return best
}

class FishingController {
    __New(button := "LButton") {
        this.button := button
        this.ctx := 0
    }

    SetContext(ctx) {
        this.ctx := ctx
    }

    GetContext() {
        return this.ctx ? this.ctx : GetReelBarContext()
    }

    Reset() {
        this.Release(true)
        for _, propName in ["lastPlayerbarPos", "lastFishPos", "pwmAccumulator"] {
            if (this.HasOwnProp(propName))
                this.DeleteProp(propName)
        }
    }

    HasReelGui() {
        return GetReelGui() ? true : false
    }

    HasActiveContext() {
        ctx := this.GetContext()
        return (ctx && ctx.fish && ctx.playerbar) ? true : false
    }

    GetProgressPercent() {
        return GetFishingCompletionPercent(this.GetContext())
    }

    UpdateCompletionState(threshold) {
        global Macro
        progress := this.GetProgressPercent()
        if (progress != "" && progress >= threshold)
            Macro.completionReached := true
    }

    IsCatchSuccessful() {
        global Macro
        return Macro.completionReached
    }

    GetRuntimeTuning(ctx) {
        global MAIN

        width := ReadPlayerbarWidth(ctx)
        shrinkRatio := 1.0
        halfWidth := ""

        if (width != "") {
            if (!this.HasOwnProp("baselinePlayerbarWidth") || width > this.baselinePlayerbarWidth)
                this.baselinePlayerbarWidth := width

            if (this.baselinePlayerbarWidth > 0)
                shrinkRatio := Max(0.35, Min(1.0, width / this.baselinePlayerbarWidth))

            halfWidth := width / 2.0
        }

        shrinkAmount := 1.0 - shrinkRatio
        closeThreshold := MAIN["close_threshold"] * shrinkRatio
        if (halfWidth != "")
            closeThreshold := Min(closeThreshold, halfWidth * 0.70)

        return {
            closeThreshold: Max(0.004, closeThreshold),
            edgeBoundary: Max(0.02, MAIN["edge_boundary"] * shrinkRatio),
            velocityDamping: MAIN["velocity_damping"] * (1.0 + (shrinkAmount * 0.85)),
            brakeLookaheadFactor: 8.0 * (1.0 + (shrinkAmount * 0.75))
        }
    }

    Update() {
        ctx := this.GetContext()
        isSafe := IsIndicatorSafe(ctx)
        if (isSafe = "") {
            this.Release()
            return
        }

        fishPos := this.GetFishPosition(ctx)
        playerbarPos := this.GetPlayerbarPosition(ctx)

        if (fishPos = "" || playerbarPos = "")
            return

        if (!this.HasOwnProp("lastPlayerbarPos"))
            this.lastPlayerbarPos := playerbarPos

        if (!this.HasOwnProp("lastFishPos"))
            this.lastFishPos := fishPos

        playerbarVelocity := playerbarPos - this.lastPlayerbarPos
        this.lastPlayerbarPos := playerbarPos

        fishVelocity := fishPos - this.lastFishPos
        this.lastFishPos := fishPos

        error := fishPos - playerbarPos

        tuning := this.GetRuntimeTuning(ctx)

        edgeBoundary := tuning.edgeBoundary
        if (playerbarPos < edgeBoundary) {
            this.Hold()
            return
        }
        if (playerbarPos > 1 - edgeBoundary) {
            this.Release()
            return
        }

        predictionScale := MAIN["prediction_strength"] * (1.0 - MAIN["resilience"])
        predicted := playerbarPos + (playerbarVelocity * predictionScale)
        predictedError := fishPos - predicted

        closeThreshold := tuning.closeThreshold
        sameSideAfterPrediction := (error * predictedError) > 0

        approachingTarget := (error * playerbarVelocity) > 0
        remainingDistance := Max(0.0, Abs(error) - closeThreshold)

        ; full stop fixing and start bleeding speed early
        brakeLookahead := Abs(playerbarVelocity) * tuning.brakeLookaheadFactor
        needsPreSlow := approachingTarget && (brakeLookahead >= remainingDistance)

        ; hard fix only when far enough and not yet in the braking zone
        if (Abs(error) > closeThreshold && sameSideAfterPrediction && !needsPreSlow) {
            if (error > 0)
                this.Hold()
            else
                this.Release()
            return
        }

        neutralDuty := MAIN["neutral_duty_cycle"]

        if (needsPreSlow && brakeLookahead > 0) {
            brakeUrgency := 1.0 - Min(1.0, remainingDistance / brakeLookahead)

            if (error > 0) {
                targetDuty := neutralDuty * (1.0 - brakeUrgency)
            } else {
                targetDuty := neutralDuty + ((1.0 - neutralDuty) * brakeUrgency)
            }
        } else {
            ; Normal pwm balancing // fine tracking
            kP := MAIN["proportional_gain"]
            kD := MAIN["derivative_gain"]
            kV := tuning.velocityDamping

            adjustment := (kP * error) + (kD * fishVelocity) - (kV * playerbarVelocity)
            targetDuty := Max(0.0, Min(1.0, neutralDuty + adjustment))
        }

        if (!this.HasOwnProp("pwmAccumulator"))
            this.pwmAccumulator := 0.0

        this.pwmAccumulator += targetDuty
        if (this.pwmAccumulator >= 1.0) {
            this.pwmAccumulator -= 1.0
            this.Hold()
        } else {
            this.Release()
        }
    }

    GetFishPosition(ctx := 0) {
        if (!ctx)
            ctx := this.GetContext()
        if (!ctx || !ctx.fish)
            return ""

        fishPos := ReadFramePosition(ctx.fish)
        fishSize := ReadFrameSize(ctx.fish)
        return fishPos.X + (fishSize.X / 2)
    }

    GetPlayerbarPosition(ctx := 0) {
        if (!ctx)
            ctx := this.GetContext()
        if (!ctx || !ctx.playerbar)
            return ""

        playerbarPos := ReadFramePosition(ctx.playerbar)
        return playerbarPos.X
    }

    Hold() {
        HoldMouseButton(this.button)
    }

    Release(force := false) {
        ReleaseMouseButton(this.button, force)
    }
}

class PinionController extends FishingController {
    static NOTE_MODE_ENTRY := 27.0
    static NOTE_MODE_EXIT  := 20.0

    pinionNoteModeActive := false

    Reset() {
        super.Reset()
        this.pinionNoteModeActive := false
    }

    GetFishPosition(ctx := 0) {
        if (!ctx)
            ctx := this.GetContext()

        fishX := super.GetFishPosition(ctx)
        playerbarX := this.GetPlayerbarPosition(ctx)

        if (playerbarX = "")
            return fishX

        progress := GetFishingCompletionPercent(ctx)

        ; Hysteresis gate: must reach entry threshold to start note mode,
        ; stays active until progress falls below the lower exit threshold.
        if (progress = "") {
            this.pinionNoteModeActive := false
            return fishX
        }

        if (this.pinionNoteModeActive) {
            if (progress < PinionController.NOTE_MODE_EXIT) {
                this.pinionNoteModeActive := false
                return fishX
            }
        } else {
            if (progress < PinionController.NOTE_MODE_ENTRY)
                return fishX
            this.pinionNoteModeActive := true
        }

        note := GetActiveNoteTarget(playerbarX, ctx)
        if (note = "")
            return fishX

        t := Min(1.0, (progress - PinionController.NOTE_MODE_ENTRY) / 28.0)
        maxReach := 0.1 + (t * 0.9)
        if (Abs(note.sx - playerbarX) > maxReach)
            return fishX

        return note.sx
    }
}

class TranquilityController {
    static HIT_Y_MIN := 0.78
    static HIT_Y_MAX := 0.90
    static KEY_COOLDOWN_MS := 30

    __New() {
        this.hitNotes := Map()
        this.lastKeySentAt := Map()
    }

    Reset() {
        ReleaseAllFishingMouse(true)
        this.hitNotes := Map()
        this.lastKeySentAt := Map()
    }

    HasReelGui() {
        return GetTranquilityGui() ? true : false
    }

    HasActiveContext() {
        return GetTranquilityLaneContainer() ? true : false
    }

    GetProgressPercent() {
        return ReadTranquilityProgressPercent()
    }

    UpdateCompletionState(threshold) {
        global Macro

        progress := this.GetProgressPercent()
        if (progress != "" && progress >= threshold)
            Macro.completionReached := true
    }

    IsCatchSuccessful() {
        global Macro
        return Macro.completionReached
    }

    Update() {
        ReleaseAllFishingMouse(true)

        root := GetTranquilityRoot()
        if (!root)
            return

        container := GetTranquilityLaneContainer(root)
        if (!container)
            return

        seenNotes := Map()

        Loop 4 {
            lane := GetTranquilityLane(A_Index, container)
            if (!lane || !ReadGuiObjectVisible(lane))
                continue

            key := GetTranquilityLaneKey(A_Index, root, lane)
            if (key = "")
                continue

            for noteAddr in ReadChildren(lane) {
                if (ReadInstanceName(noteAddr) != "Note" || ReadClassName(noteAddr) != "ImageLabel")
                    continue

                seenNotes[noteAddr] := true
                if (this.hitNotes.Has(noteAddr) || !ReadGuiObjectVisible(noteAddr))
                    continue

                pos := ReadNotePosition(noteAddr)
                if (!IsReasonableGuiScale(pos.sy))
                    continue

                if (pos.sy >= TranquilityController.HIT_Y_MIN && pos.sy <= TranquilityController.HIT_Y_MAX)
                    this.PressLaneKey(key, noteAddr)
            }
        }

        staleNotes := []
        for noteAddr, _ in this.hitNotes {
            if (!seenNotes.Has(noteAddr))
                staleNotes.Push(noteAddr)
        }

        for _, noteAddr in staleNotes
            this.hitNotes.Delete(noteAddr)
    }

    PressLaneKey(key, noteAddr) {
        now := A_TickCount
        lastSentAt := this.lastKeySentAt.Has(key) ? this.lastKeySentAt[key] : 0
        if (lastSentAt && (now - lastSentAt) < TranquilityController.KEY_COOLDOWN_MS)
            return false

        SendInput("{" key "}")
        this.lastKeySentAt[key] := now
        this.hitNotes[noteAddr] := now
        return true
    }
}

class BellonaController {
    __New() {
        this.left := FishingController("LButton")
        this.right := FishingController("RButton")
    }

    Reset() {
        this.left.Reset()
        this.right.Reset()
    }

    GetContexts() {
        return GetReelContexts()
    }

    GetSideContexts() {
        contexts := this.GetContexts()
        leftCtx := 0
        rightCtx := 0

        if (contexts.Length >= 2) {
            leftCtx := contexts[1]
            rightCtx := contexts[contexts.Length]
        } else if (contexts.Length = 1) {
            if (contexts[1].barX < 0.5)
                leftCtx := contexts[1]
            else
                rightCtx := contexts[1]
        }

        return { left: leftCtx, right: rightCtx }
    }

    HasReelGui() {
        return this.GetContexts().Length > 0
    }

    HasActiveContext() {
        for ctx in this.GetContexts() {
            if (ctx && ctx.fish && ctx.playerbar)
                return true
        }
        return false
    }

    GetProgressPercent() {
        progressValues := []
        for ctx in this.GetContexts() {
            progress := GetFishingCompletionPercent(ctx)
            if (progress != "")
                progressValues.Push(progress)
        }

        if (!progressValues.Length)
            return ""

        minProgress := progressValues[1]
        for progress in progressValues {
            if (progress < minProgress)
                minProgress := progress
        }
        return minProgress
    }

    UpdateCompletionState(threshold) {
        global Macro

        sides := this.GetSideContexts()
        if (sides.left) {
            leftProgress := GetFishingCompletionPercent(sides.left)
            if (leftProgress != "" && leftProgress >= threshold)
                Macro.bellonaLeftCompletionReached := true
        }

        if (sides.right) {
            rightProgress := GetFishingCompletionPercent(sides.right)
            if (rightProgress != "" && rightProgress >= threshold)
                Macro.bellonaRightCompletionReached := true
        }

        Macro.completionReached := Macro.bellonaLeftCompletionReached && Macro.bellonaRightCompletionReached
    }

    IsCatchSuccessful() {
        global Macro
        return Macro.bellonaLeftCompletionReached && Macro.bellonaRightCompletionReached
    }

    Update() {
        sides := this.GetSideContexts()

        if (sides.left && sides.left.fish && sides.left.playerbar) {
            this.left.SetContext(sides.left)
            this.left.Update()
        } else {
            this.left.Reset()
        }

        if (sides.right && sides.right.fish && sides.right.playerbar) {
            this.right.SetContext(sides.right)
            this.right.Update()
        } else {
            this.right.Reset()
        }
    }
}
