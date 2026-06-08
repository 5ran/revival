#pragma once

#include <cstdint>
#include <vector>

namespace macro_port
{
struct BellonaDebugBox
{
    double x = 0.0;
    double y = 0.0;
    double width = 0.0;
    double height = 0.0;
    std::uint32_t argb = 0xFFFFFFFF;
};

class BellonaDebugOverlayService
{
public:
    static void Update(const std::vector<BellonaDebugBox>& boxes);
    static void Hide();
    static const std::vector<BellonaDebugBox>& LastBoxes();

private:
    static std::vector<BellonaDebugBox> lastBoxes_;
};
}
