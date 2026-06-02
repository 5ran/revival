#include "MacroController.hpp"

#include <QVariantMap>

#define SET_IF_CHANGED(member, value) \
    do {                              \
        if ((member) == (value)) {    \
            return;                   \
        }                             \
        (member) = (value);           \
        emit stateChanged();          \
    } while (0)

MacroController::MacroController(QObject* parent)
    : QObject(parent),
      fishingStatusItems_(buildDefaultStatusItems()),
      fishingStatsItems_(buildDefaultStatsItems()),
      mutationOptions_(buildDefaultMutations()),
      availableHuntTargets_(buildDefaultHuntTargets()) {
    runtimeTimer_.setInterval(200);
    connect(&runtimeTimer_, &QTimer::timeout, this, [this]() {
        refreshRuntimeText();
    });
}

bool MacroController::macroRunning() const { return macroRunning_; }
void MacroController::setMacroRunning(bool value) {
    if (macroRunning_ == value) return;

    macroRunning_ = value;
    if (macroRunning_) {
        startRuntimeClock();
    } else {
        stopRuntimeClock();
    }

    emit stateChanged();
}
QString MacroController::activeMacroText() const { return activeMacroText_; }
void MacroController::setActiveMacroText(const QString& value) { SET_IF_CHANGED(activeMacroText_, value); }
QString MacroController::runStateText() const { return runStateText_; }
void MacroController::setRunStateText(const QString& value) { SET_IF_CHANGED(runStateText_, value); }
QString MacroController::runTimeText() const { return runTimeText_; }
QString MacroController::hotkeyText() const { return hotkeyText_; }
void MacroController::setHotkeyText(const QString& value) { SET_IF_CHANGED(hotkeyText_, value); }
QString MacroController::selectedModule() const { return selectedModule_; }
void MacroController::setSelectedModule(const QString& value) { SET_IF_CHANGED(selectedModule_, value); }
QStringList MacroController::activityFeed() const { return activityFeed_; }
int MacroController::detailsRequestToken() const { return detailsRequestToken_; }
QStringList MacroController::rodSlots() const { return rodSlots_; }
QString MacroController::selectedRodSlot() const { return selectedRodSlot_; }
void MacroController::setSelectedRodSlot(const QString& value) { SET_IF_CHANGED(selectedRodSlot_, value); }

QStringList MacroController::trackerOptions() const { return trackerOptions_; }
QString MacroController::selectedTracker() const { return selectedTracker_; }
void MacroController::setSelectedTracker(const QString& value) { SET_IF_CHANGED(selectedTracker_, value); }
QStringList MacroController::castingModes() const { return castingModes_; }
QString MacroController::selectedCastingMode() const { return selectedCastingMode_; }
void MacroController::setSelectedCastingMode(const QString& value) { SET_IF_CHANGED(selectedCastingMode_, value); }
QVariantList MacroController::fishingStatusItems() const { return fishingStatusItems_; }
QVariantList MacroController::fishingStatsItems() const { return fishingStatsItems_; }
bool MacroController::masterlineEquipped() const { return masterlineEquipped_; }
void MacroController::setMasterlineEquipped(bool value) { SET_IF_CHANGED(masterlineEquipped_, value); }
QString MacroController::masterlineRodsText() const { return masterlineRodsText_; }
void MacroController::setMasterlineRodsText(const QString& value) { SET_IF_CHANGED(masterlineRodsText_, value); }

