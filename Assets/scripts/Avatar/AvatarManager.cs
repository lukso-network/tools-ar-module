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
    private List<Assets.Avatar> avatars = new List<Assets.Avatar>();
    public DMBTDemoManager skeletonManager;
    public Material transparentMaterial;

    public GameObject testSpawner;
    public GameObject modelRoot;
    public Vector3 skinScaler = Vector3.one;

    private int testModelIdx = -1;

    // Start is called before the first frame update
    void Start() {

        skeletonManager.newPoseEvent += UpdateSkeleton;

        LoadNextTestModel();

    }

    public void LoadNextTestModel() {
        if (testSpawner.transform.childCount == 0) {
            return;
        }

        foreach (Transform child in modelRoot.transform) {
            GameObject.Destroy(child.gameObject);
        }

        testModelIdx = (testModelIdx + 1) % testSpawner.transform.childCount;


        avatars = new List<Assets.Avatar>();

        var testObj = testSpawner.transform.GetChild(testModelIdx);
        var md = testObj.GetComponent<ModelDescriptor>();
        
        if( md.type == skeletonManager.avatarType) {
            foreach (Transform child in testObj.transform) {
                var cpy = GameObject.Instantiate(child.gameObject, modelRoot.transform);
                AddModel(cpy);

                SplitModel(cpy);
            }
        }

    }

    private void SplitModel(GameObject model) {
        //var root = model.transform.parent;
        var root = new GameObject("LinearRoot:"+model.name);
        root.transform.parent = model.transform.parent;

        model.transform.parent = root.transform;
        List<Transform> children = new List<Transform>();
        Utils.GetAllChildrenDSF(model.transform, children);
        foreach(Transform t in children){//model.transform.GetComponentInChildren<Transform>()) {
            t.transform.parent = root.transform;
        }
    }


    public void AddModel(GameObject obj) {
        Utils.AddMissedJoints(skeletonManager.controller.obj, obj);
        Utils.PreparePivots(obj);
        var controller = new Assets.Avatar(obj, null);
        avatars.Add(controller);
    }

    public void ShowAvatar(bool value) {
        avatars.ForEach(a => a.obj.SetActive(value));
    }

    public bool IsAvatarsVisible() {
        return avatars.Any(a => a.obj.active);
    }

    public void UpdateSkeleton() {
        foreach (var avatar in avatars) {
            var pos = avatar.obj.transform.localPosition;
            avatar.CopyToLocalFromGlobal(skeletonManager.controller, skinScaler, skeletonManager.ikSettings.resizeBones);
            avatar.obj.transform.localPosition = pos;
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
