using Lukso;
using Mediapipe.Unity;
using Mediapipe.Unity.SkeletonTracking;
using System.Collections;
using UnityEngine;

namespace Assets.scripts.Avatar
{
  public class TransparentMaterialRenderer : MonoBehaviour
  {

    private Camera3DController cam3d;
    private SkeletonTrackingGraph skelGraph;
    private Renderer renderer;
    private AvatarManager avatarManager;
    private Material oldMaterial;
    private Material newMaterial;

    // Use this for initialization
    void Start() {

      avatarManager = FindObjectOfType<AvatarManager>();
      cam3d = FindObjectOfType<Camera3DController>();

      skelGraph = FindObjectOfType<SkeletonTrackingGraph>();
      skelGraph.newFrameRendered += OnNewFrameRendered;

      renderer = transform.parent.GetComponentInChildren<Renderer>();
      oldMaterial = renderer.material;
      newMaterial = FindObjectOfType<AvatarManager>().transparentMaterial;
      renderer.material = newMaterial;
    }

    private void OnDestroy() {
      if (skelGraph != null) {
        skelGraph.newFrameRendered -= OnNewFrameRendered;
      }
    }

    private void OnNewFrameRendered(Texture2D texture2) {
      if (!gameObject.activeSelf || !enabled) {
        renderer.material = oldMaterial;
        return;
      }

      //var imageSource = ImageSourceProvider.ImageSource;
      //var texture = imageSource.GetCurrentTexture();
      var texture = texture2;

      if (texture == null) {
        gameObject.SetActive(false);
        return;
      }
      
      renderer.material = newMaterial;
      
      //TODO start it only one time
      texture.wrapMode = TextureWrapMode.Clamp;

      float w = cam3d.ScreenSize.x;
      float h = cam3d.ScreenSize.y;
      

      var im = ImageSourceProvider.ImageSource;
      var angle = (int)im.rotation;

      var mat = new Matrix4x4(new Vector4(1 / w, 0, 0, 0), new Vector4(0, 1 / h, 0, 0), Vector3.zero, new Vector4((w - 1) / 2 / w, (h - 1) / 2 / h, 0, 1));
      
      Quaternion rot = Quaternion.Euler(0, 0, angle);
       Matrix4x4 m = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0)) * Matrix4x4.Rotate(rot) * Matrix4x4.Scale(im.isFrontFacing ? new Vector3(-1, 1, 1) : Vector3.one) * Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));

      //   m[3 * 4 + 0] = 1;
#if !UNITY_EDITOR
     //   m[3*4 + 0] = 1;
#endif
      mat = m * mat;

      renderer.material.mainTexture = texture;
      renderer.material.SetMatrix("_TextureMat", mat);

      float rootScale = renderer.transform.lossyScale.x;
      renderer.material.SetFloat("_ShrinkSize", avatarManager.transparentBodyShrinkAmount / rootScale);


    }
  }


}
