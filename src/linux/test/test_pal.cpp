#include "test_pal.h"

#include "pal.h"
//#include <cppunit/config/SourcePrefix.h>

namespace Microsoft {

    CPPUNIT_TEST_SUITE_REGISTRATION(PalTestSuite);

    void PalTestSuite::testDatatypes() {

        // windows datatypes
        CPPUNIT_ASSERT_EQUAL(sizeof(WORD),(std::size_t)2);
        CPPUNIT_ASSERT_EQUAL(sizeof(DWORD),(std::size_t)4);

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
        CPPUNIT_ASSERT_EQUAL(LONG_MIN,-2147483647L-1);
        CPPUNIT_ASSERT_EQUAL(LONG_MAX,2147483647L);
        CPPUNIT_ASSERT_EQUAL(ULONG_MAX,0xffffffff);

    }

}