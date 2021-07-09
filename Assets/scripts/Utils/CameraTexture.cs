using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraTexture : MonoBehaviour
{
    public RawImage camera_texture;
    private RawImage rawImage;
    // Start is called before the first frame update
    void Start() {
        rawImage = GetComponent<RawImage>();
    }

    // Update is called once per frame
    void Update() {
        rawImage.texture = camera_texture.texture;
    }
}
