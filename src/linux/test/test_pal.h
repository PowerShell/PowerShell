#include <cppunit/extensions/HelperMacros.h>

namespace Microsoft {

    class PalTestSuite : public CPPUNIT_NS::TestFixture {
    CPPUNIT_TEST_SUITE(PalTestSuite);
            CPPUNIT_TEST(testDatatypes);
        CPPUNIT_TEST_SUITE_END();

    public:
        void testDatatypes();

    };

}