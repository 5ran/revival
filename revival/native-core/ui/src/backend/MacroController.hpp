#pragma once

#include <QObject>
#include <QElapsedTimer>
#include <QString>
#include <QStringList>
#include <QTimer>
#include <QVariantList>

class MacroController : public QObject {
    Q_OBJECT
    Q_PROPERTY(bool macroRunning READ macroRunning WRITE setMacroRunning NOTIFY stateChanged)
    Q_PROPERTY(QString activeMacroText READ activeMacroText WRITE setActiveMacroText NOTIFY stateChanged)
    Q_PROPERTY(QString runStateText READ runStateText WRITE setRunStateText NOTIFY stateChanged)
    Q_PROPERTY(QString runTimeText READ runTimeText NOTIFY stateChanged)
    Q_PROPERTY(QString hotkeyText READ hotkeyText WRITE setHotkeyText NOTIFY stateChanged)
    Q_PROPERTY(QString selectedModule READ selectedModule WRITE setSelectedModule NOTIFY stateChanged)
    Q_PROPERTY(QStringList activityFeed READ activityFeed NOTIFY stateChanged)
    Q_PROPERTY(int detailsRequestToken READ detailsRequestToken NOTIFY stateChanged)
    Q_PROPERTY(QStringList rodSlots READ rodSlots CONSTANT)
    Q_PROPERTY(QString selectedRodSlot READ selectedRodSlot WRITE setSelectedRodSlot NOTIFY stateChanged)

    Q_PROPERTY(QStringList trackerOptions READ trackerOptions CONSTANT)
    Q_PROPERTY(QString selectedTracker READ selectedTracker WRITE setSelectedTracker NOTIFY stateChanged)
    Q_PROPERTY(QStringList castingModes READ castingModes CONSTANT)
    Q_PROPERTY(QString selectedCastingMode READ selectedCastingMode WRITE setSelectedCastingMode NOTIFY stateChanged)
    Q_PROPERTY(QVariantList fishingStatusItems READ fishingStatusItems NOTIFY stateChanged)
    Q_PROPERTY(QVariantList fishingStatsItems READ fishingStatsItems NOTIFY stateChanged)
    Q_PROPERTY(bool masterlineEquipped READ masterlineEquipped WRITE setMasterlineEquipped NOTIFY stateChanged)
    Q_PROPERTY(QString masterlineRodsText READ masterlineRodsText WRITE setMasterlineRodsText NOTIFY stateChanged)

    Q_PROPERTY(QString activeAddonTitle READ activeAddonTitle WRITE setActiveAddonTitle NOTIFY stateChanged)
    Q_PROPERTY(bool autoAquariumEnabled READ autoAquariumEnabled WRITE setAutoAquariumEnabled NOTIFY stateChanged)
    Q_PROPERTY(bool autoTotemEnabled READ autoTotemEnabled WRITE setAutoTotemEnabled NOTIFY stateChanged)
    Q_PROPERTY(bool autoSovEnabled READ autoSovEnabled WRITE setAutoSovEnabled NOTIFY stateChanged)
    Q_PROPERTY(bool huntDetectEnabled READ huntDetectEnabled WRITE setHuntDetectEnabled NOTIFY stateChanged)
    Q_PROPERTY(bool autoAquariumExpanded READ autoAquariumExpanded WRITE setAutoAquariumExpanded NOTIFY stateChanged)
    Q_PROPERTY(bool autoTotemExpanded READ autoTotemExpanded WRITE setAutoTotemExpanded NOTIFY stateChanged)
    Q_PROPERTY(bool autoSovExpanded READ autoSovExpanded WRITE setAutoSovExpanded NOTIFY stateChanged)
    Q_PROPERTY(bool huntDetectExpanded READ huntDetectExpanded WRITE setHuntDetectExpanded NOTIFY stateChanged)
    Q_PROPERTY(double aquariumCycleDelayMinutes READ aquariumCycleDelayMinutes WRITE setAquariumCycleDelayMinutes NOTIFY stateChanged)

    Q_PROPERTY(QString activeAutomationTitle READ activeAutomationTitle WRITE setActiveAutomationTitle NOTIFY stateChanged)
    Q_PROPERTY(QString selectedAutomation READ selectedAutomation WRITE setSelectedAutomation NOTIFY stateChanged)
    Q_PROPERTY(bool autoAnglerEnabled READ autoAnglerEnabled WRITE setAutoAnglerEnabled NOTIFY stateChanged)
    Q_PROPERTY(bool enchantEnabled READ enchantEnabled WRITE setEnchantEnabled NOTIFY stateChanged)
    Q_PROPERTY(bool appraiseEnabled READ appraiseEnabled WRITE setAppraiseEnabled NOTIFY stateChanged)
    Q_PROPERTY(bool treasureAppraiseEnabled READ treasureAppraiseEnabled WRITE setTreasureAppraiseEnabled NOTIFY stateChanged)

