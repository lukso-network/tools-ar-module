using Assets.PoseEstimator;
using Assets.scripts.Avatar;
using DeepMotion.DMBTDemo;
using Lukso;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using UnityEngine;
using UnityWeld.Binding;

[Binding]
public class CanvasController : MonoBehaviour, INotifyPropertyChanged
{

 //TODOLK   private WebCamScreenController player;
    public event PropertyChangedEventHandler PropertyChanged;
    public HelperDrawer helper;
    public DMBTDemoManager skeletonManager;
    public AvatarManager avatarManager;
    public SizeManager sizeManager;

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
    public void Load3DModel() {
#if UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Select model", "", "Gltf files,glb");
        avatarManager.LoadGltf(path, false);
#endif
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
    //TODOLK    sizeManager.ResetSize();
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

  //TODOLK
/*  [Binding]
    public bool IsPaused {
        get { return player == null ? false : player.isPaused; }
        set {
            if (player != null) {
                player.isPaused = value;
                OnPropertyChanged("IsPaused");
            }
        }
    }*/

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
        get { return helper.ShowLandmarks; }
        set {
            helper.ShowLandmarks = value;
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
        //TODOLK player = FindObjectOfType<WebCamScreenController>();
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

        
    }

    public void Update() {
        //OnPropertyChanged("RootScaleValue");
    }

 //TODOLK 
 /*
    [Binding]
    public float PlaybackSpeed {

        get { return player == null ? 0 : player.vp.playbackSpeed; }
        set {
            if (player != null) {
                player.vp.playbackSpeed = value;
                OnPropertyChanged("PlaybackSpeed");
            }
        }

    }*/

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
