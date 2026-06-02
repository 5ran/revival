#include <cstdlib>
#include <iostream>
#include <vector>

#include "hunt_detect_core.hpp"

static int Assert(bool c, const char* n){ if(!c){ std::cerr<<"FAIL: "<<n<<"\n"; return 1;} return 0; }

int main(){
    int failed=0;
    auto rules = macro_port::HuntDetectCore::DefaultRules();
    std::vector<std::string> entries{"A Megalodon has been spotted near Roslit"};
    auto d = macro_port::HuntDetectCore::Detect(entries, rules);
    failed += Assert(d.matched, "matched");
    failed += Assert(d.title == "Megalodon", "title");
    if(failed==0){ std::cout<<"PASS\n"; return 0;} return 1;
}