    Q_PROPERTY(QString currentFishText READ currentFishText WRITE setCurrentFishText NOTIFY stateChanged)
    Q_PROPERTY(QString autoAnglerStatusText READ autoAnglerStatusText WRITE setAutoAnglerStatusText NOTIFY stateChanged)

    Q_PROPERTY(bool autoEnchantEnabled READ autoEnchantEnabled WRITE setAutoEnchantEnabled NOTIFY stateChanged)
    Q_PROPERTY(QString enchantMode READ enchantMode WRITE setEnchantMode NOTIFY stateChanged)
    Q_PROPERTY(QString targetSearchText READ targetSearchText WRITE setTargetSearchText NOTIFY stateChanged)
    Q_PROPERTY(QStringList targetEnchants READ targetEnchants NOTIFY stateChanged)
    Q_PROPERTY(QString selectedTargetEnchant READ selectedTargetEnchant WRITE setSelectedTargetEnchant NOTIFY stateChanged)
    Q_PROPERTY(QString newEnchantText READ newEnchantText WRITE setNewEnchantText NOTIFY stateChanged)
    Q_PROPERTY(QString enchantRodText READ enchantRodText WRITE setEnchantRodText NOTIFY stateChanged)
    Q_PROPERTY(QString currentEnchantText READ currentEnchantText WRITE setCurrentEnchantText NOTIFY stateChanged)
    Q_PROPERTY(QString enchantStatusText READ enchantStatusText WRITE setEnchantStatusText NOTIFY stateChanged)

    Q_PROPERTY(bool autoAppraiseEnabled READ autoAppraiseEnabled WRITE setAutoAppraiseEnabled NOTIFY stateChanged)
    Q_PROPERTY(QString appraiseMode READ appraiseMode WRITE setAppraiseMode NOTIFY stateChanged)
    Q_PROPERTY(double gamepassSpeed READ gamepassSpeed WRITE setGamepassSpeed NOTIFY stateChanged)
    Q_PROPERTY(bool requireShiny READ requireShiny WRITE setRequireShiny NOTIFY stateChanged)
    Q_PROPERTY(bool requireSparkling READ requireSparkling WRITE setRequireSparkling NOTIFY stateChanged)
    Q_PROPERTY(bool requireTiny READ requireTiny WRITE setRequireTiny NOTIFY stateChanged)
    Q_PROPERTY(bool requireSmall READ requireSmall WRITE setRequireSmall NOTIFY stateChanged)
    Q_PROPERTY(bool requireBig READ requireBig WRITE setRequireBig NOTIFY stateChanged)
    Q_PROPERTY(bool requireGiant READ requireGiant WRITE setRequireGiant NOTIFY stateChanged)
    Q_PROPERTY(QString mutationSearchText READ mutationSearchText WRITE setMutationSearchText NOTIFY stateChanged)
    Q_PROPERTY(QVariantList mutationOptions READ mutationOptions NOTIFY stateChanged)

    Q_PROPERTY(bool autoTreasureEnabled READ autoTreasureEnabled WRITE setAutoTreasureEnabled NOTIFY stateChanged)
    Q_PROPERTY(double treasureClickDelaySeconds READ treasureClickDelaySeconds WRITE setTreasureClickDelaySeconds NOTIFY stateChanged)
    Q_PROPERTY(double treasureMinimumMulti READ treasureMinimumMulti WRITE setTreasureMinimumMulti NOTIFY stateChanged)
    Q_PROPERTY(QString treasureStatusText READ treasureStatusText WRITE setTreasureStatusText NOTIFY stateChanged)

    Q_PROPERTY(double sovMinPercent READ sovMinPercent WRITE setSovMinPercent NOTIFY stateChanged)
    Q_PROPERTY(double sovMaxPercent READ sovMaxPercent WRITE setSovMaxPercent NOTIFY stateChanged)
    Q_PROPERTY(QString sovStatusText READ sovStatusText WRITE setSovStatusText NOTIFY stateChanged)

