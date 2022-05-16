# Lukso Documentation

- [Android API](#android-api)
- [API methods](#api-methods)

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
