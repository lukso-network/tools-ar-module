using System.Collections;
using UnityEngine;

namespace Assets.scripts.Avatar
{
    public class TransparentMaterialRenderer : MonoBehaviour
    {
        private WebCamScreenController webScreenPlane;
        private Renderer renderer;

        // Use this for initialization
        void Start() {
            webScreenPlane = FindObjectOfType<WebCamScreenController>();
            webScreenPlane.newFrameRendered += OnNewFrameRendered;

            renderer = GetComponentInChildren<Renderer>();
            renderer.material = FindObjectOfType<AvatarManager>().transparentMaterial;
        }

        void OnDestroy() {
            webScreenPlane.newFrameRendered -= OnNewFrameRendered;
        }

        private void OnNewFrameRendered(Texture2D texture) {
            if (!gameObject.activeSelf) {
                return;
            }
            texture.wrapMode = TextureWrapMode.Clamp;

            float w = webScreenPlane.ScreenSize.x;
            float h = webScreenPlane.ScreenSize.y;
            var mat = new Matrix4x4(new Vector4(1 / w, 0, 0, 0), new Vector4(0, 1 / h, 0, 0), Vector3.zero, new Vector4((w - 1) / 2 / w, (h - 1) / 2 / h, 0, 1));
            //var mat = new Matrix4x4(new Vector4(-1 / w, 0, 0, 0), new Vector4(0, 1 / h, 0, 0), Vector3.zero, new Vector4(1-(w - 1) / 2 / w, (h - 1) / 2 / h, 0, 1));
            //mat = mat * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 90), Vector3.one);

            Quaternion rot = Quaternion.Euler(0, 0, webScreenPlane.VideoAngle);
            // Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, rot, webScreenPlane.IsFrontCamera() ? new Vector3(-1, 1, 1) : Vector3.one);


            Matrix4x4 m = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0)) * Matrix4x4.Rotate(rot) * Matrix4x4.Scale(webScreenPlane.IsFrontCamera() ? new Vector3(-1, 1, 1) : Vector3.one) *  Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));

            //   m[3 * 4 + 0] = 1;
#if !UNITY_EDITOR
     //   m[3*4 + 0] = 1;
#endif
            mat = m * mat;

            renderer.material.mainTexture = texture;
            renderer.material.SetMatrix("_TextureMat", mat);


        }
    }


}