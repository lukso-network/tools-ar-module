# Installation and building process

LICENSE: [Apache License 2.0](/LICENSE)

- [Mediapipe building](#mediapipe-building)
- [Start desktop application](#start-desktop-application)
- [Application usage](/Docs/application_usage.md)
- [Stable diffusion](#stable-diffusion)
- [Android build](#android-build)

## Mediapipe building

Master branch is already contains prebilding libraries for Windows and can be started without additional steps. It contains number of large files so use git-lfs to download it
```
git-lfs clone git@github.com:lukso-network/digitalwardrobe-ar-module-mediapipeline.git
```

A full compilation of Mediapipe Unity Plugin is described in a native documentation [README.md](/README.md).

## Start desktop application

- Open Unity (testend in 2020.3)
- Open **Lukso Single** scene ![](/Docs/images/lukso_scene.jpg)
- Start application:

By defaul application starts with web camera. You can switch to test video or image using UI: ![](/Docs/images/sources.jpg)

There are several test images and videos in the **/Assets/vid** folder. It can be deleted for Android build to decrease apk size

## Stable diffusion

Technical information is [here](/Docs/index.md#stable-diffusion)

To start a proxy server you need Python >= 3.7. 
- Install requirements from [req.txt](/Docs/sb_server/req.txt)
- Start a server: python sb_server.py
- To install stable diffusion please refere to an official [repo](https://gitgud.io/AUTOMATIC1111/stable-diffusion-webui)
You also need to enable REST api and add control net models

## Android build

To build android version of unity part just use standard unity pipeline:
1. In the build settings switch to Adroid platform
2. Build application

## Full application build 
To build application (Unity module + Android logic) please do the following:
1. Clone android application with submodules: ```>git clone --recurse-submodules git@github.com:lukso-network/digitalwardrobe-android-app.git```
1. Export unity build to some empty folder (for example 'unity_export'). It will create a separate android application
1. Merge this exported code with Android application^
   -


