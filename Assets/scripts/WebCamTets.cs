using System.Collections;
using UnityEngine;

namespace Assets.scripts
{
    public class WebCamTets : MonoBehaviour
    {
        public WebCamTexture webcamTexture;
        public Quaternion baseRotation;
        void Start() {
            webcamTexture = new WebCamTexture();
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.mainTexture = webcamTexture;
            baseRotation = transform.rotation;
            webcamTexture.Play();
        }

        void Update() {
            transform.rotation = baseRotation * Quaternion.AngleAxis(webcamTexture.videoRotationAngle, Vector3.up);
        }
    }
}