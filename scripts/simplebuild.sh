#!/bin/bash

if [ ! -d ext-src/cppunit ]; then
  echo "Please call from root folder of project"
  exit 1
fi

pushd ext-src/cppunit
./autogen.sh
CWD=$(pwd)
if [ -f Makefile ]; then
  make distclean
fi
./configure LD=clang LDFLAGS="-stdlib=libc++" CXX=clang++ CC=clang CXXFLAGS="-stdlib=libc++" --prefix=$CWD/../../externals/cppunit
make
make install

popd


