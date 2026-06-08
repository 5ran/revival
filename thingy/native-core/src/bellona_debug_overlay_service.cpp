#include "bellona_debug_overlay_service.hpp"

namespace macro_port
{
std::vector<BellonaDebugBox> BellonaDebugOverlayService::lastBoxes_{};

void BellonaDebugOverlayService::Update(const std::vector<BellonaDebugBox>& boxes)
{
    lastBoxes_ = boxes;
}

void BellonaDebugOverlayService::Hide()
{
    lastBoxes_.clear();
}

const std::vector<BellonaDebugBox>& BellonaDebugOverlayService::LastBoxes()
{
    return lastBoxes_;
}
}
