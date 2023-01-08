#!/bin/bash

set -e

cd ..

APP_BUNDLE=dist/bnbnav.app
RESOURCES_BUNDLE=$APP_BUNDLE/Contents/Resources

rm -rf $APP_BUNDLE || true

pushd BnbnavClient
dotnet publish -r osx-$BUILD_ARCH --self-contained
popd

mkdir -p $APP_BUNDLE/Contents/MacOS
cp -r "BnbnavClient/bin/Debug/net"*"/osx-$BUILD_ARCH/"* $APP_BUNDLE/Contents/MacOS
mkdir -p $RESOURCES_BUNDLE
#cp Distribution/icon.icns $RESOURCES_BUNDLE
cp dist/Info.plist $APP_BUNDLE/Contents/

codesign --deep --force -s - $APP_BUNDLE || true