    Q_PROPERTY(QString discordWebhook READ discordWebhook WRITE setDiscordWebhook NOTIFY stateChanged)
    Q_PROPERTY(QString newHuntTarget READ newHuntTarget WRITE setNewHuntTarget NOTIFY stateChanged)
    Q_PROPERTY(QString huntSearchText READ huntSearchText WRITE setHuntSearchText NOTIFY stateChanged)
    Q_PROPERTY(QVariantList availableHuntTargets READ availableHuntTargets NOTIFY stateChanged)
    Q_PROPERTY(QVariantList selectedHuntTargets READ selectedHuntTargets NOTIFY stateChanged)

    Q_PROPERTY(QStringList totemOptions READ totemOptions CONSTANT)
    Q_PROPERTY(QString selectedTotem READ selectedTotem WRITE setSelectedTotem NOTIFY stateChanged)
    Q_PROPERTY(bool useShinyTotem READ useShinyTotem WRITE setUseShinyTotem NOTIFY stateChanged)
    Q_PROPERTY(bool useSparklingTotem READ useSparklingTotem WRITE setUseSparklingTotem NOTIFY stateChanged)
    Q_PROPERTY(bool useMutationTotem READ useMutationTotem WRITE setUseMutationTotem NOTIFY stateChanged)
    Q_PROPERTY(bool stayDay READ stayDay WRITE setStayDay NOTIFY stateChanged)
    Q_PROPERTY(bool stayNight READ stayNight WRITE setStayNight NOTIFY stateChanged)
    Q_PROPERTY(bool timePreferenceEditable READ timePreferenceEditable WRITE setTimePreferenceEditable NOTIFY stateChanged)
    Q_PROPERTY(QString totemStatusText READ totemStatusText WRITE setTotemStatusText NOTIFY stateChanged)

    Q_PROPERTY(QString compactMacroName READ compactMacroName WRITE setCompactMacroName NOTIFY stateChanged)
    Q_PROPERTY(QString compactPhase READ compactPhase WRITE setCompactPhase NOTIFY stateChanged)
    Q_PROPERTY(QString compactRuntime READ compactRuntime WRITE setCompactRuntime NOTIFY stateChanged)
    Q_PROPERTY(QString compactStatusMessage READ compactStatusMessage WRITE setCompactStatusMessage NOTIFY stateChanged)
    Q_PROPERTY(double playerbarLeft READ playerbarLeft WRITE setPlayerbarLeft NOTIFY stateChanged)
    Q_PROPERTY(double playerbarWidth READ playerbarWidth WRITE setPlayerbarWidth NOTIFY stateChanged)
    Q_PROPERTY(double fishMarkerLeft READ fishMarkerLeft WRITE setFishMarkerLeft NOTIFY stateChanged)
    Q_PROPERTY(QString compactCaught READ compactCaught WRITE setCompactCaught NOTIFY stateChanged)
    Q_PROPERTY(QString compactLost READ compactLost WRITE setCompactLost NOTIFY stateChanged)
    Q_PROPERTY(QString compactSr READ compactSr WRITE setCompactSr NOTIFY stateChanged)
    Q_PROPERTY(QString compactAquariumStatus READ compactAquariumStatus WRITE setCompactAquariumStatus NOTIFY stateChanged)
    Q_PROPERTY(QString compactWeather READ compactWeather WRITE setCompactWeather NOTIFY stateChanged)
    Q_PROPERTY(QString compactCycle READ compactCycle WRITE setCompactCycle NOTIFY stateChanged)
    Q_PROPERTY(bool shinySurge READ shinySurge WRITE setShinySurge NOTIFY stateChanged)
    Q_PROPERTY(bool sparklingSurge READ sparklingSurge WRITE setSparklingSurge NOTIFY stateChanged)
    Q_PROPERTY(bool mutationSurge READ mutationSurge WRITE setMutationSurge NOTIFY stateChanged)
    Q_PROPERTY(QString compactOffsetsVersion READ compactOffsetsVersion WRITE setCompactOffsetsVersion NOTIFY stateChanged)

public:
    explicit MacroController(QObject* parent = nullptr);

    bool macroRunning() const;
    void setMacroRunning(bool value);
    QString activeMacroText() const;
    void setActiveMacroText(const QString& value);
    QString runStateText() const;
    void setRunStateText(const QString& value);
    QString runTimeText() const;
    QString hotkeyText() const;
    void setHotkeyText(const QString& value);
    QString selectedModule() const;
    void setSelectedModule(const QString& value);
    QStringList activityFeed() const;
    int detailsRequestToken() const;
    QStringList rodSlots() const;
    QString selectedRodSlot() const;
    void setSelectedRodSlot(const QString& value);