QString MacroController::activeAddonTitle() const { return activeAddonTitle_; }
void MacroController::setActiveAddonTitle(const QString& value) { SET_IF_CHANGED(activeAddonTitle_, value); }
bool MacroController::autoAquariumEnabled() const { return autoAquariumEnabled_; }
void MacroController::setAutoAquariumEnabled(bool value) { SET_IF_CHANGED(autoAquariumEnabled_, value); }
bool MacroController::autoTotemEnabled() const { return autoTotemEnabled_; }
void MacroController::setAutoTotemEnabled(bool value) { SET_IF_CHANGED(autoTotemEnabled_, value); }
bool MacroController::autoSovEnabled() const { return autoSovEnabled_; }
void MacroController::setAutoSovEnabled(bool value) { SET_IF_CHANGED(autoSovEnabled_, value); }
bool MacroController::huntDetectEnabled() const { return huntDetectEnabled_; }
void MacroController::setHuntDetectEnabled(bool value) { SET_IF_CHANGED(huntDetectEnabled_, value); }
bool MacroController::autoAquariumExpanded() const { return autoAquariumExpanded_; }
void MacroController::setAutoAquariumExpanded(bool value) { SET_IF_CHANGED(autoAquariumExpanded_, value); }
bool MacroController::autoTotemExpanded() const { return autoTotemExpanded_; }
void MacroController::setAutoTotemExpanded(bool value) { SET_IF_CHANGED(autoTotemExpanded_, value); }
bool MacroController::autoSovExpanded() const { return autoSovExpanded_; }
void MacroController::setAutoSovExpanded(bool value) { SET_IF_CHANGED(autoSovExpanded_, value); }
bool MacroController::huntDetectExpanded() const { return huntDetectExpanded_; }
void MacroController::setHuntDetectExpanded(bool value) { SET_IF_CHANGED(huntDetectExpanded_, value); }
double MacroController::aquariumCycleDelayMinutes() const { return aquariumCycleDelayMinutes_; }
void MacroController::setAquariumCycleDelayMinutes(double value) { SET_IF_CHANGED(aquariumCycleDelayMinutes_, value); }

QString MacroController::activeAutomationTitle() const { return activeAutomationTitle_; }
void MacroController::setActiveAutomationTitle(const QString& value) { SET_IF_CHANGED(activeAutomationTitle_, value); }
QString MacroController::selectedAutomation() const { return selectedAutomation_; }
void MacroController::setSelectedAutomation(const QString& value) { SET_IF_CHANGED(selectedAutomation_, value); }
bool MacroController::autoAnglerEnabled() const { return autoAnglerEnabled_; }
void MacroController::setAutoAnglerEnabled(bool value) { SET_IF_CHANGED(autoAnglerEnabled_, value); }
bool MacroController::enchantEnabled() const { return enchantEnabled_; }
void MacroController::setEnchantEnabled(bool value) { SET_IF_CHANGED(enchantEnabled_, value); }
bool MacroController::appraiseEnabled() const { return appraiseEnabled_; }
void MacroController::setAppraiseEnabled(bool value) { SET_IF_CHANGED(appraiseEnabled_, value); }
bool MacroController::treasureAppraiseEnabled() const { return treasureAppraiseEnabled_; }
void MacroController::setTreasureAppraiseEnabled(bool value) { SET_IF_CHANGED(treasureAppraiseEnabled_, value); }

QString MacroController::currentFishText() const { return currentFishText_; }
void MacroController::setCurrentFishText(const QString& value) { SET_IF_CHANGED(currentFishText_, value); }
QString MacroController::autoAnglerStatusText() const { return autoAnglerStatusText_; }
void MacroController::setAutoAnglerStatusText(const QString& value) { SET_IF_CHANGED(autoAnglerStatusText_, value); }

bool MacroController::autoEnchantEnabled() const { return autoEnchantEnabled_; }
void MacroController::setAutoEnchantEnabled(bool value) { SET_IF_CHANGED(autoEnchantEnabled_, value); }
QString MacroController::enchantMode() const { return enchantMode_; }
void MacroController::setEnchantMode(const QString& value) { SET_IF_CHANGED(enchantMode_, value); }
QString MacroController::targetSearchText() const { return targetSearchText_; }
void MacroController::setTargetSearchText(const QString& value) { SET_IF_CHANGED(targetSearchText_, value); }
QStringList MacroController::targetEnchants() const { return targetEnchants_; }
QString MacroController::selectedTargetEnchant() const { return selectedTargetEnchant_; }
void MacroController::setSelectedTargetEnchant(const QString& value) { SET_IF_CHANGED(selectedTargetEnchant_, value); }
QString MacroController::newEnchantText() const { return newEnchantText_; }
void MacroController::setNewEnchantText(const QString& value) { SET_IF_CHANGED(newEnchantText_, value); }
QString MacroController::enchantRodText() const { return enchantRodText_; }
void MacroController::setEnchantRodText(const QString& value) { SET_IF_CHANGED(enchantRodText_, value); }
QString MacroController::currentEnchantText() const { return currentEnchantText_; }
void MacroController::setCurrentEnchantText(const QString& value) { SET_IF_CHANGED(currentEnchantText_, value); }
QString MacroController::enchantStatusText() const { return enchantStatusText_; }
void MacroController::setEnchantStatusText(const QString& value) { SET_IF_CHANGED(enchantStatusText_, value); }

