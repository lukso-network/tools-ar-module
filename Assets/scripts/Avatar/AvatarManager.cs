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
    public WebCamScreenController cameraSurface;

    private int testModelIdx = -1;

    // Start is called before the first frame update
    void Start() {

        skeletonManager.newPoseEvent += UpdateSkeleton;

        LoadNextTestModel();

    }

    public async void LoadGltf(string url, bool replaceModel) {
    
        var model = await GltfGlbLoader.LoadUrl(url);
        if (model != null) {

            if (replaceModel) {
                RemoveAllModels();
            }
            AddModel(model);
        }
    }

    public void RemoveAllModels() {
        foreach (Transform child in modelRoot.transform) {
            GameObject.Destroy(child.gameObject);
        }


        avatars = new List<Assets.Avatar>();

    }

    public void LoadNextTestModel() {
        if (testSpawner.transform.childCount == 0) {
            return;
        }

        RemoveAllModels();

        testModelIdx = (testModelIdx + 1) % testSpawner.transform.childCount;


        var testObj = testSpawner.transform.GetChild(testModelIdx);
        var md = testObj.GetComponent<ModelDescriptor>();
        
        if( md.type == skeletonManager.avatarType) {
            foreach (Transform child in testObj.transform) {
                var cpy = GameObject.Instantiate(child.gameObject, modelRoot.transform);
                AddModel(cpy);
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

        model.transform.position = Vector3.zero;
        model.transform.localScale = Vector3.one;
        model.transform.rotation = Quaternion.identity;

        foreach (Transform t in children) {//model.transform.GetComponentInChildren<Transform>()) {
          //  t.transform.parent = model.transform;
        }

    }


    public void AddModel(GameObject obj) {


        obj.transform.parent = modelRoot.transform;
        skeletonManager.controller.RestoreSkeleton();

        Utils.AddMissedJoints(skeletonManager.controller.obj, obj);
        Utils.PreparePivots(obj);
       
        var controller = new Assets.Avatar(obj, skeletonManager.Skeleton);
        float scale = skeletonManager.controller.GetRelativeBonesScale(controller);

        obj.transform.localScale /= scale;
        controller.InitJoints();
        avatars.Add(controller);
        SplitModel(obj);
        obj.SetActive(false);
    }

    public void ShowAvatar(bool value) {
        avatars.ForEach(a => a.obj.SetActive(value));
    }

    public bool IsAvatarsVisible() {
        return avatars.Any(a => a.obj.active);
    }

    public void UpdateSkeleton() {
        foreach (var avatar in avatars) {
            avatar.obj.SetActive(true);
            var pos = avatar.obj.transform.localPosition;
            avatar.CopyToLocalFromGlobal(skeletonManager.controller, skinScaler, skeletonManager.ikSettings.resizeBones);
            avatar.obj.transform.localPosition = pos;
        }
    }
}