    QStringList trackerOptions() const;
    QString selectedTracker() const;
    void setSelectedTracker(const QString& value);
    QStringList castingModes() const;
    QString selectedCastingMode() const;
    void setSelectedCastingMode(const QString& value);
    QVariantList fishingStatusItems() const;
    QVariantList fishingStatsItems() const;
    bool masterlineEquipped() const;
    void setMasterlineEquipped(bool value);
    QString masterlineRodsText() const;
    void setMasterlineRodsText(const QString& value);

    QString activeAddonTitle() const;
    void setActiveAddonTitle(const QString& value);
    bool autoAquariumEnabled() const;
    void setAutoAquariumEnabled(bool value);
    bool autoTotemEnabled() const;
    void setAutoTotemEnabled(bool value);
    bool autoSovEnabled() const;
    void setAutoSovEnabled(bool value);
    bool huntDetectEnabled() const;
    void setHuntDetectEnabled(bool value);
    bool autoAquariumExpanded() const;
    void setAutoAquariumExpanded(bool value);
    bool autoTotemExpanded() const;
    void setAutoTotemExpanded(bool value);
    bool autoSovExpanded() const;
    void setAutoSovExpanded(bool value);
    bool huntDetectExpanded() const;
    void setHuntDetectExpanded(bool value);
    double aquariumCycleDelayMinutes() const;
    void setAquariumCycleDelayMinutes(double value);

    QString activeAutomationTitle() const;
    void setActiveAutomationTitle(const QString& value);
    QString selectedAutomation() const;
    void setSelectedAutomation(const QString& value);
    bool autoAnglerEnabled() const;
    void setAutoAnglerEnabled(bool value);
    bool enchantEnabled() const;
    void setEnchantEnabled(bool value);
    bool appraiseEnabled() const;
    void setAppraiseEnabled(bool value);
    bool treasureAppraiseEnabled() const;
    void setTreasureAppraiseEnabled(bool value);

    QString currentFishText() const;
    void setCurrentFishText(const QString& value);
    QString autoAnglerStatusText() const;
    void setAutoAnglerStatusText(const QString& value);

    bool autoEnchantEnabled() const;
    void setAutoEnchantEnabled(bool value);
    QString enchantMode() const;
    void setEnchantMode(const QString& value);
    QString targetSearchText() const;
    void setTargetSearchText(const QString& value);
    QStringList targetEnchants() const;
    QString selectedTargetEnchant() const;
    void setSelectedTargetEnchant(const QString& value);
    QString newEnchantText() const;
    void setNewEnchantText(const QString& value);
    QString enchantRodText() const;
    void setEnchantRodText(const QString& value);
    QString currentEnchantText() const;
    void setCurrentEnchantText(const QString& value);
    QString enchantStatusText() const;
    void setEnchantStatusText(const QString& value);

    bool autoAppraiseEnabled() const;
    void setAutoAppraiseEnabled(bool value);
    QString appraiseMode() const;
    void setAppraiseMode(const QString& value);
    double gamepassSpeed() const;
    void setGamepassSpeed(double value);
    bool requireShiny() const;
    void setRequireShiny(bool value);
    bool requireSparkling() const;
    void setRequireSparkling(bool value);
    bool requireTiny() const;
    void setRequireTiny(bool value);
    bool requireSmall() const;
    void setRequireSmall(bool value);
    bool requireBig() const;
    void setRequireBig(bool value);
    bool requireGiant() const;
    void setRequireGiant(bool value);
    QString mutationSearchText() const;
    void setMutationSearchText(const QString& value);
    QVariantList mutationOptions() const;

    bool autoTreasureEnabled() const;
    void setAutoTreasureEnabled(bool value);
    double treasureClickDelaySeconds() const;
    void setTreasureClickDelaySeconds(double value);
    double treasureMinimumMulti() const;
    void setTreasureMinimumMulti(double value);
    QString treasureStatusText() const;
    void setTreasureStatusText(const QString& value);

    double sovMinPercent() const;
    void setSovMinPercent(double value);
    double sovMaxPercent() const;
    void setSovMaxPercent(double value);
    QString sovStatusText() const;
    void setSovStatusText(const QString& value);

