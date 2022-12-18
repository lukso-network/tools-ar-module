using Assets.scripts.Api;
using Lukso;
using Mediapipe.Unity;
using Mediapipe.Unity.SelfieSegmentation;
using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityWeld.Binding;
using SimpleFileBrowser;
using System.IO;
using Mediapipe.Unity.SkeletonTracking;




[Binding]
public class CanvasController : MonoBehaviour, INotifyPropertyChanged {

    //TODOLK   private WebCamScreenController player;
    public event PropertyChangedEventHandler PropertyChanged;
    public HelperDrawer helper;
    public DMBTDemoManager skeletonManager;
    public AvatarManager avatarManager;
    public SizeManager sizeManager;
    private string initFilePath = null;


    [SerializeField] private SkeletonTrackingSolution solution;
    [SerializeField] private Camera screenCamera;
    [SerializeField] private SelfieSegmentationCreator segmentation;
    [SerializeField] private ApiManager apiManager;
    [SerializeField] private GameObject annotationLayer;



    [Binding]
    public string SelectedCamera {
        get {
            return ImageSourceProvider.ImageSource?.sourceName ?? "";
        }
        set {
            if (ImageSourceProvider.ImageSource?.sourceName == value) {
                return; // No change.
            }


            var sources = CameraSource;
            int idx = Array.IndexOf(sources, value);
            apiManager.SelectCamera("" + idx);
            StartCoroutine(UpdateCameraSettings());

        }
    }

    private IEnumerator UpdateCameraSettings() {
        // Debug functionality
        yield return new WaitForSeconds(0.1f);
        // OnPropertyChanged("SelectedCamera");
    }

    [Binding]
    public void SelectVideo() {

        //GameObject.Find("ApiManager").GetComponent<Assets.scripts.Api.ApiManager>().ShowHelpers("false");
        // GameObject.Find("ApiManager").GetComponent<Assets.scripts.Api.ApiManager>().SelectCamera("1");
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select Video", "", "Video files,mp4,avi,mov");
        //TODOLK  player.LoadUrl(path);
#endif
    }

    [Binding]
    public string[] CameraSource {
        get {
            return ImageSourceProvider.ImageSource == null ? new string[] { } : ImageSourceProvider.ImageSource.sourceCandidateNames;
        }
    }

    [Binding]
    public void Load3DModel() {
        /*#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel("Select model", "", "Gltf files,glb");
            avatarManager.LoadGltf(path, false);
        #endif
        */
        StartCoroutine(ShowLoadDialogCoroutine());
    }


    IEnumerator ShowLoadDialogCoroutine() {

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, initFilePath, null, "Load models", "Load");

