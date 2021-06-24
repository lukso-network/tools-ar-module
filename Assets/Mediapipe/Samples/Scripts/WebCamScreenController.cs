using Mediapipe;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class WebCamScreenController : MonoBehaviour {
  [SerializeField] int Width = 640;
  [SerializeField] int Height = 480;
  [SerializeField] int FPS = 30;
  [SerializeField] float FocalLengthPx = 2.0f; /// TODO: calculate it from webCamDevice info if possible.
  private const int TEXTURE_SIZE_THRESHOLD = 50;
  private const int MAX_FRAMES_TO_BE_INITIALIZED = 500;
    public int videoRotateAngle = 0;

    public int VideoAngle { get => prevAngle; }
    private int prevAngle = -1;
    private int actualFrameWidth = 0;
    private int actualFrameHeight = 0;
    private int scrWidth = 0;
    private int scrHeight = 0;
    public Vector2 ScreenSize { get; private set; }

    public Texture2D test;
  private WebCamDevice webCamDevice;
  private WebCamTexture webCamTexture;
  private Texture2D outputTexture;
  private Color32[] pixelData;
    private Quaternion baseRotation;


    public bool useCamera = true;
    protected Texture2D videoTexture;
    public VideoPlayer vp;


    public delegate void OnNewFrameRendered(Texture2D texture);
    public event OnNewFrameRendered newFrameRendered;

    public void Awake() {
        if (useCamera) {
            vp.enabled = false;
        }

    }
    public void Start() {
        //videoTexture = new Texture2D((intt)vp.clip.width, (int)vp.clip.height, TextureFormat.RGB24, false);
        videoTexture = new Texture2D(640, 480, TextureFormat.RGB24, false);
        vp.sendFrameReadyEvents = true;
        vp.frameReady += OnNewFrame;
        baseRotation = transform.localRotation;

        //vp.loopPointReached += LoopPointReached;
        vp.Play();

        UpdateSize(Width, Height);
    }

    private void Resize() {
        var cam = Camera.main;
        
    }

    protected void OnNewFrame(VideoPlayer source, long frameIdx) {
        RenderTexture renderTexture = source.texture as RenderTexture;
        if (videoTexture.width != renderTexture.width || videoTexture.height != renderTexture.height) {
            videoTexture.Resize(renderTexture.width, renderTexture.height);
            //GetComponent<Renderer>().material.mainTexture = videoTexture;
        }

        RenderTexture oldRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        videoTexture.ReadPixels(new UnityEngine.Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        videoTexture.Apply();
        //videoTexture.Resize(640, 480);
        RenderTexture.active = oldRT;
        //OnNewFrame(this);
    }

    public bool isPaused;

    public bool isPlaying {
    get { return isWebCamTextureInitialized && webCamTexture.isPlaying; }
  }

  private bool isWebCamTextureInitialized {
    get {
      // Some cameras may take time to be initialized, so check the texture size.
      return webCamTexture != null && webCamTexture.width > TEXTURE_SIZE_THRESHOLD;
    }
  }

  private bool isWebCamReady {
    get {
      return isWebCamTextureInitialized && pixelData != null;
    }
  }

  public IEnumerator ResetScreen(WebCamDevice? device) {
    if (isPlaying) {
      webCamTexture.Stop();
      webCamTexture = null;
      pixelData = null;
    }

    if (device is WebCamDevice deviceValue) {
      webCamDevice = deviceValue;
    } else {
      yield break;
    }

    webCamTexture = new WebCamTexture(webCamDevice.name, Width, Height, FPS);
    WebCamTextureFramePool.Instance.SetDimension(Width, Height);

    try {
      webCamTexture.Play();
      Debug.Log($"WebCamTexture Graphics Format: {webCamTexture.graphicsFormat}");
    } catch (Exception e) {
      Debug.LogWarning(e.ToString());
      yield break;
    }

    var waitFrame = MAX_FRAMES_TO_BE_INITIALIZED;

    yield return new WaitUntil(() => {
      return isWebCamTextureInitialized || --waitFrame < 0;
    });

    if (!isWebCamTextureInitialized) {
      Debug.LogError("Failed to initialize WebCamTexture");
      yield break;
    }

    Renderer renderer = GetComponent<Renderer>();
   // outputTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
    outputTexture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
    renderer.material.mainTexture = outputTexture;

    pixelData = new Color32[webCamTexture.width * webCamTexture.height];
  }

  public float GetFocalLengthPx() {
    return isPlaying ? FocalLengthPx : 0;
  }

  public Color32[] GetPixels32() {
    return isPlaying ? webCamTexture.GetPixels32(pixelData) : null;
  }

  public IntPtr GetNativeTexturePtr() {
    return webCamTexture.GetNativeTexturePtr();
  }

  public Texture2D GetScreen() {
    return outputTexture;
  }

  public void DrawScreen(Color32[] colors) {
    if (!isWebCamReady) { return; }

    // TODO: size assertion
    outputTexture.SetPixels32(colors);
    outputTexture.Apply();
  }

  public void DrawScreen(TextureFrame src) {
    if (!isWebCamReady) { return; }

    // TODO: size assertion
    src.CopyTexture(outputTexture);
        newFrameRendered(src.GetTexture());
    }

  public void DrawScreen(ImageFrame imageFrame) {
    if (!isWebCamReady) { return; }

    outputTexture.LoadRawTextureData(imageFrame.MutablePixelData(), imageFrame.PixelDataSize());
    outputTexture.Apply();
  }

  public void DrawScreen(GpuBuffer gpuBuffer) {
    if (!isWebCamReady) { return; }

#if (UNITY_STANDALONE_LINUX || UNITY_ANDROID) && !UNITY_EDITOR_OSX && !UNITY_EDITOR_WIN
    // TODO: create an external texture
    outputTexture.UpdateExternalTexture((IntPtr)gpuBuffer.GetGlTextureBuffer().Name());
#else
    throw new NotSupportedException();
#endif
  }


    int frame = 0;
  public TextureFramePool.TextureFrameRequest RequestNextFrame() {
    return WebCamTextureFramePool.Instance.RequestNextTextureFrame((TextureFrame textureFrame) => {
      if (isPlaying && !isPaused) {
            //TODO TEMPORARY
            if (useCamera) {
                textureFrame.CopyTextureFrom(webCamTexture);
                UpdateSize(webCamTexture.width, webCamTexture.height, webCamTexture.videoRotationAngle);
            } else {
                frame++;
            if (frame % 1 == 0 && vp.isPlaying) {
                    textureFrame.CopyTextureFrom(videoTexture);
                    UpdateSize(videoTexture.width, videoTexture.height, videoRotateAngle);
                }
            }
           // textureFrame.CopyTextureFrom(webCamTexture);

            //textureFrame.CopyTextureFrom((Texture2D)GetComponent<Renderer>().material.mainTexture);
           // textureFrame.CopyTextureFrom(videoTexture);
      }
    });
  }

    private void UpdateSize(int width, int height, int angle = 0) {
        //Debug.Log("******* width:" + width + ", angle:" + angle);
        if (angle == 90 || angle == 270) {
            var temp = width;
            width = height;
            height = temp;
        }

        if (width == actualFrameWidth && height == actualFrameHeight && angle == prevAngle) {
            return;
        }

        prevAngle = angle;
        actualFrameWidth = width;
        actualFrameHeight = height;
        int refHeight = 4;
        transform.localScale = new Vector3((float)width / height * refHeight, 1, refHeight);


        Quaternion rot = Quaternion.Euler(0, 0, angle);
        Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
        
        GetComponent<Renderer>().material.SetMatrix("_TextureRotation", m);
        //transform.localRotation = baseRotation * Quaternion.Euler(0, angle, 0);

        UpdateCamera();
    }

    private void UpdateCamera() {
        var cam = Camera.main;
        float hCam = transform.localScale.z;


        float texAspect = (float)actualFrameWidth / actualFrameHeight;

        float dist = 0;
        var tg = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad/2);
        if (texAspect < cam.aspect) {
            dist = transform.localScale.z / tg / 2;
            ScreenSize = new Vector2(texAspect / cam.aspect, 1);
        } else {
            dist = transform.localScale.x / cam.aspect / tg / 2;
            ScreenSize = new Vector2(1, cam.aspect / texAspect);
        }
/*
        float h1 = transform.localScale.x / cam.aspect ;
        float h2 = transform.localScale.z;

        


        dist = Mathf.Max(h1, h2)/ tg / 2;
        //  var h = Mathf.Max(h1, h2);
*/
        //  var dist = h / tg;
        var pos = cam.transform.position;
        pos.z = -dist;

        cam.transform.position = pos;
    }

    void Update() {
        if (Screen.width != scrWidth || Screen.height != scrHeight) {
            scrWidth = Screen.width;
            scrHeight = Screen.height;

            UpdateCamera();
        }

    }

    private class WebCamTextureFramePool : TextureFramePool {}
}
