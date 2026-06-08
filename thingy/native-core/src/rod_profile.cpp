#include "rod_profile.hpp"

#include <cmath>

namespace macro_port
{
std::unique_ptr<RodProfile> RodProfile::For(RodKind kind)
{
    switch (kind)
    {
    case RodKind::BellonaWaraxe:
        return std::make_unique<BellonaWaraxeRodProfile>();
    case RodKind::MasterlineRod:
        return std::make_unique<MasterlineRodProfile>();
    case RodKind::Pinion:
        return std::make_unique<PinionRodProfile>();
    case RodKind::Tranquility:
        return std::make_unique<TranquilityRodProfile>();
    case RodKind::Dreambreaker:
        return std::make_unique<DreambreakerRodProfile>();
    case RodKind::Requiem:
        return std::make_unique<RequiemRodProfile>();
    case RodKind::SplitbranchTwig:
        return std::make_unique<SplitbranchTwigRodProfile>();
    case RodKind::MiguRod:
        return std::make_unique<MiguRodProfile>();
    default:
        return std::make_unique<DefaultRodProfile>();
    }
}

bool DreambreakerRodProfile::TransformHold(bool desiredHold, const std::optional<double>& progress)
{
    if (progress && *progress >= 40.0)
    {
        return !desiredHold;
    }
    return desiredHold;
}

void PinionRodProfile::Reset()
{
    notesCaught_ = 0;
    noteCounted_ = false;
    resonanceActive_ = false;
}

ReelMetrics PinionRodProfile::AdjustTarget(const ReelMetrics& metrics, const std::optional<NoteTarget>& note)
{
    static constexpr double NoteDeadzone = -19.5;
    if (!note.has_value())
    {
        return metrics;
    }

    if (note->sy < NoteDeadzone)
    {
        return metrics;
    }

    UpdateNoteCount(*note, metrics);

    if (resonanceActive_)
    {
        ReelMetrics out = metrics;
        out.fishCenter = note->sx;
        return out;
    }

    const auto halfWidth = metrics.playerbarWidth / 2.0;
    const auto both = GetBothTargets(metrics.fishCenter, note->sx, halfWidth);
    ReelMetrics out = metrics;
    out.fishCenter = both.has_value() ? *both : note->sx;
    return out;
}

void PinionRodProfile::UpdateNoteCount(const NoteTarget& note, const ReelMetrics& metrics)
{
    if (!noteCounted_ && note.sy >= -0.8 && note.sy <= 0.53)
    {
        if (IsNoteInPlayerBar(note.sx, metrics, 0.1))
        {
            noteCounted_ = true;
            notesCaught_ += 1;
        }
        else
        {
            notesCaught_ = 0;
            resonanceActive_ = false;
            noteCounted_ = true;
        }
    }

    if (note.sy < -8.0)
    {
        noteCounted_ = false;
    }

    if (notesCaught_ >= 7)
    {
        resonanceActive_ = true;
    }
}

bool PinionRodProfile::IsNoteInPlayerBar(double x, const ReelMetrics& metrics, double padding)
{
    const auto halfWidth = metrics.playerbarWidth / 2.0;
    return x >= metrics.playerbarCenter - halfWidth - padding &&
           x <= metrics.playerbarCenter + halfWidth + padding;
}

std::optional<double> PinionRodProfile::GetBothTargets(double fishX, double noteX, double halfWidth)
{
    const auto distance = std::abs(noteX - fishX);
    const auto fullWidth = halfWidth * 2.0;
    if (distance > fullWidth)
    {
        return std::nullopt;
    }
    return (fishX + noteX) / 2.0;
}
}

