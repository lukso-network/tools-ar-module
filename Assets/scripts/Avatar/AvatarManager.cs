using DeepMotion.DMBTDemo;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Demo.Scripts;
using Assets;

public class AvatarManager : MonoBehaviour
{
    private List<Assets.Avatar> avatars = new List<Assets.Avatar>();
    public SkeletonManager skeletonManager;
    public DMBTDemoManager posManager;
    public Material transparentMaterial;

    public GameObject testSpawner;
    public GameObject modelRoot;
    public Vector3 skinScaler = Vector3.one;
    public WebCamScreenController cameraSurface;

    private bool skeletonJustAppeared = true;

    private int testModelIdx = -1;

    // Start is called before the first frame update
    void Start() {

        posManager.newPoseEvent += UpdateSkeleton;

      //  LoadNextTestModel();

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

        foreach (Transform child in testObj.transform) {
            var cpy = GameObject.Instantiate(child.gameObject, modelRoot.transform);
            AddModel(cpy);
        }
        
    }

    private void SplitModel(GameObject model) {
        List<Transform> children = new List<Transform>();
        Utils.GetAllChildrenDSF(model.transform, children);
        foreach (Transform t in children) {
            t.transform.parent = model.transform.parent;
            var skin = t.gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skin != null) {
                // its required because skinned mesh renderer can have incorrect bounds
                skin.updateWhenOffscreen = true;
            }
        }

    }
    
    public void AddModel(GameObject obj) {
        var root = new GameObject("LinearRoot:" + obj.name);
        root.transform.parent = modelRoot.transform;

        obj.transform.parent = root.transform;

        var controllerAvatar = skeletonManager.GetOrCreateControllerAvatar(obj);
        if (controllerAvatar == null) {
            return;
        }
        controllerAvatar.RestoreSkeleton();

        var curController = new Assets.Avatar(root, controllerAvatar.Skeleton);
        float scale = controllerAvatar.GetRelativeBonesScale(curController);

        obj.transform.localScale /= scale;

        //TODO check it. 
        curController.InitJoints();
        avatars.Add(curController);
        SplitModel(obj);

        root.SetActive(true);

        CleanUpUnusedSkeletons();
    }

    public void ShowAvatar(bool value) {
        avatars.ForEach(a => a.obj.SetActive(value));
    }

    public bool IsAvatarsVisible() {
        return avatars.Any(a => a.obj.active);
    }

    private void CleanUpUnusedSkeletons() {
        var usedSkeletonTypes = avatars.Select(a => a.Skeleton.Name).Distinct().ToArray();
        skeletonManager.RemoveNotInList(usedSkeletonTypes);
    }

    public void UpdateSkeleton(bool skeletonExist) {

        if (!skeletonExist) {

            foreach (var avatar in avatars) {
                avatar.obj.SetActive(false);
            }
            skeletonJustAppeared = true;
            return;
        }

        if (skeletonJustAppeared) {
            foreach (var avatar in avatars) {
                avatar.obj.SetActive(true);
            }
            skeletonJustAppeared = false;
        }
         
        foreach (var avatar in avatars) {

            var pos = avatar.obj.transform.localPosition;

            var controller = skeletonManager.GetController(avatar.Skeleton);
            if (controller != null) {
                avatar.CopyToLocalFromGlobal(controller, skinScaler, skeletonManager.ikSettings.resizeBones);
                avatar.obj.transform.localPosition = pos;
            }
        }
    }
}
