# Native Code Testing Guide

"Native" tests are tests which validate the native C/C++ function calls into the system library.  To run ONLY the native tests, it is assumed that you have completed the process in the README.md, and have run `source monad-docker.sh`.

From the scripts directory, run `monad-run make -j native-tests`.  This will output an xml file to your scripts directory.

# Creating new tests

monad-linux/src/monad-native/src/tests is the test folder containing all the .cpp files.  you will need to `#include <gtest/gtest.h>` and follow gtest guidelines for creating your test suite.