using Lukso;
using Mediapipe.Unity;
using Mediapipe.Unity.SkeletonTracking;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.scripts.Api
{
    public class ApiManager : MonoBehaviour
    {
        public AvatarManager avatarManager;
        public CanvasController canvasController;
        public SizeManager sizeManager;
        [SerializeField] private SkeletonTrackingSolution solution;
    // Use this for initialization
    void Start() {

      }

      public void SetSkinScaleX(string floatValue) {
          float value = ToFloat(floatValue);

          var v = avatarManager.skinScaler;
          avatarManager.skinScaler = new Vector3(value, v.y, value);
      }

      public void SetSkinScaleZ(string floatValue) {
          float value = ToFloat(floatValue);

          var v = avatarManager.skinScaler;
          avatarManager.skinScaler = new Vector3(v.x, v.y, value);
      }

      public async void LoadModel(string url) {
          avatarManager.Load(url, true);
      }

      public async void AppendModel(string url) {
          avatarManager.Load(url, false);
      }

      public async void ShowUI(string boolStr) {
          canvasController.gameObject.SetActive(ToBool(boolStr));
          canvasController.GetComponent<Canvas>().enabled = ToBool(boolStr);
      }

      public async void SelectCamera(string intStr) {
          int camIdx = ToInt(intStr);

          var devices = ImageSourceProvider.ImageSource.sourceCandidateNames;

          if (camIdx >= devices.Length || camIdx < 0) {
              Debug.LogError($"Camera does not exist:{camIdx} is requested");
              return;
          }


          StartCoroutine(PlayCamera(camIdx));
      
      }

      private IEnumerator PlayCamera(int camIdx) {
        ImageSourceProvider.ImageSource.Stop();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        ImageSourceProvider.ImageSource.SelectSource(camIdx);
        solution.StartTracking();
    }



      //canvasController.gameObject.SetActive(ToBool(boolStr));
    

    public async void ShowHelpers(string boolStr) {
            var show = ToBool(boolStr);
            canvasController.IsShowLandmarks = show;
            canvasController.IsShowSkeleton = show;
            //canvasController.IsShowTransparent = show;
        }

        public async void CalculateSize() {
            sizeManager.CalculateSize();
        }

        private bool ToBool(string boolStr) {
            return boolStr == "true";
        }

        private int ToInt(string intStr) {
            try {
                return int.Parse(intStr);
            } catch (Exception ex) {
                Debug.LogError("Can't parse string to int:" + intStr);
                return 0;
            }
        }

        private float ToFloat(string floatStr) {
            try {
                return float.Parse(floatStr);
            } catch (Exception ex) {
                Debug.LogError("Can't parse string to float:" + floatStr);
                return 0;
            }
        }

    }
}
