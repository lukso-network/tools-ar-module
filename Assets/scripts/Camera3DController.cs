using Mediapipe.Unity;
using System;
using System.Collections;
using UnityEngine;

namespace Lukso
{
  public class Camera3DController : MonoBehaviour {

    private int width = -1;
    private int height = -1;
    private RotationAngle rotation = RotationAngle.Rotation0;
    public Vector2 ScreenSize;

    [SerializeField] private Camera camera;
    [SerializeField] private GameObject screenPlane;

    private float cameraScale = 1;
    public float CameraScale {
      get => cameraScale;
      set {
        cameraScale = Mathf.Clamp(value, 0.1f, 10f);
        UpdateCamera();
      }
    }

    public float TextureAspect => (float)width / height;

    // Use this for initialization
    void Start() {
    CameraScale = 1;
    }

    // Update is called once per frame
    void Update() {

    }

    internal void UpdateCamera(TextureFrame textureFrame, RotationAngle rotation, ImageSource imageSource) {
      if (textureFrame.width == width && textureFrame.height == height && rotation == this.rotation) {
        //return;
      }

      width = textureFrame.width;
      height = textureFrame.height;
      this.rotation = rotation;

      UpdateSize(width, height, (int)this.rotation, imageSource.isFrontFacing);
    }


    private void UpdateSize(int width, int height, int angle, bool frontCamera) {
      //Debug.Log("******* width:" + width + ", angle:" + angle);
      if (angle == 90 || angle == 270) {
        var temp = width;
        width = height;
        height = temp;
      }

      this.width = width;
      this.height = height;


     // Quaternion rot = Quaternion.Euler(0, 0, angle);
     // Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, rot, frontCamera ? new Vector3(-1, 1, 1) : Vector3.one);

     // GetComponent<Renderer>().material.SetMatrix("_TextureRotation", m);
      //transform.localRotation = baseRotation * Quaternion.Euler(0, angle, 0);

      UpdateCamera();
    }

    private void UpdateCamera() {

      float refHeight = 4 * cameraScale;
      screenPlane.transform.localScale = new Vector3((float)width / height * refHeight, refHeight, 1);

      float texAspect = (float)width / height;

      float dist = 0;
      var tg = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2);

      if (texAspect < camera.aspect) {
        dist = screenPlane.transform.localScale.y / tg / 2;
        ScreenSize = new Vector2(texAspect / camera.aspect, 1);
      } else {
        dist = screenPlane.transform.localScale.x / camera.aspect / tg / 2;
        ScreenSize = new Vector2(1, camera.aspect / texAspect);
      }

      var pos = camera.transform.position;
      pos.z = -dist;

      camera.transform.position = pos;
    }

  }
}