        if (FileBrowser.Success) {
            var file = FileBrowser.Result[0];

            initFilePath = Path.GetDirectoryName(file);
            avatarManager.Load(file, false);
        }
    }


    [Binding]
    public void RemveAll() {
        avatarManager.RemoveAllModels(true);
    }

    [Binding]
    public void CalcSize() {
        GameObject.Find("ApiManager").GetComponent<Assets.scripts.Api.ApiManager>().CalculateSize();
    }

    [Binding]
    public void ResetClothSize() {
        sizeManager.ResetSize();
    }

    [Binding]
    public void CaptureSegmentation() {
        segmentation.CaptureSegmentation();
    }

    [Binding]
    public void SelectNextSource() {

        ImageSourceProvider.ImageSource.SelectNextSource();
        solution.StartTracking();
    }
    [Binding]
    public void SwitchSource() {

        var t = ImageSourceProvider.ImageSource.type;
        if (t == ImageSource.SourceType.Image) {
            ImageSourceProvider.SwitchSource(ImageSource.SourceType.Camera);
        } else {
            ImageSourceProvider.SwitchSource(ImageSource.SourceType.Image);
        }
        solution.StartTracking();
    }

    [Binding]
    public void SelectNextResolution() {

        ImageSourceProvider.ImageSource.SelectNextResolution();
        solution.StartTracking();
    }

    [Binding]
    public void ShowNextModel() {
        avatarManager.LoadNextTestModel();

    }

    [Binding]
    public void ResetAvatar() {
        skeletonManager.ResetAvatar();
    }

    [Binding]
    public void Rotate90() {
        var angles = screenCamera.transform.eulerAngles;
        angles.z += 90;
        screenCamera.transform.eulerAngles = angles;

    }

    [Binding]
    public bool IsPaused {
        get { return !ImageSourceProvider.ImageSource?.isPlaying ?? false; }
        set {
            var source = ImageSourceProvider.ImageSource;
            if (source == null || !source.isPlaying == value) {
                return;
            }

            if (value) {
                source.Pause();
            } else {
                StartCoroutine(source.Resume());
            }
            OnPropertyChanged("IsPaused");
        }
    }

    [Binding]
    public bool IsShowBody {
        get { return helper.ShowBody; }
        set {
            helper.ShowBody = value;
            OnPropertyChanged("IsShowBody");
        }
    }

    [Binding]
    public bool IsShowSkeleton {
        get { return helper.ShowSkeleton; }
        set {
            helper.ShowSkeleton = value;
            OnPropertyChanged("IsShowSkeleton");
        }
    }

    [Binding]
    public bool IsShowLandmarks {
        get { return annotationLayer.activeSelf; }
        set {
            annotationLayer.SetActive(value);
            OnPropertyChanged("IsShowLandmarks");
        }
    }

    [Binding]
    public bool IsShowTransparent {
        get { return avatarManager.ShowTransparentBody; }// return skeletonManager.controller?.obj?.activeSelf ?? true; }
        set {
            avatarManager.ShowTransparentBody = value;
            OnPropertyChanged("IsShowTransparent");
        }
    }


    [Binding]
    public bool IsShowFaceTransparent {
        get { return skeletonManager.ShowTransparentFace; }// return skeletonManager.controller?.obj?.activeSelf ?? true; }
        set {
            skeletonManager.ShowTransparentFace = value;
            OnPropertyChanged("IsShowFaceTransparent");
        }
    }


    [Binding]
    public bool IsShowFace {
        get { return skeletonManager.ShowFace; }// return skeletonManager.controller?.obj?.activeSelf ?? true; }
        set {
            skeletonManager.ShowFace = value;
            OnPropertyChanged("IsShowFace");
        }
    }

    [Binding]
    public bool IsUsePhysics {
        get { return skeletonManager.UsePhysics; }// return skeletonManager.controller?.obj?.activeSelf ?? true; }
        set {
            skeletonManager.UsePhysics = value;
            OnPropertyChanged("IsUsePhysics");
        }
    }

    [Binding]
    public bool IsFaceAnimationEnabled {
        get { return skeletonManager.enableFaceAnimation; }// return skeletonManager.controller?.obj?.activeSelf ?? true; }
        set {
            skeletonManager.enableFaceAnimation = value;
            OnPropertyChanged("IsFaceAnimationEnabled");
        }
    }

    [Binding]
    public bool IsVrmCloth {
        get { return skeletonManager.UsePhysics; }// return skeletonManager.controller?.obj?.activeSelf ?? true; }
        set {
            skeletonManager.UsePhysics = value;
            OnPropertyChanged("IsVrmCloth");
        }
    }

    [Binding]
    public float RootScaleValue {
        get { return 1; }// return skeletonManager.controller.GetHips().transform.localScale.x; }
        set {
        }
    }


    private void OnPropertyChanged(string propertyName) {
        if (PropertyChanged != null) {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    void Start() {
        OnPropertyChanged("IsPaused");
        OnPropertyChanged("PlaybackSpeed");
        OnPropertyChanged("IsShowBody");
        OnPropertyChanged("IsShowSkeleton");
        OnPropertyChanged("IsShowLandmarks");
        OnPropertyChanged("IsShowTransparent");
        OnPropertyChanged("IsShowFaceTransparent");
        OnPropertyChanged("RootScaleValue");
        OnPropertyChanged("ScaleDepth");
        OnPropertyChanged("SkinScaleX");
        OnPropertyChanged("SkinScaleZ");
        OnPropertyChanged("SelectedCamera");
        OnPropertyChanged("CameraSource");
        OnPropertyChanged("IsShowFace");
        OnPropertyChanged("IsUsePhysics");
        OnPropertyChanged("IsFaceAnimationEnabled");
        OnPropertyChanged("IsVrmCloth");

        StartCoroutine(WaitBootStrap());

        FileBrowser.SetFilters(true, new FileBrowser.Filter("Model", new string[] { ".glb", ".vrm" }), new FileBrowser.Filter("Gltf", ".glb"), new FileBrowser.Filter("VRoid", ".vrm"));
        FileBrowser.SetDefaultFilter(".glb");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
    }

    private IEnumerator WaitBootStrap() {
        while (ImageSourceProvider.ImageSource?.sourceName == null) {
            yield return new WaitForEndOfFrame();
        }
        OnPropertyChanged("SelectedCamera");
        FindObjectOfType<DropdownBinding>().Init();

    }

    public void Update() {
        //OnPropertyChanged("RootScaleValue");
    }

    //TODOLK 
    [Binding]
    public float PlaybackSpeed {

        get { return 0; }
        set {
        }

    }

    [Binding]
    public float ScaleDepth {

        get => skeletonManager.scaleDepth;
        set {
            skeletonManager.scaleDepth = value;
            OnPropertyChanged("ScaleDepth");
        }

    }

    [Binding]
    public float SkinScaleX {

        get => avatarManager.skinScaler.x;
        set {
            var v = avatarManager.skinScaler;
            // Note X changes Z too for simulteneous update
            avatarManager.skinScaler = new Vector3(value, v.y, value);
            OnPropertyChanged("SkinScaleX");
            OnPropertyChanged("SkinScaleZ");
        }

    }

    [Binding]
    public float SkinScaleZ {

        get => avatarManager.skinScaler.z;
        set {
            var v = avatarManager.skinScaler;
            // Note X changes Z too for simulteneous update
            avatarManager.skinScaler = new Vector3(v.x, v.y, value);
            OnPropertyChanged("SkinScaleZ");
        }

    }


}