bool MacroController::autoAppraiseEnabled() const { return autoAppraiseEnabled_; }
void MacroController::setAutoAppraiseEnabled(bool value) { SET_IF_CHANGED(autoAppraiseEnabled_, value); }
QString MacroController::appraiseMode() const { return appraiseMode_; }
void MacroController::setAppraiseMode(const QString& value) { SET_IF_CHANGED(appraiseMode_, value); }
double MacroController::gamepassSpeed() const { return gamepassSpeed_; }
void MacroController::setGamepassSpeed(double value) { SET_IF_CHANGED(gamepassSpeed_, value); }
bool MacroController::requireShiny() const { return requireShiny_; }
void MacroController::setRequireShiny(bool value) { SET_IF_CHANGED(requireShiny_, value); }
bool MacroController::requireSparkling() const { return requireSparkling_; }
void MacroController::setRequireSparkling(bool value) { SET_IF_CHANGED(requireSparkling_, value); }
bool MacroController::requireTiny() const { return requireTiny_; }
void MacroController::setRequireTiny(bool value) { SET_IF_CHANGED(requireTiny_, value); }
bool MacroController::requireSmall() const { return requireSmall_; }
void MacroController::setRequireSmall(bool value) { SET_IF_CHANGED(requireSmall_, value); }
bool MacroController::requireBig() const { return requireBig_; }
void MacroController::setRequireBig(bool value) { SET_IF_CHANGED(requireBig_, value); }
bool MacroController::requireGiant() const { return requireGiant_; }
void MacroController::setRequireGiant(bool value) { SET_IF_CHANGED(requireGiant_, value); }
QString MacroController::mutationSearchText() const { return mutationSearchText_; }
void MacroController::setMutationSearchText(const QString& value) { SET_IF_CHANGED(mutationSearchText_, value); }
QVariantList MacroController::mutationOptions() const { return mutationOptions_; }

bool MacroController::autoTreasureEnabled() const { return autoTreasureEnabled_; }
void MacroController::setAutoTreasureEnabled(bool value) { SET_IF_CHANGED(autoTreasureEnabled_, value); }
double MacroController::treasureClickDelaySeconds() const { return treasureClickDelaySeconds_; }
void MacroController::setTreasureClickDelaySeconds(double value) { SET_IF_CHANGED(treasureClickDelaySeconds_, value); }
double MacroController::treasureMinimumMulti() const { return treasureMinimumMulti_; }
void MacroController::setTreasureMinimumMulti(double value) { SET_IF_CHANGED(treasureMinimumMulti_, value); }
QString MacroController::treasureStatusText() const { return treasureStatusText_; }
void MacroController::setTreasureStatusText(const QString& value) { SET_IF_CHANGED(treasureStatusText_, value); }

double MacroController::sovMinPercent() const { return sovMinPercent_; }
void MacroController::setSovMinPercent(double value) { SET_IF_CHANGED(sovMinPercent_, value); }
double MacroController::sovMaxPercent() const { return sovMaxPercent_; }
void MacroController::setSovMaxPercent(double value) { SET_IF_CHANGED(sovMaxPercent_, value); }
QString MacroController::sovStatusText() const { return sovStatusText_; }
void MacroController::setSovStatusText(const QString& value) { SET_IF_CHANGED(sovStatusText_, value); }

QString MacroController::discordWebhook() const { return discordWebhook_; }
void MacroController::setDiscordWebhook(const QString& value) { SET_IF_CHANGED(discordWebhook_, value); }
QString MacroController::newHuntTarget() const { return newHuntTarget_; }
void MacroController::setNewHuntTarget(const QString& value) { SET_IF_CHANGED(newHuntTarget_, value); }
QString MacroController::huntSearchText() const { return huntSearchText_; }
void MacroController::setHuntSearchText(const QString& value) { SET_IF_CHANGED(huntSearchText_, value); }
QVariantList MacroController::availableHuntTargets() const { return availableHuntTargets_; }
QVariantList MacroController::selectedHuntTargets() const { return selectedHuntTargets_; }

