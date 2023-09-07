# Installation and building process

LICENSE: [Apache License 2.0](/LICENSE)

- [Mediapipe building](#mediapipe-building)
- [Start desktop application](#start-desktop-application)
- [Supported features](#supported-features)
	- [Skeleton calculation](#skeleton-calculation)
	- [Multiple models support](#multiple-models-support)
	- [Cloth calculation](#cloth-calculation)
	- [GLTF model loading](#gltf-model-loading)
	- [Light estimation](#light-estimation)
	- [VRM model loading](#vrm-model-loading)
	- [VRM model material replacement](#vrm-material-replacement)
	- [VRM animation of eye and mounth](#vrm-animation)
	- [VRM physics](#vrm-physics)
- [Technology description](#technology-description)
- [3rd party libraries](#third-party-libraries)

## Mediapipe building

Master branch is already contains prebilding libraries for Windows and can be started without additional steps. It contains number of large files so use git-lfs to download it
```
git-lfs clone git@github.com:lukso-network/digitalwardrobe-ar-module-mediapipeline.git
```

A full compilation of Mediapipe Unity Plugin is described in a native documentation [README.md](/README.md).

## Start desktop application

- Open Unity (testend in 2020.3)
- Open **Lukso Single** scene ![](Docs/lukso_scene.jpg)




