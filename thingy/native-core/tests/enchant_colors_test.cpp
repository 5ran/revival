#include <cstdlib>
#include <iostream>

#include "enchant_colors.hpp"

static int Assert(bool c, const char* n){ if(!c){ std::cerr<<"FAIL: "<<n<<"\n"; return 1;} return 0; }

int main(){
    int failed=0;
    auto c1 = macro_port::EnchantColors::GetEnchantColor("Abyssal");
    failed += Assert(c1.b == 180, "abyssal-blue");
    auto name = macro_port::EnchantColors::FindEnchantName("text with lucky buff");
    failed += Assert(!name.empty(), "find-lucky");
    if(failed==0){ std::cout<<"PASS\n"; return 0;} return 1;
}
