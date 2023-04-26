using Mediapipe.Unity;
using Mediapipe.Unity.SelfieSegmentation;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Screen = UnityEngine.Screen;

namespace Lukso {

    class ManualSizing {
        private GameObject selected;
        private GameObject duplicate;


        public void ProcessUI() {
            if (Input.GetMouseButtonDown(0)) {

                if (selected == null) {

                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out hit, LayerMask.NameToLayer("joint"))) {
                        Transform objectHit = hit.transform;

                        selected = objectHit.gameObject;
                        duplicate = GameObject.Instantiate(selected);
                        duplicate.SetActive(false);


                        selected.transform.localScale *= 3;
                        selected.GetComponent<Renderer>().material.color = new Color(1, 0.5f, 0);
                        // Do something with the object that was hit by the raycast.
                    }
                } else {

                }


            }

            if (Input.GetMouseButtonUp(0) && selected != null) {

                selected.transform.localScale = duplicate.transform.localScale;
                selected.GetComponent<Renderer>().material.color = duplicate.GetComponent<Renderer>().material.color;
                GameObject.Destroy(duplicate);

                selected = duplicate = null;
            }
        }
    }

    public class SizeManager : MonoBehaviour {

        [SerializeField] Camera clothCamera;
        [SerializeField] Shader selfieClothShader;
        [SerializeField] Shader maskShader;
        [SerializeField] DMBTDemoManager poseManager;
        //[SerializeField] SelfieSegmentation selfieSegmentation;
        [SerializeField] SelfieSegmentationCreator selfieSegmentation;
        [SerializeField] AvatarManager avatarManager;
        [SerializeField] ComputeShader iouShader;
        [SerializeField] SkeletonManager skeletonManager;
        [SerializeField] private GameObject screenPlane;
        [SerializeField] private Camera skeletonCamera;
        [SerializeField] UnityEngine.UI.RawImage outputUI = null;
        [SerializeField] private GameObject bodyPrefab = null;

        [Range(-0.5f, 0.5f)]
        [SerializeField] private float[] clothParametersTest;
        [SerializeField] private bool manualClothParameters;
        [SerializeField] private bool useTransparentBody;
        //[SerializeField] private LayerMask layermask;
        [SerializeField] Shader whiteMaskShader;

        private ComputeBuffer iouBuffer;
        private int iouKernerlHandle;

        private ManualSizing manualSizing = new ManualSizing();
        private bool calculateionInProgres = false;
        private Material maskMaterial;
        private RenderTexture renderedMask;

        // Use this for initialization
        void Start() {
            clothCamera.targetTexture = new RenderTexture(256, 256, 0);
            clothCamera.SetReplacementShader(selfieClothShader, null);
            poseManager.newPoseEvent += UpdateSegmentation;
            maskMaterial = new Material(maskShader);
            renderedMask = new RenderTexture(256, 256, 0);

            //clothCamera.enabled = false;

            InitComputeShader();
            SetClothTexture(clothCamera.targetTexture);
        }

        void OnDestroy() {
            Destroy(maskMaterial);
            Destroy(renderedMask);
        }

        private void InitComputeShader() {
            iouBuffer = new ComputeBuffer(4, sizeof(uint));
            iouKernerlHandle = iouShader.FindKernel("IOU");
            iouShader.SetBuffer(iouKernerlHandle, "sum_buffer", iouBuffer);
        }

        private void SetClothTexture(Texture texture) {
            //selfieSegmentation.SetClothTexture(texture);
        }

        private void UpdateSegmentation(bool hasSkeleton) {
            //TODO debugging - called on every pose event
            //selfieSegmentation.CaptureSegmentation(poseManager.GetLastFrame());
        }

        void Update() {


            var avatar = skeletonManager.GetClothController();
            if (avatar != null) {
                if (manualClothParameters) {
                    avatar.CopyToClothParameters(clothParametersTest);
                } else {
                    avatar.CopyFromClothParameters(clothParametersTest);
                }
            }

            return;
            InitClothCamera();
            var v = CalculateIOR(selfieSegmentation.GetLastMask(), clothCamera.targetTexture);
            //  Debug.Log(v);

            CaptureSegmentation();

        }

        private void CaptureSegmentation() {
            var mask = selfieSegmentation.CaptureSelfieToTexture();
            if (mask == null) {
                return;
            }

            clothCamera.Render();
            return;
            maskMaterial.SetTexture("_MaskTexture", mask);
            maskMaterial.SetTexture("_ClothTexture", clothCamera.targetTexture);
            Graphics.Blit(null, renderedMask, maskMaterial);
            //Graphics.Blit(null, null, maskMaterial);
            outputUI.texture = renderedMask;
        }

        /*  private void UpdateCamera() {
              var mc = Camera.main;

              clothCamera.transform.position = mc.transform.position;
              clothCamera.fieldOfView = mc.fieldOfView;

           //   CalculateIOR(selfieSegmentation.GetMask(), clothCamera.targetTexture); 
          }*/

        // private void PlayVideo(bool play) {
        //player.isPaused = !play;
        //    poseManager.PauseProcessing(!play);
        // }

        public void ResetSize() {
            skeletonManager.GetClothController()?.ResetClothSize();
            skeletonManager.GetClothController()?.CopyFromClothParameters(clothParametersTest);
        }

        public void CalculateSize() {
            if (calculateionInProgres) {
                calculateionInProgres = false;
                return;
            }


            // 1 Pause video
            // 2 get mask
            // 3 draw outfit
            // 4 compare mask
            // 5 tune parameters
            // 6 Goto 3
            // 7 Resume video

            //StartCoroutine(CalculateSizeCoroutine());
            //

            StartCoroutine(FindBestSize());
        }

        private IEnumerator FindBestSize() {
            manualClothParameters = false;
            /*
            for(int k = 0; k < 200; ++k) {
                var a = skeletonManager.GetClothController();
                a.DebugChange();
                yield return new WaitForEndOfFrame();
            }
            yield break;
            */

            var avatar = skeletonManager.GetClothController();
            if (avatar == null) {
                yield break;
            }

            var prevTransparentState = avatarManager.ShowTransparentBody;
            
            var mask = selfieSegmentation.CaptureSelfieToTexture();
            if (mask == null) {
                yield break;
            }

            ResetSize();

            calculateionInProgres = true;
            var imageSource = ImageSourceProvider.ImageSource;

            var isPaused = !imageSource.isPlaying;
            imageSource.Pause();
            poseManager.PauseProcessing(true);

            avatarManager.SetSkinRecalulation(true);
            avatarManager.ShowTransparentBody = useTransparentBody;

            //!!! Very strange problem
            // if InitClothCamera is used then /yield return new WaitForEndOfFrame(); should be used too
            // in the other case if can give an incorrect calculations


            InitClothCamera(); //yield return new WaitForEndOfFrame(); // use at the same time

            yield return avatar.FindBestCloth(() => {
                avatar.ApplyClothShift(true);
                avatarManager.UpdateSkeleton(true);
                var old = RenderTexture.active;
                RenderTexture.active = clothCamera.targetTexture;
                clothCamera.Render();
                RenderTexture.active = old;

                SetClothTexture(clothCamera.targetTexture);

                // minus as we find maximum ior
                var ior = CalculateIOR(mask, clothCamera.targetTexture);
                Debug.Log("Ior1:" + ior);
                return -ior;
            });

            //yield return new WaitForSeconds(1);
            poseManager.PauseProcessing(false);
            if (!isPaused) {
                StartCoroutine(imageSource.Resume());
            }
            avatarManager.SetSkinRecalulation(false);
            calculateionInProgres = false;

            avatarManager.ShowTransparentBody = prevTransparentState;

        }

        private void InitClothCamera() {
            var mc = skeletonCamera;
            clothCamera.transform.position = mc.transform.position;


            var s = screenPlane.transform.lossyScale;
            var p = screenPlane.transform.position;
            var d = p.z - clothCamera.transform.position.z;
            var fov = Mathf.Rad2Deg * (Mathf.Atan(s.y / d / 2)) * 2;
            var aspect = s.x / s.y;
            clothCamera.aspect = aspect;
            clothCamera.fieldOfView = fov;

        }


        private float CalculateIOR(Texture mask, RenderTexture targetTexture) {
            if (mask == null || targetTexture == null) {
                return 0;
            }
            /*
            var old = RenderTexture.active;
            RenderTexture.active = (RenderTexture)mask;


            Texture2D tex0 = new Texture2D(targetTexture.width, targetTexture.height);
            tex0.ReadPixels(new UnityEngine.Rect(0, 0, tex0.width, tex0.height), 0, 0);
            tex0.Apply();
            var colors1 = tex0.GetPixels32();

            RenderTexture.active = targetTexture;


            Texture2D tex = new Texture2D(targetTexture.width, targetTexture.height);
            tex.ReadPixels(new UnityEngine.Rect(0, 0, tex.width, tex.height), 0, 0);
            var colors2 = tex.GetPixels32();
            tex.Apply();

            RenderTexture.active = old;

            int count1 = 0;
            int count2 = 0;
            int countOr = 0;
            int countAnd = 0;
            var s = new float[4];
            for (int y = 0; y < 144; ++y) {
                for (int x = 0; x < 256; ++x) {
                    var c1 = tex0.GetPixel(255-x, y);
                    var c2 = tex.GetPixel(x, y);

                    var b1 = c1.r >= 0.5f;
                    var b2 = c2.r > 0;

                    s[(b2 ? 2 : 0) + (b1 ? 1 : 0)] += 1;
                    if (b1) {
                        count1 += 1;
                    }

                    if (b2) {
                        count2 += 1;
                    }

                    if (b1 && b2) {
                        countAnd++;
                    }

                    if (b1 || b2) {
                        countOr++;
                    }

                }
            }


            //Debug.Log("CPU:" + countAnd + " " + countOr + " " + (countAnd / (countOr + 0.01f))+" val:" + count1 + " " + count2 + " " );
            Debug.Log("CPU:" + (countAnd / (countOr + 0.01f)) + " " + s[0] + " " + s[1] + " " + s[2] + " " + s[3] + " "  );
            */


            var data = new uint[4];
            iouBuffer.SetData(data);
            iouShader.SetTexture(iouKernerlHandle, "image", mask);
            iouShader.SetTexture(iouKernerlHandle, "image2", targetTexture);


            //TODO
            int w = 256;
            int h = 256;
            iouShader.Dispatch(iouKernerlHandle, w / 8, h / 8, 1);

            iouBuffer.GetData(data);

            float and = data[3] + 0.001f;
            uint onTexture = data[2]; //cloth texture
            uint onMask = data[1];//human mask

            float ior = and / (and + onTexture * skeletonManager.ikSettings.clothPenalty + onMask * skeletonManager.ikSettings.clothTooThinPenalty);

            ior = and / (and + onTexture + onMask) - onTexture / and * skeletonManager.ikSettings.clothPenalty - onMask / and * skeletonManager.ikSettings.clothTooThinPenalty;

            ior = and / (and + onTexture + onMask);
            float z1 = skeletonManager.ikSettings.clothPenalty;
            float z2 = skeletonManager.ikSettings.clothTooThinPenalty;
            //ior = ior - (z1 + z2) / (onTexture + onMask + 0.001f);

            ior = ior - z1 * (z2 * onMask + (1 - z2) * onTexture) / (onMask + onTexture + 0.001f);
            //ior = -z1 * (z2 * onMask + (1 - z2) * onTexture) / (onMask + onTexture + 0.001f);
            // ior = ior * z1 * (z2 * onMask + (1 - z2) * onTexture) / (onMask + onTexture + 0.001f);
            ior = and / (and + z1 * (z2 * onMask + (1 - z2) * onTexture) * 2);


            //Debug.Log("Shader:"+ior + " " + data[0] + " " + data[1] + " " + data[2] + " " + data[3]);

            // Debug.Log("Shader:" + ior + " def:" + and / (and + onTexture + onMask + 0.001f));
            //float ior = data[0] / (float)(data[1] + 0.01f);
            //Debug.Log("Shader:"+data[0] + " " + data[1] + " " + ior + " val: " + data[2] + " " + data[3]);
            return ior;
        }

        Texture2D toTexture2D(RenderTexture rTex) {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            return tex;
        }

        byte[] SaveTexturePNG(RenderTexture rTex, string fileName) {
            
            
            byte[] bytes = toTexture2D(rTex).EncodeToPNG();
            //File.WriteAllBytes(fileName, bytes);
            return bytes;
        }


        private byte[] RenderFullImage(string name, params Camera[] cams) {
            // Создаем RenderTexture с размерами экрана
            RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);

            //var cam = GameObject.Find("Skeleton Camera").GetComponent<Camera>();
            // Устанавливаем RenderTexture в качестве цели для рендеринга камеры
            //cam.targetTexture = rt;

            // Создаем Texture2D с размерами экрана
            Texture2D screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

            foreach(var c in cams) {
                c.targetTexture = rt;
                c.Render();
            }

            // Рендерим кадр в RenderTexture
            //cam.Render();

            // Активируем RenderTexture
            RenderTexture.active = rt;

            // Копируем пиксели из RenderTexture в Texture2D
            screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);

            // Применяем изменения в Texture2D
            screenShot.Apply();

            foreach (var c in cams) {
                c.targetTexture = null;
            }

            RenderTexture.active = null;
            var bytes = SaveTexturePNG(rt, name);
            // Освобождаем память от RenderTexture
            Destroy(rt);
            return bytes;
        }

        public void Experiment() {

            int idx = (int)Time.realtimeSinceStartup;

            var ui = GameObject.Find("UI");
        //    ui.SetActive(false);

            var cam2 = GameObject.Find("Skeleton Camera").GetComponent<Camera>();
            var cam1 = GameObject.Find("MP Camera").GetComponent<Camera>();
           // cam2.clearFlags = CameraClearFlags.Color;


            //var cam = GameObject.Find("MP Camera").GetComponent<Camera>();
            var oldMask = cam2.cullingMask;
            
            var image1 = RenderFullImage($"d:\\mpimages\\cloth-full_{idx}.png", cam1, cam2);
            //ScreenCapture.CaptureScreenshot("d:\\mpimages\\cloth-full2_{idx}.png");

            var clearFlag = cam2.clearFlags;
            cam2.clearFlags = CameraClearFlags.Color;
            //cam2.cullingMask = layermask;
          //  cam2.SetReplacementShader(whiteMaskShader, null);
            cam2.cullingMask = clothCamera.cullingMask;// layermask;
            var oldTexture = avatarManager.transparentMaterial.mainTexture;
            avatarManager.transparentMaterial.mainTexture = Texture2D.blackTexture;
            var image2 = RenderFullImage($"d:\\mpimages\\cloth-mask_{idx}.png", cam2);
            avatarManager.transparentMaterial.mainTexture = oldTexture;

            ui.SetActive(true);
            cam2.clearFlags = clearFlag;
            cam2.cullingMask = oldMask;
            cam2.SetReplacementShader(null, null);


            StartCoroutine(CallSBProcessing(image1, image2));

            return;

            //  SaveTexturePNG(RenderTexture.active, "d:\\cloth-full.png");
            var old = RenderTexture.active;
            RenderTexture.active = clothCamera.targetTexture;
            clothCamera.Render();
            RenderTexture.active = old;
            SaveTexturePNG(clothCamera.targetTexture, $"d:\\mpimages\\cloth-mask2_{idx}.png");
            //SetClothTexture(clothCamera.targetTexture);
        }

        class AcceptAllCertificatesSignedWithASpecificPublicKey : CertificateHandler {
            // Encoded RSAPublicKey
            private static string PUB_KEY = "somepublickey";

            protected override bool ValidateCertificate(byte[] certificateData) {
                //X509Certificate2 certificate = new X509Certificate2(certificateData);

                //string pk = certificate.GetPublicKeyString();
                //Debug.Log(pk.ToLower());
                //return pk.Equals(PUB_KEY);
                return true; // pk.Equals(PUB_KEY);
            }
        }

        private IEnumerator CallSBProcessing(byte[] image1Bytes, byte[] image2Bytes) {
            // Create form to send data
            WWWForm form = new WWWForm();
            form.AddBinaryData("image1", image1Bytes);
            form.AddBinaryData("image2", image2Bytes);

            // Send data to REST API

            using (UnityWebRequest www = UnityWebRequest.Post("http://10.8.0.204:5002/process", form)) {

                Debug.Log("Sent images");
                yield return www.SendWebRequest();
                // Check for errors
                if (www.result != UnityWebRequest.Result.Success) {
                    Debug.Log(www.error);
                } else {
                    // Get response data
                    byte[] responseData = www.downloadHandler.data;
                    Debug.Log("Received images");

                    // Create new texture from response data
                //    Texture2D newImage = new Texture2D(2, 2);
                   // newImage.LoadImage(responseData);

                    // Use newImage here
                }
            }
        }

        public void Experiment2() {

            int idx = (int)Time.realtimeSinceStartup;

            var ui = GameObject.Find("UI");
            //    ui.SetActive(false);

            var cam2 = GameObject.Find("Skeleton Camera").GetComponent<Camera>();
            var cam1 = GameObject.Find("MP Camera").GetComponent<Camera>();


            //var cam = GameObject.Find("MP Camera").GetComponent<Camera>();
            var oldMask = cam2.cullingMask;

            RenderFullImage($"d:\\mpimages\\cloth-full_{idx}.png", cam1, cam2);
            //ScreenCapture.CaptureScreenshot("d:\\mpimages\\cloth-full2_{idx}.png");


            //cam2.cullingMask = layermask;
            //  cam2.SetReplacementShader(whiteMaskShader, null);
            cam2.cullingMask = clothCamera.cullingMask;// layermask;
            var oldTexture = avatarManager.transparentMaterial.mainTexture;
            avatarManager.transparentMaterial.mainTexture = Texture2D.blackTexture;
            RenderFullImage($"d:\\mpimages\\cloth-mask_{idx}.png", cam2);
            avatarManager.transparentMaterial.mainTexture = oldTexture;

            ui.SetActive(true);
            cam2.cullingMask = oldMask;
            cam2.SetReplacementShader(null, null);
            return;

            //  SaveTexturePNG(RenderTexture.active, "d:\\cloth-full.png");
            var old = RenderTexture.active;
            RenderTexture.active = clothCamera.targetTexture;
            clothCamera.Render();
            RenderTexture.active = old;
            SaveTexturePNG(clothCamera.targetTexture, $"d:\\mpimages\\cloth-mask2_{idx}.png");
            //SetClothTexture(clothCamera.targetTexture);
        }

    }
}
