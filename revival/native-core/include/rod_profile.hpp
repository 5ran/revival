#pragma once

#include <memory>
#include <optional>

#include "note_target.hpp"
#include "reel_metrics.hpp"
#include "rod_kind.hpp"

namespace macro_port
{
class RodProfile
{
public:
    virtual ~RodProfile() = default;
    virtual RodKind Kind() const = 0;
    virtual int FishingActionDelayMs() const { return 0; }
    virtual ReelMetrics AdjustTarget(const ReelMetrics& metrics, const std::optional<NoteTarget>& note) { return metrics; }
    virtual bool TransformHold(bool desiredHold, const std::optional<double>& progress) { (void)progress; return desiredHold; }
    virtual void Reset() {}

    static std::unique_ptr<RodProfile> For(RodKind kind);
};

class DefaultRodProfile final : public RodProfile
{
public:
    RodKind Kind() const override { return RodKind::Default; }
};

class BellonaWaraxeRodProfile final : public RodProfile
{
public:
    RodKind Kind() const override { return RodKind::BellonaWaraxe; }
};

class MasterlineRodProfile final : public RodProfile
{
public:
    RodKind Kind() const override { return RodKind::MasterlineRod; }
};

class TranquilityRodProfile final : public RodProfile
{
public:
    RodKind Kind() const override { return RodKind::Tranquility; }
};

class DreambreakerRodProfile final : public RodProfile
{
public:
    RodKind Kind() const override { return RodKind::Dreambreaker; }
    bool TransformHold(bool desiredHold, const std::optional<double>& progress) override;
};

class RequiemRodProfile final : public RodProfile
{
public:
    RodKind Kind() const override { return RodKind::Requiem; }
    int FishingActionDelayMs() const override { return 160; }
};

class SplitbranchTwigRodProfile final : public RodProfile
{
public:
    RodKind Kind() const override { return RodKind::SplitbranchTwig; }
};

class MiguRodProfile final : public RodProfile
{
public:
    RodKind Kind() const override { return RodKind::MiguRod; }
};

class PinionRodProfile final : public RodProfile
{
public:
    RodKind Kind() const override { return RodKind::Pinion; }
    void Reset() override;
    ReelMetrics AdjustTarget(const ReelMetrics& metrics, const std::optional<NoteTarget>& note) override;

private:
    static bool IsNoteInPlayerBar(double x, const ReelMetrics& metrics, double padding);
    static std::optional<double> GetBothTargets(double fishX, double noteX, double halfWidth);
    void UpdateNoteCount(const NoteTarget& note, const ReelMetrics& metrics);

    int notesCaught_ = 0;
    bool noteCounted_ = false;
    bool resonanceActive_ = false;
};
}

