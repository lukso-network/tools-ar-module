using DeepMotion.DMBTDemo;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Demo.Scripts;

public class AvatarManager : MonoBehaviour
{
    public string avatarType = "female-normal";
    private List<Assets.Avatar> avatars = new List<Assets.Avatar>();
    public DMBTDemoManager skeletonManager;
    public Material transparentMaterial;
     
    void Awake() {
        skeletonManager.avatarType = avatarType;
    }

    // Start is called before the first frame update
    void Start()
    {

        skeletonManager.newPoseEvent += UpdateSkeleton;
            
        avatars = new List<Assets.Avatar>();

        var sourceObj = skeletonManager.controller.obj;
        foreach (Transform t in transform) {

            if (!t.gameObject.activeSelf) {
                continue;
            }
            var descriptors = t.GetComponentsInChildren<ModelDescriptor>();

            var found = false;
            foreach (var md in descriptors) { 
                if (md.type == avatarType) {
                    Utils.AddMissedJoints(sourceObj, md.gameObject);
                    Utils.PreparePivots(md.gameObject);
                    var controller = new Assets.Avatar(md.gameObject, null);
                    avatars.Add(controller);
                    found = true;
                }
            }


            if (!found) {
                t.gameObject.SetActive(false);
            }
        }
    }

    public void ShowAvatar(bool value) {
        avatars.ForEach(a => a.obj.SetActive(value));
    }

    public bool IsAvatarsVisible() {
        return avatars.Any(a => a.obj.active);
    }

    public void UpdateSkeleton() {
        foreach (var avatar in avatars) {
            avatar.CopyRotationAndPositionFromAvatar(skeletonManager.controller);
        }

        UpdateTransparentBody();
    }

    private void UpdateTransparentBody() {
        /*
        var image = skeletonManager.videoDisplay.GetComponent<RawImage>();
        
        if (image == null || image.texture == null) {
            return;
        }

        bodyRenderer.material.mainTexture = image.texture;

        if (Screen.width == scrWidth && Screen.height == scrHeight) {
         //   return;
        }
        scrWidth = Screen.width;
        scrHeight = Screen.height;

        var rectTransform = skeletonManager.videoDisplay.GetComponent<RectTransform>();

        float w1 = Screen.width;
        float h1 = Screen.height;
        float w2 = rectTransform.rect.width;
        float h2 = rectTransform.rect.height;
        float dx = rectTransform.offsetMin.x;
        float dy = rectTransform.offsetMin.y;

        Matrix4x4 mat = new Matrix4x4(new Vector4(w1 / w2, 0, 0, 0), new Vector4(0, h1 / h2, 0, 0), Vector3.zero, new Vector4(-dx / w2, -dy / h2, 0, 1));


        float angle = 0;
        if (image.texture is WebCamTexture) {
            angle = ((WebCamTexture)image.texture).videoRotationAngle;
        }
        
        var rot = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0)) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle)) * Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));
        mat = rot * mat;
        bodyRenderer.material.SetMatrix("_TextureMat", mat);
        */
    }

}
