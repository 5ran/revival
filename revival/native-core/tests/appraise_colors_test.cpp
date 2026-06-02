#include <cstdlib>
#include <iostream>

#include "appraise_colors.hpp"

static int Assert(bool c, const char* n){ if(!c){ std::cerr<<"FAIL: "<<n<<"\n"; return 1;} return 0; }

int main(){
    int failed=0;
    auto c1 = macro_port::AppraiseColors::GetAppraiseColor("Hexed");
    failed += Assert(c1.r == 255 && c1.g == 0, "hexed-red");
    auto names = macro_port::AppraiseColors::GetKnownMutationNames();
    failed += Assert(!names.empty(), "names-nonempty");
    if(failed==0){ std::cout<<"PASS\n"; return 0;} return 1;
}
