#!/bin/bash

set -e

cd ..

APP_BUNDLE=dist/bnbnav.app

rm -rf $APP_BUNDLE || true

pushd BnbnavNetClient.Mac
dotnet workload restore
dotnet publish -r osx-$BUILD_ARCH --self-contained -c release
popd

mv "BnbnavNetClient.Mac/bin/Release/net"*"/osx-$BUILD_ARCH/BnbnavNetClient.Mac.app" $APP_BUNDLE

codesign --deep --force -s - $APP_BUNDLE || true
