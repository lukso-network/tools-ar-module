using System.Collections;
using UnityEngine;

namespace Assets.scripts.Api
{
    public class ApiManager : MonoBehaviour
    {
        public AvatarManager avatarManager;
        public CanvasController canvasController;
        // Use this for initialization
        void Start() {

        }



        private bool ToBool(string boolStr) {
            return boolStr == "true";
        }

        public async void LoadModel(string url) {
            avatarManager.LoadGltf(url);
        }

        public async void ShowUI(string boolStr) {
            canvasController.gameObject.SetActive(ToBool(boolStr));
        }

        public async void ShowHelpers(string boolStr) {
            var show = ToBool(boolStr);
            canvasController.IsShowLandmarks = show;
            canvasController.IsShowSkeleton = show;
            canvasController.IsShowOrig = show;
        }
    }
}