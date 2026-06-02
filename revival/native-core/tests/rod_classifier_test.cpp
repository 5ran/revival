#include <cstdlib>
#include <iostream>

#include "rod_classifier.hpp"

using macro_port::RodClassifier;
using macro_port::RodKind;

static int AssertEq(RodKind expected, RodKind actual, const char* name)
{
    if (expected != actual)
    {
        std::cerr << "FAIL: " << name << "\n";
        return 1;
    }
    return 0;
}

int main()
{
    int failed = 0;
    failed += AssertEq(RodKind::BellonaWaraxe, RodClassifier::Classify("Bellona's Waraxe"), "bellona");
    failed += AssertEq(RodKind::MasterlineRod, RodClassifier::Classify("Masterline Rod"), "masterline");
    failed += AssertEq(RodKind::Pinion, RodClassifier::Classify("Pinion's Aria"), "pinion");
    failed += AssertEq(RodKind::SplitbranchTwig, RodClassifier::Classify("Splitbranch Twig"), "splitbranch");
    failed += AssertEq(RodKind::Default, RodClassifier::Classify(""), "default-empty");

    if (failed == 0)
    {
        std::cout << "PASS\n";
        return 0;
    }

    return 1;
}

