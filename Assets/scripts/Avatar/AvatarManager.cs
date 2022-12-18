using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Assets.Demo.Scripts;
using Lukso;
using Skeleton = Lukso.Skeleton;
using VRM;
using System.Reflection;
using Lukso;
using Avatar = Lukso.Avatar;
using System.Collections;

public class AvatarManager : MonoBehaviour {
    private List<Avatar> avatars = new List<Avatar>();
    public SkeletonManager skeletonManager;
    public DMBTDemoManager posManager;
    public Material transparentMaterial;

    public GameObject testSpawner;
    public GameObject modelRoot;
    public Transform transpBodyRoot;
    public Vector3 skinScaler = Vector3.one;
    public bool ShowTransparentBody;
    public string avatarLayerMask;
    public Shader vrmShader;
    public bool replaceVRMMaterial;
    public bool vrmClothOnly = false;
    public Material discardMaterial;

    [Range(-0.01f, 0.01f)]
    public float transparentBodyShrinkAmount = 0.04f;

    private bool skeletonJustAppeared = true;

    private int testModelIdx = -1;

    private List<VRMSpringBone> vrmPhysicsObjects = new List<VRMSpringBone>();

    // Start is called before the first frame update
    void Start() {
        posManager.newPoseEvent += UpdateSkeleton;
    }

    public async void Load(string url, bool replaceModel) {
        if (url.ToLower().EndsWith("glb")) {
            LoadGltf(url, replaceModel);
        }

        if (url.ToLower().EndsWith("vrm")) {
            LoadVrm(url, replaceModel);
        }
    }

    private async void LoadGltf(string url, bool replaceModel) {

        var model = await GltfGlbLoader.LoadUrl(url);
        if (model != null) {

            if (replaceModel) {
                RemoveAllModels(false);
            }
            AddModel(model);
        }
    }

    void LateUpdate() {
        var instanceMethod = typeof(VRMSpringBone).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (var b in vrmPhysicsObjects) {
            instanceMethod.Invoke(b, null);
        }
    }

    private void SetKeyword(Material material, bool value, string keyword) {
        if (value) {
            material.EnableKeyword(keyword);
        } else {
            material.DisableKeyword(keyword);
        }
    }

    private void ChangeMaterial(Material material) {

        int srcBlend = material.GetInt("_SrcBlend");
        int dstBlend = material.GetInt("_DstBlend");
        int zWrite = material.GetInt("_ZWrite");
        float cullMode = material.GetFloat("_CullMode");

        var enabledKeywords = material.shaderKeywords.Select(x => (x, material.IsKeywordEnabled(x))).ToList();

      //  bool alphaOn = material.IsKeywordEnabled("_ALPHATEST_ON");
     //   bool alphaBlendOn = material.IsKeywordEnabled("_ALPHABLEND_ON");
      //  bool alphaPreMult = material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON");
        var rq = material.renderQueue;

        material.shader = vrmShader;
        material.SetInt("_SrcBlend", srcBlend);
        material.SetInt("_DstBlend", dstBlend);
        material.SetInt("_ZWrite", zWrite);
        material.SetFloat("_CullMode", cullMode);
       // SetKeyword(material, alphaOn, "_ALPHATEST_ON");
      //  SetKeyword(material, alphaBlendOn, "_ALPHABLEND_ON");
      //  SetKeyword(material, alphaPreMult, "_ALPHAPREMULTIPLY_ON");


        enabledKeywords.ForEach(x=> SetKeyword(material, x.Item2, x.Item1));

        material.renderQueue = rq;

        /*
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        */
    }

