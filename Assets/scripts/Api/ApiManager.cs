﻿using System;
using System.Collections;
using UnityEngine;

namespace Assets.scripts.Api
{
    public class ApiManager : MonoBehaviour
    {
        public AvatarManager avatarManager;
        public CanvasController canvasController;
        public SceneDirector sceneDirector;
        // Use this for initialization
        void Start() {

        }

        public async void LoadModel(string url) {
            avatarManager.LoadGltf(url, true);
        }

        public async void AppendModel(string url) {
            avatarManager.LoadGltf(url, false);
        }

        public async void ShowUI(string boolStr) {
            canvasController.gameObject.SetActive(ToBool(boolStr));
            canvasController.GetComponent<Canvas>().enabled = ToBool(boolStr);
        }

        public async void SelectCamera(string intStr) {
            int camIdx = ToInt(intStr);
            var devices = WebCamTexture.devices;

            if (camIdx >= devices.Length) {
                Debug.LogError($"Camera does not exist:{camIdx} is requested");
                return;
            }
            sceneDirector.ChangeWebCamDevice(devices[camIdx]);

            //canvasController.gameObject.SetActive(ToBool(boolStr));
        }

        public async void ShowHelpers(string boolStr) {
            var show = ToBool(boolStr);
            canvasController.IsShowLandmarks = show;
            canvasController.IsShowSkeleton = show;
            canvasController.IsShowOrig = show;
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
    }
}