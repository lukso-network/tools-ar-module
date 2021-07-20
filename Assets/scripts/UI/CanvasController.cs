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
    public AvatarManager avatarManager;

    [Binding]
    public void SelectVideo() {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select Video", "", "Video files,mp4,avi,mov");
        player.LoadUrl(path);
#endif
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
        get { return skeletonManager.controller?.obj?.activeSelf ?? true; }
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
        OnPropertyChanged("SkinScaleX");
        OnPropertyChanged("SkinScaleZ");
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