    QString discordWebhook() const;
    void setDiscordWebhook(const QString& value);
    QString newHuntTarget() const;
    void setNewHuntTarget(const QString& value);
    QString huntSearchText() const;
    void setHuntSearchText(const QString& value);
    QVariantList availableHuntTargets() const;
    QVariantList selectedHuntTargets() const;

    QStringList totemOptions() const;
    QString selectedTotem() const;
    void setSelectedTotem(const QString& value);
    bool useShinyTotem() const;
    void setUseShinyTotem(bool value);
    bool useSparklingTotem() const;
    void setUseSparklingTotem(bool value);
    bool useMutationTotem() const;
    void setUseMutationTotem(bool value);
    bool stayDay() const;
    void setStayDay(bool value);
    bool stayNight() const;
    void setStayNight(bool value);
    bool timePreferenceEditable() const;
    void setTimePreferenceEditable(bool value);
    QString totemStatusText() const;
    void setTotemStatusText(const QString& value);

    QString compactMacroName() const;
    void setCompactMacroName(const QString& value);
    QString compactPhase() const;
    void setCompactPhase(const QString& value);
    QString compactRuntime() const;
    void setCompactRuntime(const QString& value);
    QString compactStatusMessage() const;
    void setCompactStatusMessage(const QString& value);
    double playerbarLeft() const;
    void setPlayerbarLeft(double value);
    double playerbarWidth() const;
    void setPlayerbarWidth(double value);
    double fishMarkerLeft() const;
    void setFishMarkerLeft(double value);
    QString compactCaught() const;
    void setCompactCaught(const QString& value);
    QString compactLost() const;
    void setCompactLost(const QString& value);
    QString compactSr() const;
    void setCompactSr(const QString& value);
    QString compactAquariumStatus() const;
    void setCompactAquariumStatus(const QString& value);
    QString compactWeather() const;
    void setCompactWeather(const QString& value);
    QString compactCycle() const;
    void setCompactCycle(const QString& value);
    bool shinySurge() const;
    void setShinySurge(bool value);
    bool sparklingSurge() const;
    void setSparklingSurge(bool value);
    bool mutationSurge() const;
    void setMutationSurge(bool value);
    QString compactOffsetsVersion() const;
    void setCompactOffsetsVersion(const QString& value);

    Q_INVOKABLE void toggleMacro();
    Q_INVOKABLE void rebindHotkey();
    Q_INVOKABLE void useCursorPosition();
    Q_INVOKABLE void addEnchant();
    Q_INVOKABLE void addHuntTarget();
    Q_INVOKABLE void unselectAllHuntTargets();
    Q_INVOKABLE void toggleHuntTarget(const QString& target);
    Q_INVOKABLE void removeSelectedHuntTarget(const QString& target);
    Q_INVOKABLE void openModuleDetails(const QString& moduleName);
    Q_INVOKABLE void pushActivity(const QString& message);

signals:
    void stateChanged();

private:
    void startRuntimeClock();
    void stopRuntimeClock();
    void refreshRuntimeText();
    QString formatDurationMs(qint64 durationMs) const;

    QVariantList buildDefaultStatusItems() const;
    QVariantList buildDefaultStatsItems() const;
    QVariantList buildDefaultMutations() const;
    QVariantList buildDefaultHuntTargets() const;

    bool macroRunning_ = false;
    QString activeMacroText_ = QStringLiteral("Fishing Macro");
    QString runStateText_ = QStringLiteral("Stopped");
    QString runTimeText_ = QStringLiteral("00:00:00");
    QTimer runtimeTimer_;
    QElapsedTimer runtimeElapsed_;
    qint64 runtimeElapsedMs_ = 0;
    QString hotkeyText_ = QStringLiteral("F6");
    QString selectedModule_ = QStringLiteral("Fishing Core");
    QStringList activityFeed_ {
        QStringLiteral("System ready"),
        QStringLiteral("Awaiting macro start")
    };
    int detailsRequestToken_ = 0;
    QStringList rodSlots_ { QStringLiteral("1"), QStringLiteral("2"), QStringLiteral("3"), QStringLiteral("4") };
    QString selectedRodSlot_ = QStringLiteral("1");

    QStringList trackerOptions_ { QStringLiteral("Hybrid"), QStringLiteral("Tracking 1"), QStringLiteral("Tracking 2"), QStringLiteral("Tracking 3") };
    QString selectedTracker_ = QStringLiteral("Hybrid");
    QStringList castingModes_ { QStringLiteral("Normal"), QStringLiteral("Perfect Cast") };
    QString selectedCastingMode_ = QStringLiteral("Normal");
    QVariantList fishingStatusItems_;
    QVariantList fishingStatsItems_;
    bool masterlineEquipped_ = false;
    QString masterlineRodsText_ = QStringLiteral("None");

