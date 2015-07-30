#pragma once

#include <string>
#include "common/coreclrutil.h"

namespace HostUtil
{

//!\brief       get a list of absolute paths separated by : from a list of relative/absolute paths separated by :
std::string getAbsolutePathList(const std::string& paths)
{
    //std::cerr << "getAbsolutePathList: paths=" << paths << std::endl;
    std::string result;

    // split by :
    size_t lastPos = 0;
    size_t curPos = paths.find(':',lastPos);
    do
    {
        const std::string token = paths.substr(lastPos,curPos-lastPos);
        //std::cerr << "curPos=" << curPos << " lastPos=" << lastPos << " token=" << token << std::endl;

        // skip empty tokens
        if (token != "")
        {
            std::string absolutePath;
            if (CoreCLRUtil::GetAbsolutePath(token.c_str(),absolutePath))
            {
                // add colons correctly
                if (result.size() == 0)
                    result += absolutePath;
                else
                    result += ":" + absolutePath;
            }
        }

        // increment lastPos to skip the :
        lastPos += token.size() + 1;
        curPos = paths.find(':',lastPos);
    }
    while (lastPos < paths.size());

    return result;
}

}


