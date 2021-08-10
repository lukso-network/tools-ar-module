using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Threading.Tasks;
using GLTFast;

public class GltfGlbLoader { 

    public static async Task<GameObject> LoadUrl(String url) {
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


     private static void ResizeMeshToUnit(GameObject gameObject) {
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


}