QStringList MacroController::totemOptions() const { return totemOptions_; }
QString MacroController::selectedTotem() const { return selectedTotem_; }
void MacroController::setSelectedTotem(const QString& value) { SET_IF_CHANGED(selectedTotem_, value); }
bool MacroController::useShinyTotem() const { return useShinyTotem_; }
void MacroController::setUseShinyTotem(bool value) { SET_IF_CHANGED(useShinyTotem_, value); }
bool MacroController::useSparklingTotem() const { return useSparklingTotem_; }
void MacroController::setUseSparklingTotem(bool value) { SET_IF_CHANGED(useSparklingTotem_, value); }
bool MacroController::useMutationTotem() const { return useMutationTotem_; }
void MacroController::setUseMutationTotem(bool value) { SET_IF_CHANGED(useMutationTotem_, value); }
bool MacroController::stayDay() const { return stayDay_; }
void MacroController::setStayDay(bool value) { SET_IF_CHANGED(stayDay_, value); }
bool MacroController::stayNight() const { return stayNight_; }
void MacroController::setStayNight(bool value) { SET_IF_CHANGED(stayNight_, value); }
bool MacroController::timePreferenceEditable() const { return timePreferenceEditable_; }
void MacroController::setTimePreferenceEditable(bool value) { SET_IF_CHANGED(timePreferenceEditable_, value); }
QString MacroController::totemStatusText() const { return totemStatusText_; }
void MacroController::setTotemStatusText(const QString& value) { SET_IF_CHANGED(totemStatusText_, value); }

QString MacroController::compactMacroName() const { return compactMacroName_; }
void MacroController::setCompactMacroName(const QString& value) { SET_IF_CHANGED(compactMacroName_, value); }
QString MacroController::compactPhase() const { return compactPhase_; }
void MacroController::setCompactPhase(const QString& value) { SET_IF_CHANGED(compactPhase_, value); }
QString MacroController::compactRuntime() const { return compactRuntime_; }
void MacroController::setCompactRuntime(const QString& value) { SET_IF_CHANGED(compactRuntime_, value); }
QString MacroController::compactStatusMessage() const { return compactStatusMessage_; }
void MacroController::setCompactStatusMessage(const QString& value) { SET_IF_CHANGED(compactStatusMessage_, value); }
double MacroController::playerbarLeft() const { return playerbarLeft_; }
void MacroController::setPlayerbarLeft(double value) { SET_IF_CHANGED(playerbarLeft_, value); }
double MacroController::playerbarWidth() const { return playerbarWidth_; }
void MacroController::setPlayerbarWidth(double value) { SET_IF_CHANGED(playerbarWidth_, value); }
double MacroController::fishMarkerLeft() const { return fishMarkerLeft_; }
void MacroController::setFishMarkerLeft(double value) { SET_IF_CHANGED(fishMarkerLeft_, value); }
QString MacroController::compactCaught() const { return compactCaught_; }
void MacroController::setCompactCaught(const QString& value) { SET_IF_CHANGED(compactCaught_, value); }
QString MacroController::compactLost() const { return compactLost_; }
void MacroController::setCompactLost(const QString& value) { SET_IF_CHANGED(compactLost_, value); }
QString MacroController::compactSr() const { return compactSr_; }
void MacroController::setCompactSr(const QString& value) { SET_IF_CHANGED(compactSr_, value); }
QString MacroController::compactAquariumStatus() const { return compactAquariumStatus_; }
void MacroController::setCompactAquariumStatus(const QString& value) { SET_IF_CHANGED(compactAquariumStatus_, value); }
QString MacroController::compactWeather() const { return compactWeather_; }
void MacroController::setCompactWeather(const QString& value) { SET_IF_CHANGED(compactWeather_, value); }
QString MacroController::compactCycle() const { return compactCycle_; }
void MacroController::setCompactCycle(const QString& value) { SET_IF_CHANGED(compactCycle_, value); }
bool MacroController::shinySurge() const { return shinySurge_; }
void MacroController::setShinySurge(bool value) { SET_IF_CHANGED(shinySurge_, value); }
bool MacroController::sparklingSurge() const { return sparklingSurge_; }
void MacroController::setSparklingSurge(bool value) { SET_IF_CHANGED(sparklingSurge_, value); }
bool MacroController::mutationSurge() const { return mutationSurge_; }
void MacroController::setMutationSurge(bool value) { SET_IF_CHANGED(mutationSurge_, value); }
QString MacroController::compactOffsetsVersion() const { return compactOffsetsVersion_; }
void MacroController::setCompactOffsetsVersion(const QString& value) { SET_IF_CHANGED(compactOffsetsVersion_, value); }

void MacroController::toggleMacro() {
    // TODO: connect to real macro runtime start/stop command dispatcher.
    setMacroRunning(!macroRunning_);
    runStateText_ = macroRunning_ ? QStringLiteral("Running") : QStringLiteral("Stopped");
    compactStatusMessage_ = macroRunning_ ? QStringLiteral("Macro active") : QStringLiteral("Macro stopped");
    compactPhase_ = macroRunning_ ? QStringLiteral("Tracking") : QStringLiteral("Idle");
    pushActivity(macroRunning_ ? QStringLiteral("Macro started") : QStringLiteral("Macro stopped"));
    emit stateChanged();
}