    QString activeAddonTitle_ = QStringLiteral("Fishing Addons");
    bool autoAquariumEnabled_ = false;
    bool autoTotemEnabled_ = false;
    bool autoSovEnabled_ = false;
    bool huntDetectEnabled_ = false;
    bool autoAquariumExpanded_ = true;
    bool autoTotemExpanded_ = false;
    bool autoSovExpanded_ = false;
    bool huntDetectExpanded_ = false;
    double aquariumCycleDelayMinutes_ = 15.0;

    QString activeAutomationTitle_ = QStringLiteral("Other Automation");
    QString selectedAutomation_ = QStringLiteral("Auto Angler");
    bool autoAnglerEnabled_ = false;
    bool enchantEnabled_ = false;
    bool appraiseEnabled_ = false;
    bool treasureAppraiseEnabled_ = false;

    QString currentFishText_ = QStringLiteral("N/A");
    QString autoAnglerStatusText_ = QStringLiteral("Idle");

    bool autoEnchantEnabled_ = false;
    QString enchantMode_ = QStringLiteral("Gamepass");
    QString targetSearchText_;
    QStringList targetEnchants_ {
        QStringLiteral("Sea Overlord"),
        QStringLiteral("Blessed Song"),
        QStringLiteral("Resonance")
    };
    QString selectedTargetEnchant_ = QStringLiteral("Sea Overlord");
    QString newEnchantText_;
    QString enchantRodText_ = QStringLiteral("Frightful Tryhard Masterline Rod");
    QString currentEnchantText_ = QStringLiteral("No enchant detected");
    QString enchantStatusText_ = QStringLiteral("Waiting");

    bool autoAppraiseEnabled_ = false;
    QString appraiseMode_ = QStringLiteral("Gamepass");
    double gamepassSpeed_ = 0.5;
    bool requireShiny_ = false;
    bool requireSparkling_ = false;
    bool requireTiny_ = false;
    bool requireSmall_ = false;
    bool requireBig_ = false;
    bool requireGiant_ = false;
    QString mutationSearchText_;
    QVariantList mutationOptions_;

    bool autoTreasureEnabled_ = false;
    double treasureClickDelaySeconds_ = 0.25;
    double treasureMinimumMulti_ = 1.0;
    QString treasureStatusText_ = QStringLiteral("Idle");

    double sovMinPercent_ = 45.0;
    double sovMaxPercent_ = 92.0;
    QString sovStatusText_ = QStringLiteral("Idle");

    QString discordWebhook_;
    QString newHuntTarget_;
    QString huntSearchText_;
    QVariantList availableHuntTargets_;
    QVariantList selectedHuntTargets_;

    QStringList totemOptions_ {
        QStringLiteral("Aurora Totem"),
        QStringLiteral("Tempest Totem"),
        QStringLiteral("Abyssal Totem")
    };
    QString selectedTotem_ = QStringLiteral("Aurora Totem");
    bool useShinyTotem_ = false;
    bool useSparklingTotem_ = false;
    bool useMutationTotem_ = false;
    bool stayDay_ = true;
    bool stayNight_ = false;
    bool timePreferenceEditable_ = true;
    QString totemStatusText_ = QStringLiteral("Waiting for cycle window");

    QString compactMacroName_ = QStringLiteral("Fishing");
    QString compactPhase_ = QStringLiteral("Tracking");
    QString compactRuntime_ = QStringLiteral("00:00:00");
    QString compactStatusMessage_ = QStringLiteral("Ready");
    double playerbarLeft_ = 120.0;
    double playerbarWidth_ = 95.0;
    double fishMarkerLeft_ = 145.0;
    QString compactCaught_ = QStringLiteral("0");
    QString compactLost_ = QStringLiteral("0");
    QString compactSr_ = QStringLiteral("100%");
    QString compactAquariumStatus_ = QStringLiteral("Auto Aquarium: idle");
    QString compactWeather_ = QStringLiteral("Clear");
    QString compactCycle_ = QStringLiteral("Day");
    bool shinySurge_ = false;
    bool sparklingSurge_ = false;
    bool mutationSurge_ = false;
    QString compactOffsetsVersion_ = QStringLiteral("Roblox offsets: unknown");
};
