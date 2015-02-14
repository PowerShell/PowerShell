#include "pal.h"
#include "test_pal.h"

// pull in some more headers
#include "NativeMshConstants.h"
#include "ClrHostWrapper.h"
#include "IPwrshCommonOutput.h"
#include "ConfigFileReader.h"
#include "NativeMsh.h"
#include "SystemCallFacade.h"
#include "WinSystemCallFacade.h"

namespace Microsoft {

    CPPUNIT_TEST_SUITE_REGISTRATION(PalTestSuite);

    // this unit test is used to test if stuff from different headers was pulled in correctly
    // this is for porting compatbility tests, not really functional tests
    void PalTestSuite::testHeaders() {

        // check NativeMshConstants.h
        CPPUNIT_ASSERT_EQUAL(NativeMsh::g_MISSING_COMMAND_LINE_ARGUMENT,1);

        // check ClrHostWrapper.h
        NativeMsh::ICLRRuntimeHost2Wrapper clrHostWrapper;

    }

    void PalTestSuite::testDatatypes() {

        // check basic pointer lengths
        CPPUNIT_ASSERT_EQUAL(sizeof(void*), sizeof(PVOID));

        // windows datatypes
        CPPUNIT_ASSERT_EQUAL(sizeof(WORD),(std::size_t)2);
        CPPUNIT_ASSERT_EQUAL(sizeof(DWORD),(std::size_t)4);
        CPPUNIT_ASSERT_EQUAL(sizeof(HANDLE),sizeof(void*));
        CPPUNIT_ASSERT_EQUAL(sizeof(HWND),sizeof(void*));
        CPPUNIT_ASSERT_EQUAL(sizeof(HMODULE),sizeof(void*));
        CPPUNIT_ASSERT_EQUAL(sizeof(HINSTANCE),sizeof(void*));
        CPPUNIT_ASSERT_EQUAL(sizeof(HGLOBAL),sizeof(void*));
        CPPUNIT_ASSERT_EQUAL(sizeof(HLOCAL),sizeof(void*));
        CPPUNIT_ASSERT_EQUAL(sizeof(HRSRC),sizeof(void*));
        CPPUNIT_ASSERT_EQUAL(sizeof(HRESULT),sizeof(LONG));
        CPPUNIT_ASSERT_EQUAL(sizeof(NTSTATUS),sizeof(LONG));

        // windows integer datatypes
        CPPUNIT_ASSERT_EQUAL(sizeof(INT),(std::size_t)4);
        CPPUNIT_ASSERT_EQUAL(sizeof(INT8),(std::size_t)1);
        CPPUNIT_ASSERT_EQUAL(sizeof(INT16),(std::size_t)2);
        CPPUNIT_ASSERT_EQUAL(sizeof(INT32),(std::size_t)4);
        CPPUNIT_ASSERT_EQUAL(sizeof(INT64),(std::size_t)8);
        CPPUNIT_ASSERT_EQUAL(sizeof(UINT),(std::size_t)4);
        CPPUNIT_ASSERT_EQUAL(sizeof(UINT8),(std::size_t)1);
        CPPUNIT_ASSERT_EQUAL(sizeof(UINT16),(std::size_t)2);
        CPPUNIT_ASSERT_EQUAL(sizeof(UINT32),(std::size_t)4);
        CPPUNIT_ASSERT_EQUAL(sizeof(UINT64),(std::size_t)8);

        // windows integer max and min size constants

        CPPUNIT_ASSERT_EQUAL(CHAR_BIT,8);
        CPPUNIT_ASSERT_EQUAL(SCHAR_MIN,-127-1);
        CPPUNIT_ASSERT_EQUAL(SCHAR_MAX,127);
        CPPUNIT_ASSERT_EQUAL(UCHAR_MAX,0xff);
        CPPUNIT_ASSERT_EQUAL(SHRT_MIN,-32767-1);
        CPPUNIT_ASSERT_EQUAL(SHRT_MAX,32767);
        CPPUNIT_ASSERT_EQUAL(USHRT_MAX,0xffff);
        CPPUNIT_ASSERT_EQUAL(INT_MIN,-2147483647-1);
        CPPUNIT_ASSERT_EQUAL(INT_MAX,2147483647);
        CPPUNIT_ASSERT_EQUAL(UINT_MAX,0xffffffff);

        // TODO: these are part of limits.h and will never fit windows values
//        CPPUNIT_ASSERT_EQUAL(LONG_MIN,-2147483647L-1);
//        CPPUNIT_ASSERT_EQUAL(LONG_MAX,2147483647L);
//        CPPUNIT_ASSERT_EQUAL(ULONG_MAX,0xffffffffUL);

        CPPUNIT_ASSERT_EQUAL(MAXSHORT,0x7fff);
        CPPUNIT_ASSERT_EQUAL(MAXLONG,0x7fffffff);
        CPPUNIT_ASSERT_EQUAL(MAXCHAR,0x7f);
        CPPUNIT_ASSERT_EQUAL(MAXDWORD,0xffffffff);

        // character data types
        CPPUNIT_ASSERT_EQUAL(sizeof(CHAR),(std::size_t)1);
        CPPUNIT_ASSERT_EQUAL(sizeof(TCHAR),(std::size_t)1);


    }

}