void MacroController::rebindHotkey() {
    // TODO: connect to native keybind capture and persistence.
    hotkeyText_ = QStringLiteral("Press key...");
    emit stateChanged();
}

void MacroController::useCursorPosition() {
    // TODO: connect to real cursor sampling from game window context.
    autoAnglerStatusText_ = QStringLiteral("Cursor position captured (stub)");
    emit stateChanged();
}

void MacroController::addEnchant() {
    // TODO: connect to real enchant target store/settings persistence.
    const QString value = newEnchantText_.trimmed();
    if (value.isEmpty()) {
        enchantStatusText_ = QStringLiteral("Enter an enchant name first");
        emit stateChanged();
        return;
    }

    if (!targetEnchants_.contains(value, Qt::CaseInsensitive)) {
        targetEnchants_.append(value);
    }
    selectedTargetEnchant_ = value;
    newEnchantText_.clear();
    enchantStatusText_ = QStringLiteral("Enchant target added (stub)");
    emit stateChanged();
}

void MacroController::addHuntTarget() {
    // TODO: connect to real hunt target repository persistence.
    const QString value = newHuntTarget_.trimmed();
    if (value.isEmpty()) return;

    for (const QVariant& item : availableHuntTargets_) {
        const QVariantMap map = item.toMap();
        if (map.value(QStringLiteral("name")).toString().compare(value, Qt::CaseInsensitive) == 0) {
            newHuntTarget_.clear();
            emit stateChanged();
            return;
        }
    }

    QVariantMap row;
    row.insert(QStringLiteral("name"), value);
    row.insert(QStringLiteral("selected"), false);
    availableHuntTargets_.append(row);
    newHuntTarget_.clear();
    emit stateChanged();
}

void MacroController::unselectAllHuntTargets() {
    // TODO: connect to real hunt detect runtime selection toggles.
    selectedHuntTargets_.clear();

    QVariantList rewritten;
    rewritten.reserve(availableHuntTargets_.size());
    for (const QVariant& item : availableHuntTargets_) {
        QVariantMap map = item.toMap();
        map.insert(QStringLiteral("selected"), false);
        rewritten.append(map);
    }
    availableHuntTargets_ = rewritten;
    emit stateChanged();
}

void MacroController::toggleHuntTarget(const QString& target) {
    // TODO: connect to real hunt detect runtime selection toggles.
    QVariantList rewritten;
    rewritten.reserve(availableHuntTargets_.size());
    bool selected = false;

    for (const QVariant& item : availableHuntTargets_) {
        QVariantMap map = item.toMap();
        const QString name = map.value(QStringLiteral("name")).toString();
        if (name == target) {
            const bool current = map.value(QStringLiteral("selected")).toBool();
            map.insert(QStringLiteral("selected"), !current);
            selected = !current;
        }
        rewritten.append(map);
    }
    availableHuntTargets_ = rewritten;

    if (selected) {
        if (!selectedHuntTargets_.contains(target)) selectedHuntTargets_.append(target);
    } else {
        selectedHuntTargets_.removeAll(target);
    }
    emit stateChanged();
}

void MacroController::removeSelectedHuntTarget(const QString& target) {
    // TODO: connect to real hunt detect runtime selection toggles.
    selectedHuntTargets_.removeAll(target);

    QVariantList rewritten;
    rewritten.reserve(availableHuntTargets_.size());
    for (const QVariant& item : availableHuntTargets_) {
        QVariantMap map = item.toMap();
        if (map.value(QStringLiteral("name")).toString() == target) {
            map.insert(QStringLiteral("selected"), false);
        }
        rewritten.append(map);
    }
    availableHuntTargets_ = rewritten;
    emit stateChanged();
}

void MacroController::openModuleDetails(const QString& moduleName) {
    // TODO: map selected module to the corresponding real runtime settings model.
    if (moduleName.trimmed().isEmpty()) return;
    selectedModule_ = moduleName;
    ++detailsRequestToken_;
    pushActivity(QStringLiteral("Opened module: %1").arg(moduleName));
    emit stateChanged();
}