    private async void LoadVrm(string url, bool replaceModel) {
        var loaded = await VrmUtility.LoadAsync(url);
        loaded.ShowMeshes();
        //loaded.EnableUpdateWhenOffscreen();
        var model = loaded.gameObject;


        if (model != null) {
            if (replaceVRMMaterial) {
                if (vrmShader == null) {
                    vrmShader = Shader.Find("Standard");
                }

                var renderers = model.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers) {
                    foreach (var m in r.materials) {
                        ChangeMaterial(m);
                    }
                    
                    if (r is SkinnedMeshRenderer) {
                        ((SkinnedMeshRenderer)r).sharedMesh.RecalculateBounds();
                        ((SkinnedMeshRenderer)r).sharedMesh.RecalculateNormals();
                        ((SkinnedMeshRenderer)r).sharedMesh.RecalculateTangents();
                        //((SkinnedMeshRenderer)r).enabled = false;
                    }
                }
            }
            if (replaceModel) {
                RemoveAllModels(false);
            }
            AddModel(model, true);
            AddFaceController(model);

            if (vrmClothOnly) {
                RemoveVRMBody(model);
            }
        }

    }

    private void AddFaceController(GameObject model) {
        model.AddComponent<FaceAnimationController>();
    }

    private void RemoveVRMBody(GameObject model) {
        foreach(Transform child in model.transform.parent) {
            if (child.name.StartsWith("Face") || child.name.StartsWith("Hair")) {
                child.gameObject.SetActive(false);
            }
        }

        var body = model.transform.parent.Find("Body");
        if (body != null) {
            var r = body.GetComponent<Renderer>();


            Material[] matArray = r.materials;
            for (int i = 0; i < matArray.Length; ++i) {
                var name = matArray[i].name.ToLower();
                if (name.Contains("ear") || !name.Contains("cloth")) {
                    matArray[i] = discardMaterial;
                }
            }
            r.materials = matArray;
            

        }
    }

    public void RemoveAllModels(bool clearUnused) {
        foreach (Transform child in modelRoot.transform) {
            GameObject.Destroy(child.gameObject);
        }
        vrmPhysicsObjects.Clear();


        avatars = new List<Avatar>();

        if (clearUnused) {
            CleanUpUnusedSkeletons();
        }

    }

    public void LoadNextTestModel() {
        if (testSpawner.transform.childCount == 0) {
            return;
        }

        RemoveAllModels(false);

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

        var layer = LayerMask.NameToLayer(avatarLayerMask);
        foreach (Transform t in children) {
            t.gameObject.layer = layer;
            t.transform.parent = model.transform.parent;
            var skin = t.gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skin != null) {
                // its required because skinned mesh renderer can have incorrect bounds
                skin.updateWhenOffscreen = true;
            }
        }

    }

    private void DeletePhysicsObjects(GameObject obj) {
        foreach (var c in obj.GetComponentsInChildren(typeof(VRMSpringBone), true)) {
            Destroy(c);
        }
        foreach (var c in obj.GetComponentsInChildren(typeof(VRMSpringBoneColliderGroup), true)) {
            Destroy(c);
        }
    }

    public void AddModel(GameObject obj, bool unique_avatar = false) {
        var root = new GameObject("LinearRoot:" + obj.name);
        root.transform.parent = modelRoot.transform;

        obj.transform.parent = root.transform;

        var controllerAvatar = skeletonManager.GetOrCreateControllerAvatar(obj, unique_avatar);
        if (controllerAvatar == null) {
            GameObject.Destroy(obj);
            return;
        }

        bool enablePhysics = posManager.UsePhysics;
        vrmPhysicsObjects.Clear();
        DeletePhysicsObjects(obj);
        if (!enablePhysics) {
            DeletePhysicsObjects(controllerAvatar.obj);
        } else {
            foreach (VRMSpringBone c in controllerAvatar.obj.GetComponentsInChildren(typeof(VRMSpringBone), true)) {
                vrmPhysicsObjects.Add(c);
            }
        }



        controllerAvatar.RestoreSkeleton();

        var curController = new Avatar(root, controllerAvatar.Skeleton);
        float scale = controllerAvatar.GetRelativeBonesScale(curController);

        obj.transform.localScale /= scale;

        //TODO check it. 
        //curController.InitJoints();
        avatars.Add(curController);

        DeletePhysicsObjects(obj);

        SplitModel(obj);

        root.SetActive(true);

        CleanUpUnusedSkeletons();

        if (!IsTransparent(obj)) {
            AddTransparentBody(root, controllerAvatar);
        }
    }

    private bool IsTransparent(GameObject obj) {
        return obj.GetComponentInChildren<TransparentMaterialRenderer>(true) != null;
    }

    private void AddTransparentBody(GameObject obj, Avatar controllerAvatar) {
        var name = controllerAvatar.Skeleton.Name;
        var transp_name = $"{name}_transparent";

        if (avatars.Find(x => x.obj.name.Contains(transp_name)) != null) {
            return;
        }

        //TODO temporary way to find male or female
        bool female = false;
        foreach (Transform t in obj.transform) {
            if (t.name.ToLower().Contains("alice")) {
                female = true;
                break;
            }
        }

        var body = transpBodyRoot.Find(female ? (name + "_female") : name);
        if (body == null) {
            return;
        }

        var transpObj = GameObject.Instantiate(body.gameObject);
        transpObj.name = transp_name;
        AddModel(transpObj);
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

    private void UpdateTransparentBody() {
        foreach (var avatar in avatars) {

            if (IsTransparent(avatar.obj)) {
                avatar.obj.SetActive(ShowTransparentBody);
            }

        }
    }

    // Required for cloth size estimation
    public void SetSkinRecalulation(bool onRender) {
        foreach (var avatar in avatars) {
            var sm = avatar.obj.GetComponentInChildren<SkinnedMeshRenderer>();
            if (sm != null) {
                sm.forceMatrixRecalculationPerRender = true;
            }
        }
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

        UpdateTransparentBody();

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
