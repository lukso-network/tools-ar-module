# Application usage

![Main ui](/Docs/images/ui.jpg)

UI options:

1. **Body** - shows or hide body model
1. **Skeleton** - shows skeleton 3d bones
1. **Landmarks** - shows white landmarks lines
1. **Transparent** - shows transparent body. It uses to hide invisible geometry
1. **Face  transparent** - face view mode: it can be transparent or white
1. **Show face** - show or hide face geometry 
1. **Physics** - If true then enables physics calculation (works for VRM models only)
1. **Face animation** - Shows face animation - it controls mouth size). Works for VRM models only
1. **VRM cloth** - automatically removes a body from VRM model and shows only dress
1. **Replace material** - Replace standard VRM material with more realistic one

Buttons

1. **Next model** - Loads predefined model (provided with application)
1. **Load model** - Loads any glb/vrm model from a file system
1. **Remove All** - Removes loaded models
1. **Calculate size** - Calculates cloth size
1. **Reset cloth** - Resets cloth size to default
1. **Stable diffusion** - Passes current image to stable diffusion to generate realistic fashion image. It requires additional server support
1. **Next source** - Default build contains several test images and videos. This button select next one
1. **Switch source** - Swithes between static image, video file and web camera.
1. **Load models** -  Helper function to automatically load test models from a server (requires server support). It loads zipped models and unpacks it in device. Then a user can load it with a "Load model" button

Additional controls

**Depth** - controls Z axis depth of models (similar to focal size)
**Pause** - pauses a video feed
