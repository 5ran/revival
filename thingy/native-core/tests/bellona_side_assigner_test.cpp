#include <cstdlib>
#include <iostream>
#include <vector>

#include "bellona_side_assigner.hpp"

using macro_port::BellonaSideAssigner;
using macro_port::ReelContext;
using macro_port::ReelLocatedContext;

static int Assert(bool condition, const char* name)
{
    if (!condition)
    {
        std::cerr << "FAIL: " << name << "\n";
        return 1;
    }
    return 0;
}

int main()
{
    int failed = 0;
    BellonaSideAssigner assigner;

    std::vector<ReelLocatedContext> two{
        ReelLocatedContext{ReelContext{1, 2, 3}, 0.2},
        ReelLocatedContext{ReelContext{4, 5, 6}, 0.8},
    };
    auto right = assigner.SelectRight(two);
    failed += Assert(right.has_value() && right->context.bar == 4, "two-reels-rightmost");

    std::vector<ReelLocatedContext> oneMid{
        ReelLocatedContext{ReelContext{7, 8, 9}, 0.50},
    };
    auto stickyRight = assigner.SelectRight(oneMid);
    failed += Assert(stickyRight.has_value() && stickyRight->context.bar == 7, "single-sticky-right");

    std::vector<ReelLocatedContext> oneLeft{
        ReelLocatedContext{ReelContext{10, 11, 12}, 0.30},
    };
    auto switchedLeft = assigner.SelectRight(oneLeft);
    failed += Assert(!switchedLeft.has_value(), "single-switch-left");

    auto stayLeft = assigner.SelectRight(oneMid);
    failed += Assert(!stayLeft.has_value(), "single-mid-stays-left");

    std::vector<ReelLocatedContext> oneRight{
        ReelLocatedContext{ReelContext{13, 14, 15}, 0.60},
    };
    auto switchedRight = assigner.SelectRight(oneRight);
    failed += Assert(switchedRight.has_value() && switchedRight->context.bar == 13, "single-switch-right");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }
    return 1;
}
