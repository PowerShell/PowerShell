#!/bin/bash

if [ ! -d ext-src/cppunit ]; then
  echo "Please call from root folder of project"
  exit 1
fi

pushd ext-src/cppunit
./autogen.sh
./configure --prefix=../../externals/cppunit
make
make install

popd


