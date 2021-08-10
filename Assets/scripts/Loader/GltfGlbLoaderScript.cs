using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;

public class GltfGlbLoaderScript : MonoBehaviour
{
    private GameObject currentlyDisplayedModel;

    // Start is called before the first frame update
    async void Start()
    {
        //MessageFromAndroid("https://github.com/JeneaVranceanu/temporary_repo/raw/main/texas_hat.gltf");
        //MessageFromAndroid("https://github.com/JeneaVranceanu/temporary_repo/raw/main/leather_jacket.glb");
        //MessageFromAndroid("file://d:/projects/blender/body_turbosq/gltfexport/medic.glb");


    }

    async void MessageFromAndroid(String message) {
        LoadUrl(message);
    }

    public async void LoadUrl(String message) {
        var gltf = new GltfImport();
        var success = await gltf.Load(message);

        if (success) {
            // Here you can customize the post-loading behavior

            GameObject oldModel = currentlyDisplayedModel;
            Quaternion rotation = new Quaternion(0, 180, 0, 1);

            if (oldModel != null) {
                rotation = oldModel.transform.rotation;
            } 

            currentlyDisplayedModel = new GameObject("Instance 1");
            currentlyDisplayedModel.AddComponent<ConstantRotationScript>();
            
            gltf.InstantiateGltf(currentlyDisplayedModel.transform);
            currentlyDisplayedModel.transform.position = currentlyDisplayedModel.transform.position + new Vector3(0, -0.25f, 3);
            
            if (rotation != null) {
                currentlyDisplayedModel.transform.Rotate (rotation.x, rotation.y, rotation.z);
            } else {
                currentlyDisplayedModel.transform.Rotate (0, 180, 0);
            }

            ResizeMeshToUnit(currentlyDisplayedModel);

            if (oldModel != null) {
                Destroy(oldModel);
            }
        } else {
            Debug.LogError("Loading glTF failed!");
        }
    }



     void ResizeMeshToUnit(GameObject gameObject) {
         Debug.Log ("ResizeMeshToUnit");
        MeshFilter mf = gameObject.GetComponentInChildren<MeshFilter>();
        var skin = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if (mf == null && skin == null) {
            Debug.Log ("MeshFilter and skinMesh are nulls");
            return;
        }

        Renderer renderer = gameObject.GetComponentInChildren<Renderer>();


        Bounds bounds = renderer.bounds;

        Debug.Log (bounds);
        float size = bounds.size.x;
        if (size < bounds.size.y)
            size = bounds.size.y;
        if (size < bounds.size.z)
            size = bounds.size.z;
        
        if (Math.Abs(1.0f - size) < 0.01f) {
            Debug.Log ("Already unit size");
            return;
        }
        
        float scale = (1.0f / size) / 2;
        gameObject.transform.localScale = new Vector3(scale, scale, scale);
     }

    // Update is called once per frame
    void Update()
    {
        
    }


    public async Task<GameObject> LoadUrl2(String url) {
        var gltf = new GltfImport();
        var success = await gltf.Load(url);
        GameObject model = null;
        if (success) {
            model = new GameObject("Model:" + url);

            gltf.InstantiateGltf(model.transform);

            ResizeMeshToUnit(model.transform.GetChild(0).gameObject);
        } else {
            Debug.LogError("Loading glTF failed! " + url);
        }

        return model;
    }
}
