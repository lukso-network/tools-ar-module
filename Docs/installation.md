# Installation and building process

LICENSE: [Apache License 2.0](/LICENSE)

- [Mediapipe building](#mediapipe-building)
- [Start desktop application](#start-desktop-application)
- [Application usage](/Docs/application_usage.md)

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

By defaul application start with web camera. You can switch to test video or image using UI: ![](/Docs/images/sources.jpg)

There are several test images and videos in the **/Assets/vid** folder. It can be deleted for Android build to decrease apk size