void MacroController::pushActivity(const QString& message) {
    if (message.trimmed().isEmpty()) return;
    activityFeed_.prepend(message);
    while (activityFeed_.size() > 80) {
        activityFeed_.removeLast();
    }
    emit stateChanged();
}

void MacroController::startRuntimeClock() {
    runtimeElapsedMs_ = 0;
    runTimeText_ = QStringLiteral("00:00:00");
    compactRuntime_ = runTimeText_;
    runtimeElapsed_.restart();
    runtimeTimer_.start();
}

void MacroController::stopRuntimeClock() {
    if (runtimeElapsed_.isValid()) {
        runtimeElapsedMs_ = runtimeElapsed_.elapsed();
    }
    runtimeTimer_.stop();
    refreshRuntimeText();
}

void MacroController::refreshRuntimeText() {
    qint64 ms = runtimeElapsedMs_;
    if (macroRunning_ && runtimeElapsed_.isValid()) {
        ms = runtimeElapsed_.elapsed();
    }

    const QString formatted = formatDurationMs(ms);
    if (runTimeText_ == formatted && compactRuntime_ == formatted) {
        return;
    }

    runTimeText_ = formatted;
    compactRuntime_ = formatted;
    emit stateChanged();
}

QString MacroController::formatDurationMs(qint64 durationMs) const {
    if (durationMs < 0) durationMs = 0;
    const qint64 totalSeconds = durationMs / 1000;
    const qint64 hours = totalSeconds / 3600;
    const qint64 minutes = (totalSeconds % 3600) / 60;
    const qint64 seconds = totalSeconds % 60;
    return QStringLiteral("%1:%2:%3")
        .arg(hours, 2, 10, QLatin1Char('0'))
        .arg(minutes, 2, 10, QLatin1Char('0'))
        .arg(seconds, 2, 10, QLatin1Char('0'));
}

QVariantList MacroController::buildDefaultStatusItems() const {
    QVariantList out;
    out.append(QVariantMap{{QStringLiteral("key"), QStringLiteral("Rod - Equipped")}, {QStringLiteral("value"), QStringLiteral("Masterline Rod")}});
    out.append(QVariantMap{{QStringLiteral("key"), QStringLiteral("Input")}, {QStringLiteral("value"), QStringLiteral("Idle")}});
    out.append(QVariantMap{{QStringLiteral("key"), QStringLiteral("Success")}, {QStringLiteral("value"), QStringLiteral("0")}});
    return out;
}

QVariantList MacroController::buildDefaultStatsItems() const {
    QVariantList out;
    out.append(QVariantMap{{QStringLiteral("key"), QStringLiteral("Caught")}, {QStringLiteral("value"), QStringLiteral("0")}});
    out.append(QVariantMap{{QStringLiteral("key"), QStringLiteral("Lost")}, {QStringLiteral("value"), QStringLiteral("0")}});
    out.append(QVariantMap{{QStringLiteral("key"), QStringLiteral("SR")}, {QStringLiteral("value"), QStringLiteral("100%")}});
    return out;
}

QVariantList MacroController::buildDefaultMutations() const {
    QVariantList out;
    out.append(QVariantMap{{QStringLiteral("name"), QStringLiteral("Abyssal")}, {QStringLiteral("selected"), false}});
    out.append(QVariantMap{{QStringLiteral("name"), QStringLiteral("Electric")}, {QStringLiteral("selected"), false}});
    out.append(QVariantMap{{QStringLiteral("name"), QStringLiteral("Frosted")}, {QStringLiteral("selected"), false}});
    out.append(QVariantMap{{QStringLiteral("name"), QStringLiteral("Molten")}, {QStringLiteral("selected"), false}});
    return out;
}

QVariantList MacroController::buildDefaultHuntTargets() const {
    QVariantList out;
    out.append(QVariantMap{{QStringLiteral("name"), QStringLiteral("Ancient Kraken")}, {QStringLiteral("selected"), false}});
    out.append(QVariantMap{{QStringLiteral("name"), QStringLiteral("Phantom Leviathan")}, {QStringLiteral("selected"), false}});
    out.append(QVariantMap{{QStringLiteral("name"), QStringLiteral("Storm Serpent")}, {QStringLiteral("selected"), false}});
    out.append(QVariantMap{{QStringLiteral("name"), QStringLiteral("Moonray")}, {QStringLiteral("selected"), false}});
    out.append(QVariantMap{{QStringLiteral("name"), QStringLiteral("Aurelion Shark")}, {QStringLiteral("selected"), false}});
    return out;
}
