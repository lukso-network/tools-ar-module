using Assets.PoseEstimator;
using Assets.scripts.Avatar;
using DeepMotion.DMBTDemo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityWeld.Binding;

[Binding]
public class CanvasController : MonoBehaviour, INotifyPropertyChanged
{

    private WebCamScreenController player;
    public event PropertyChangedEventHandler PropertyChanged;
    public HelperDrawer helper;
    public DMBTDemoManager skeletonManager;

    [Binding]
    public void SelectVideo() {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select Video", "", "Video files,mp4,avi,mov");
        player.LoadUrl(path);
#endif
    }

    [Binding]
    public void ResetAvatar() {
        skeletonManager.ResetAvatar();
    }

    [Binding]
    public void Rotate90() {
        var angles = Camera.main.transform.eulerAngles;
        angles.z += 90;
        Camera.main.transform.eulerAngles = angles;

    }

    [Binding]
    public bool IsPaused {
        get { return player == null ? false : player.isPaused; }
        set {
            if (player != null) {
                player.isPaused = value;
                OnPropertyChanged("IsPaused");
            }
        }
    }

    [Binding]
    public bool IsShowBody {
        get { return helper.ShowBody; }
        set {
            helper.ShowBody = value;
            OnPropertyChanged("ShowBody");
        }
    }

    [Binding]
    public bool IsShowSkeleton {
        get { return helper.ShowSkeleton; }
        set {
            helper.ShowSkeleton = value;
            OnPropertyChanged("ShowSkeleton");
        }
    }

    [Binding]
    public bool IsShowLandmarks {
        get { return helper.ShowLandmarks; }
        set {
            helper.ShowLandmarks = value;
            OnPropertyChanged("ShowLandmarks");
        }
    }

    [Binding]
    public bool IsShowOrig {
        get { return skeletonManager.controller.obj.activeSelf; }
        set {
            skeletonManager.controller.obj.SetActive(value);
             OnPropertyChanged("IsShowOrig");
        }
    }

    [Binding]
    public float RootScaleValue {
        get { return skeletonManager.controller.GetHips().transform.localScale.x; }
        set {
        }
    }


    private void OnPropertyChanged(string propertyName) {
        if (PropertyChanged != null) {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    void Start() {
        player = FindObjectOfType<WebCamScreenController>();
        OnPropertyChanged("IsPaused");
        OnPropertyChanged("PlaybackSpeed");
        OnPropertyChanged("ShowBody");
        OnPropertyChanged("ShowSkeleton");
        OnPropertyChanged("ShowLandmarks");
        OnPropertyChanged("IsShowOrig");
        OnPropertyChanged("RootScaleValue");
        OnPropertyChanged("ScaleDepth");
    }

    public void Update() {
        OnPropertyChanged("RootScaleValue");
    }

    [Binding]
    public float PlaybackSpeed {

        get { return player == null ? 0 : player.vp.playbackSpeed; }
        set {
            if (player != null) {
                player.vp.playbackSpeed = value;
                OnPropertyChanged("PlaybackSpeed");
            }
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
    /*
    private static string[] filterOptions = Enum.GetNames(typeof(FilterType));
    private string selectedFilter;

    [Binding]
    public float KalmanDt {
        get { return poseScaler.kalmanDt; }
        set {
            poseScaler.kalmanDt = value;
            OnPropertyChanged("KalmanDt");
            poseScaler.OnValidate();
        }
    }

    [Binding]
    public float ScalingFilterSmoothStep {
        get { return poseScaler.cameraScaleKalmanDt; }
        set {
            poseScaler.cameraScaleKalmanDt = value;
            OnPropertyChanged("ScalingFilterSmoothStep");
            poseScaler.OnValidate();
        }
    }

    [Binding]
    public float StepCount {
        get { return dmManager.ikSettings.stepCount; }
        set {
            dmManager.ikSettings.stepCount = (int) value;
            OnPropertyChanged("StepCount");
        }
    }


    [Binding]
    public bool SelfieEnabled {
        get { return poseScaler.selfieEnabled; }
        set {
            poseScaler.selfieEnabled = value;
            OnPropertyChanged("SelfieEnabled");
        }
    }

    [Binding]
    public bool StretchingEnabled {
        get { return dmManager.ikSettings.stretchingEnabled; }
        set {
            dmManager.ikSettings.stretchingEnabled = value;
            OnPropertyChanged("StretchingEnabled");
        }
    }


    [Binding]
    public bool ShowAvatar {
        get { return avatarManager.IsAvatarsVisible(); }
        set {
            avatarManager.ShowAvatar(value);
            OnPropertyChanged("ShowAvatar");
        }
    }

    [Binding]
    public bool RawFilterEnabled {
        get { return poseScaler.discardPointStep > 1; }
        set {

            poseScaler.discardPointStep = value ? 10 : 0;
            OnPropertyChanged("RawFilterEnabled");
            poseScaler.OnValidate();
        }
    }

    [Binding]
    public string SelectedFilterItem {
        get {
            return poseScaler.filterType.ToString();
        }
        set {
            poseScaler.filterType = (FilterType)Enum.Parse(typeof(FilterType), value, true);

            OnPropertyChanged("SelectedFilterItem");
            poseScaler.OnValidate();
        }
    }

    public string[] FilterOptions {
        get {
            return filterOptions;
        }
    }



    public event PropertyChangedEventHandler PropertyChanged;
    public DMBTDemoManager dmManager;
    public PoseScaler poseScaler;
    public AvatarManager avatarManager;

    private void OnPropertyChanged(string propertyName) {
        if (PropertyChanged != null) {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        OnPropertyChanged("StepCount");
        OnPropertyChanged("StretchingEnabled");
        OnPropertyChanged("RawFilterEnabled");
        OnPropertyChanged("KalmanDt");
        OnPropertyChanged("SelectedFilterItem");
        OnPropertyChanged("ScalingFilterSmoothStep");
        OnPropertyChanged("ShowAvatar");
        OnPropertyChanged("SelfieEnabled");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    */

}
