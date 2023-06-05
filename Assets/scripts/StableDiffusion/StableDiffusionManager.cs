using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityWeld.Binding;

namespace Lukso {

    [Binding]
    public class StableDiffusionManager : MonoBehaviour {

        [SerializeField] private GameObject canvas;

        [SerializeField] private RawImage resultImage;
        [SerializeField] private Image errorText;
        [SerializeField] Camera clothCamera;
        [SerializeField] AvatarManager avatarManager;
        [SerializeField] RectTransform loader;
        // Use this for initialization
        void Start() {
            Close();
        }

        [Binding]
        public void Close() {
            canvas.SetActive(false);
            errorText.gameObject.SetActive(false);
            resultImage.gameObject.SetActive(false);
            loader.gameObject.SetActive(false);
        }

        public void ShowImage(Texture2D texture) {
            resultImage.gameObject.SetActive(true);
            resultImage.texture = texture;
            canvas.SetActive(true);
            loader.gameObject.SetActive(false);
        }

        private void ShowError(string message) {
            canvas.SetActive(true);
            errorText.GetComponentInChildren<Text>().text = "Could not create image:\n" + message;
            errorText.gameObject.SetActive(true);
            loader.gameObject.SetActive(false);
            StartCoroutine(AutoHide(5));
        }

        private IEnumerator AutoHide(int timeout) {
            yield return new WaitForSeconds(timeout);
            Close();
        }

        private void ShowLoader() {
            loader.gameObject.SetActive(true);
            canvas.gameObject.SetActive(true);
        }

        // Update is called once per frame
        void Update() {
            if (loader.gameObject.activeInHierarchy) {
                loader.rotation = Quaternion.Euler(0, 0, loader.rotation.eulerAngles.z - Time.deltaTime*40);
            }
        }


        private byte[] GetTexturePNG(RenderTexture rTex) {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            Destroy(tex);
            return bytes;
        }


        private byte[] RenderFullImage(params Camera[] cams) {
            RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
            
            foreach (var c in cams) {
                c.targetTexture = rt;
                c.Render();
            }

            RenderTexture.active = rt;

            foreach (var c in cams) {
                c.targetTexture = null;
            }

            RenderTexture.active = null;
            var bytes = GetTexturePNG(rt);
            Destroy(rt);
            return bytes;
        }

        public void Experiment() {

            GetComponent<TextureSwapper>().CaptureAndSave();
            return;

            int idx = (int)Time.realtimeSinceStartup;

            var ui = GameObject.Find("UI");
            var cam2 = GameObject.Find("Skeleton Camera").GetComponent<Camera>();
            var cam1 = GameObject.Find("MP Camera").GetComponent<Camera>();
            // cam2.clearFlags = CameraClearFlags.Color;

            var oldMask = cam2.cullingMask;

            var image1 = RenderFullImage(cam1, cam2);

            var clearFlag = cam2.clearFlags;
            cam2.clearFlags = CameraClearFlags.Color;
            
            cam2.cullingMask = clothCamera.cullingMask;// layermask;
            var oldTexture = avatarManager.transparentMaterial.mainTexture;
            avatarManager.transparentMaterial.mainTexture = Texture2D.blackTexture;
            var image2 = RenderFullImage(cam2);
            avatarManager.transparentMaterial.mainTexture = oldTexture;

            ui.SetActive(true);
            cam2.clearFlags = clearFlag;
            cam2.cullingMask = oldMask;

            StartCoroutine(CallSBProcessing(image1, image2));

        }


        private IEnumerator CallSBProcessing(byte[] image1Bytes, byte[] image2Bytes) {
            // Create form to send data
            WWWForm form = new WWWForm();
            form.AddBinaryData("image1", image1Bytes);
            form.AddBinaryData("image2", image2Bytes);

            // Send data to REST API


            using (UnityWebRequest req = UnityWebRequest.Post("http://10.8.0.204:5002/process", form)) {
                ShowLoader();
                req.timeout = 120;
                Debug.Log("Sent images");
                req.downloadHandler = new DownloadHandlerTexture();
                yield return req.SendWebRequest();
                // Check for errors
                if (req.result != UnityWebRequest.Result.Success) {
                    Debug.Log(req.error);
                    if (req.responseCode == 500) {
                        ShowError("Server error: " + req.error);
                    } else {
                        ShowError("Connection error: " + req.error);
                    }
                } else {
                    Texture2D texture = DownloadHandlerTexture.GetContent(req);
                    Debug.Log(texture.width);
                    ShowImage(texture);
                }
            }
        }

    }
}