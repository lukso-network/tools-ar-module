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
        List<Transform> children = new List<Transform>();
        Utils.GetAllChildrenDSF(model.transform, children);
        foreach(Transform t in children){//model.transform.GetComponentInChildren<Transform>()) {
            t.transform.parent = model.transform.parent;
        }
    }


    public void AddModel(GameObject obj) {
        var root = new GameObject("LinearRoot:" + obj.name);
        root.transform.parent = modelRoot.transform;

        obj.transform.parent = root.transform;
        skeletonManager.controller.RestoreSkeleton();

        Utils.AddMissedJoints(skeletonManager.controller.obj, obj);
        Utils.PreparePivots(obj);
       
        var controller = new Assets.Avatar(root, skeletonManager.Skeleton);
        float scale = skeletonManager.controller.GetRelativeBonesScale(controller);

        obj.transform.localScale /= scale;
        controller.InitJoints();
        avatars.Add(controller);
        SplitModel(obj);

        // remove from view
        obj.transform.position = new Vector3(float.PositiveInfinity, 0, 0);
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
    }
}
