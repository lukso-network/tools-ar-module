# Lukso Documentation

LICENSE: [Apache License 2.0](/LICENSE)

## Installation process
Fully described [here](/Docs/installation.md)

## Application usage
[Here](/Docs/application_usage.md)

## Techincal details:
- [Android API](#android-api)
- [API methods](#api-methods)
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
 	- [Stable diffusion](#stable-diffusion)
- [Technology description](#technology-description)
- [3rd party libraries](#third-party-libraries)

## Android API

Api is managed by [ApiManager.cs](/Assets/scripts/Api/ApiManager.cs). It provides functionality for model management, camera selection, UI, parameters

This API can be used with `UnitySendMessage(objName, methodName, args)` method in Android
- objName - is the name of UnityObject 'ApiManager'
- methodName - API method
- args - arguments of method (all data are passed as strings)

The current Android Module has a helper class for every API call (ArCameraView.kt)

```kotlin

sealed class UnityMessage(
    val objName: String = "ApiManager",
    val methodName: String = "MessageFromAndroid",
    val args: String = "",
) {
    object EnableBody : UnityMessage(methodName = "ShowHelpers", args="true")
    object DisableBody : UnityMessage(methodName = "ShowHelpers", args="false")
    object EnableDeepMotion : UnityMessage(args = "enable_dmmn")
    object DisableDeepMotion : UnityMessage(args = "disable_dmmn")
    object EnableUI : UnityMessage(methodName = "ShowUI", args = "true")
    object DisableUI : UnityMessage(methodName = "ShowUI", args = "false")
    object ShowFacemask : UnityMessage(methodName = "ShowFaceMask", args = "true")
    object HideFacemask : UnityMessage(methodName = "ShowFaceMask", args = "false")

    class LoadModel(args: String) :
        UnityMessage(methodName="LoadModel", args = args)

    class AppendModel(args: String) :
        UnityMessage(methodName="AppendModel", args = args)

    class SetSkinScaleX(args: String) :
        UnityMessage(methodName="SetSkinScaleX", args = args)

    class SetSkinScaleZ(args: String) :
        UnityMessage(methodName="SetSkinScaleZ", args = args)

    class SelectCamera(args: String) : UnityMessage(methodName = "SelectCamera", args = args)
}
```

UnityMessage can be sent with the helper function
```kotlin
fun sendMessageToUnity(message: UnityMessage) =
    with(message) {
        UnitySendMessage(objName, methodName, args)
    }
```

## API methods

### Model processing

- `LoadModel(string uri)` <br> Loads model by specified uri. It deletes the existing model and adds a new one. Currently, uri to file system is supported
- `AppendModel(string uri)` <br> The same functionality as `LoadModel` but it does not remove previous model

### Camera

- `SelectCamera(string intStr)` <br> Switches camera to the given one. Parameters is the integer string (camera id)

### Cloth estimation

- `CalculateSize()` <br> Forces cloth to be calculated. Currently does not have Android function for it

### UI
- `ShowUI(string boolStr)` <br> Helper function. It shows or hides debugging unity UI. Parameters is "true"/"false" 

### Other

- `void SetSkinScaleX(string floatValue)`
- `void SetSkinScaleY(string floatValue)` <br> Sets body scale in two directions

------------

## Supported Features
Currently application supports the following features:
- [Skeleton calculation](#skeleton-calculation)
- [Multiple models support](#multiple-models-support)
- [Cloth calculation](#cloth-calculation)
- [GLTF model loading](#gltf-model-loading)
- [Light estimation](#light-estimation)
- [VRM model loading](#vrm-model-loading)
- [VRM model material replacement](#vrm-material-replacement)
- [VRM animation of eye and mounth](#vrm-animation)
- [VRM physics](#vrm-physics)

### Skeleton calculation
Skeleton position is calculated using the mediapipe library and consists of several steps
1. Processing the original image using the**[mediapipe unity plugin](https://github.com/homuler/MediaPipeUnityPlugin)**  library. As a result, the application receives a set of key points corresponding to bone joints. 
Each point is presented in normalized XYZ coordinates (visible range -1..1).

2.  Each point is pre-processed to convert the normalized coordinates into true 3D coordinates and correct the associated perspective distortion.  To correct the perspective, a special parameter has been added that affects the level of distortion
Additional smoothing and filtering is added in steps 1 and 2 to reduce judder. [One euro 
filter](https://github.com/jaantollander/OneEuroFilter) is used for this

3.  The position and rotation of the bones are calculated to get the closest position of the skeleton to the found points:
	- Calculation of the global scale and position of the skeleton (by comparing the length of the bones of the skeleton and points on the image)
	- Calculation of torso points using the inverse kinematics method (position of the shoulders and pelvis)
	- Calculation of the rest of the bones of the arms and legs using the technology of pulling and rotating the bones to match the points
	- Each bone joint is further offset and scaled to fit clothing.
	
	Note: For every type of model we need to create its structure by hand and store it in [skeletons.txt](/Assets/Resources/skeletons.txt) file

### Multiple models support

The application supports the imposition of several items of clothing at the same time. Each element can be downloaded separately. The implementation of this mechanism is made using avatars:

- The application supports several types of skeleton. All models (with the exception of the VRM format) of the same type are controlled by one skeleton (avatar). For VRM format, each model has its own avatar (format limitation)
- To hide invisible elements (for example, the back of the collar or the effect of hands overlapping the body), a standard avatar of the model is displayed with a material background, which creates the effect of "invisible person"

### GLTF model loading
The application can load 3D models in GLTF format with materials. Loading occurs only from a file (downloading from the Internet is not supported)

**Note**: if some material in GLTF does not work please look [here](https://github.com/atteneder/glTFast#materials-and-shader-variants)

### Light Estimation

The application can roughly determine the direction to the light source and its intensity. The technology is based on the following:
1.  With the help of the mediapipe, the application receives the coordinates of all points of the face
2. Using data on the illumination of the face, the algorithm tries to calculate the position of the light and the average intensity
3. The calculated positions of the light source are averaged and smoothed to reduce jitter.

### VRM model loading

The VRM file format is supported by the  [UniVRM](https://github.com/vrm-c/UniVRM.git)  library. This library allows  to load standard VRM files with materials, animation and physics support. Note: The source code of the library has been tweaked to implement correct physics support in the application.
The standard tool for creating models is [VRoid studio](https://vroid.com/en/studio). 

**Note:** each vrm file has the same basic structure, but is not compatible with each other and cannot use the same avatar (weights of bones, clothing and hair nodes are different). Therefore, when loading a vrm file, each model is associated with its own avatar.

### VRM material replacement

Standard materials in VRM models are not "Only" materials and do not support lighting. The application provides the ability to replace such materials with a custom shader that supports lighting and makes models more realistic. At the moment, the parameter that regulates this option is not supported through the API and works only in Unity

### VRM animation

We have added experimental functionality to capture animation from the user's face and transfer it to the vrm model. Only mouth and eye animations are currently supported. Animation of other elements is not supported. BlendSnaps used in BPM format do not have separate animations for this

### VRM physics

The vrm plugin supports hair and clothing physics in models. When loading into the application, it is possible to enable or disable this functionality. 
**Note:** Models must be true-to-life in order to correctly simulate vrm physics.
**Note:** Each vrm file has the same basic structure, but is not compatible with each other and cannot use the same avatar (weights of bones, clothing and hair nodes are different). Therefore, when loading a vrm file, each model is associated with its own avatar.

## Stable diffusion

This feature uses a powerfull image generation technique to create a photo realistic image. It also uses an innovative approach to display the generated image over the model.
It requires a server support as image generation can taks 20-30 seconds

Main steps:
1. Lusko application captures current image and send it to a simple proxy server using REST protocol.
2. Proxy server preprocess image, generate a correct request for stable diffusion (promts, parameters and so on) and passes it to main Stable diffusion server.
3. Processed image is passed back to lukso appplication
4. Lukso application shows this image and also update model's texture to show image over the model

Required software
1. Proxy server - a simple rest server [Look here](/sb_server)
2. Stable diffusion [server](https://gitgud.io/AUTOMATIC1111/stable-diffusion-webui) with control net model installed

**Note:**
Texture calculation requires to use [gamma color space](https://docs.unity3d.com/Manual/LinearRendering-LinearOrGammaWorkflow.html).
We use the following settings:
![](/Docs/images/gamma_settings.jpg)

------------
## Technology description
------------
## Third party libraries

|Library   |   Description|   License|
| ------------ | ------------ | ------------ |
| **[mediapipe](https://google.github.io/mediapipe/)**  |This is a conveyor for different ml operation like pose esitimation, face finding and so on. It is used in the project indirectly through [mediapipe-unity-plugin](https://github.com/homuler/MediaPipeUnityPlugin)  | [Apache License 2.0](http://www.apache.org/licenses/LICENSE-2.0)  |
| **[mediapipe unity plugin](https://github.com/homuler/MediaPipeUnityPlugin)**  |This plugin links Unity and mediapipe library (0.8.3 version is used in the project)  | [MIT](https://github.com/homuler/MediaPipeUnityPlugin/blob/master/LICENSE)  |
| **[glTFast](https://github.com/atteneder/glTFast)**  | Enables use of glTF files |[Apache License 2.0](http://www.apache.org/licenses/LICENSE-2.0)   |
| **[draco](https://github.com/atteneder/DracoUnity.git)**  |Unity package that integrates the Draco 3D data compression library within Unity. Used indirectly with [glTFast](#glTFast)   | [Apache License 2.0](http://www.apache.org/licenses/LICENSE-2.0)   |
| **[KTXUnity](https://github.com/atteneder/KtxUnity.git)**  | Unity package that allows users to load KTX 2.0 or Basis Universal texture files. Required to support materials in glTFast models  |[Apache License 2.0](http://www.apache.org/licenses/LICENSE-2.0)   |
| **[Simple File Browser](https://github.com/yasirkula/UnitySimpleFileBrowser.git)**   | Adds file open dialog for different OS.  Required for development only   | [MIT](https://github.com/yasirkula/UnitySimpleFileBrowser/blob/master/LICENSE.txt)  |
|  **[Unity Weld](https://github.com/Real-Serious-Games/Unity-Weld)** | MVVM-style data-binding system for Unity.   | [MIT](https://github.com/Real-Serious-Games/Unity-Weld/blob/master/LICENSE)  |
|  **[UniVRM](https://github.com/vrm-c/UniVRM.git)** |  The standard implementation of 3D Avatar file format VRM for Unity. (**Note:** the source code of the library has been tweaked to implement the correct support for physics in the application)| [MIT](https://github.com/vrm-c/UniVRM/blob/master/LICENSE.txt)  |







