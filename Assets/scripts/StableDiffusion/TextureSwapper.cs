using System.Collections;
using UnityEngine;

using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Lukso;

public class TextureSwapper : MonoBehaviour {

    public Shader materialIdShader;
    public Camera captureCamera;

    private List<(Renderer, Material[])> originalMaterials = new List<(Renderer, Material[])>();
    private Dictionary<int, Material> matById = new Dictionary<int, Material>();
    private Dictionary<Texture, Texture> textureReplacement = new Dictionary<Texture, Texture>();
    private Dictionary<string, Material> matByName = new Dictionary<string, Material>();

    private System.Random rnd = new System.Random();
    public bool restore_textures = true;

    void Start() {
    }

    public void SwapAllTextures() {
        originalMaterials.Clear();
        matById.Clear();
        textureReplacement.Clear();
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        int count = 1;
        //var transpMaterial = FindObjectOfType<AvatarManager>().transparentMaterial;
        foreach (Renderer renderer in renderers) {
            originalMaterials.Add((renderer, renderer.sharedMaterials));
            Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
            if (renderer.gameObject.GetComponent<TransparentMaterialRenderer>() != null) {
                continue;
            }

            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) {
                continue;
            }


            for (int i = 0; i < newMaterials.Length; i++) {
                var curMat = renderer.sharedMaterials[i];
                var newMat = curMat;
                if (!curMat.name.StartsWith("TransparentMaterial")) {
                    matById[count] = curMat;
                    if (!matByName.TryGetValue(curMat.name, out newMat)) {
                        newMat = new Material(materialIdShader);
                    }
                    
                    //newMat.SetColor("_IdColor", new Color(count / 256.0f, (float)rnd.NextDouble(), ( count + 1)/ 256.0f, 1));
                    var (r, g, b) = ((count / 100), (count % 100) / 10, count % 10);
                    //newMat.SetColor("_IdColor", new Color(count / 256.0f, (count + 10) / 256.0f, (count + 1) / 256.0f, 1));
                    newMat.SetColor("_IdColor", new Color((r * 20 + 10) / 256.0f, (g * 20 + 10) / 256.0f, (b * 20 + 10) / 256.0f, 1));
                    newMat.SetTexture("_MainTex", curMat.mainTexture);
                    newMat.renderQueue = curMat.renderQueue;

                    if (curMat.HasProperty("_Cutoff")) {
                        newMat.SetFloat("_Cutoff", curMat.GetFloat("_Cutoff"));
                    }

                    count += 1;
                }

                newMaterials[i] = newMat;


            }

            renderer.sharedMaterials = newMaterials;
        }

    }

    private void UpdateMaterialTexture(Material mat) {
        // return;
        var prefix = "_replaced_lukso";
        var originalTexture = mat.mainTexture as Texture2D;
        if (!originalTexture || originalTexture.name.StartsWith(prefix)) {
            return;
        }

        if (textureReplacement.ContainsKey(originalTexture)) {
            var existed = textureReplacement[originalTexture];
            mat.mainTexture = existed;
            return;
        }

        Texture2D copyTexture = new Texture2D(originalTexture.width, originalTexture.height);
        copyTexture.name = prefix + mat.mainTexture.name;
        textureReplacement[originalTexture] = copyTexture;

        if (originalTexture.isReadable) {
            copyTexture.SetPixels(originalTexture.GetPixels());
        } else {

            RenderTexture previous = RenderTexture.active;

            RenderTexture tmp = RenderTexture.GetTemporary(
                                originalTexture.width,
                                originalTexture.height,
                                0,
                                RenderTextureFormat.Default,
                                RenderTextureReadWrite.Linear);

            Graphics.Blit(originalTexture, tmp);
            RenderTexture.active = tmp;
            copyTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            copyTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);
        }

        copyTexture.Apply();

        mat.mainTexture = copyTexture;
    }

    public void RestoreAllTextures() {

        if (!restore_textures) {
            return;
        }
        //        return;
        foreach (var (renderer, materials) in originalMaterials) {
            renderer.sharedMaterials = materials;
        }
        originalMaterials.Clear();
    }

    private (Color[], Color32[], int, int) RenderCoordinatesAndMaterials() {
        const int scale = 2;

        var prevRenderTexture = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(Screen.width * scale, Screen.height * scale, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

        RenderTexture.active = renderTexture;

        Rect rect = new Rect(0, 0, renderTexture.width, renderTexture.height);
        //captureCamera.SetReplacementShader(replacementShader, null);

        captureCamera.targetTexture = renderTexture;

        Shader.SetGlobalFloat("_ShowCoordinates", 1);
        captureCamera.Render();
        texture.ReadPixels(rect, 0, 0);
        texture.Apply();
        Color[] coordinates = texture.GetPixels();

      //  byte[] bytes = texture.EncodeToPNG();
      //  File.WriteAllBytes("d://rendered-coord.png", bytes);



        Shader.SetGlobalFloat("_ShowCoordinates", 0);
        captureCamera.Render();
        texture.ReadPixels(rect, 0, 0);
        texture.Apply();
        Color32[] materials = texture.GetPixels32();

      //  bytes = texture.EncodeToPNG();
      //  File.WriteAllBytes("d://rendered-mat.png", bytes);



        RenderTexture.active = prevRenderTexture;
        RenderTexture.ReleaseTemporary(renderTexture);

        captureCamera.targetTexture = null;

        return (coordinates, materials, texture.width, texture.height);
    }

    public void CaptureAndSave(Texture2D replaceTexture = null) {

        SwapAllTextures();

        var (coordinates, materials, width, height) = RenderCoordinatesAndMaterials();


        HashSet<int> updatedMaterials = new HashSet<int>();

        var srcColors = replaceTexture != null ? replaceTexture.GetPixels() : null;

        int id = 0;
        for (int y = 0; y < height; ++y) {
            for (int x = 0; x < width; ++x, ++id) {
                /*var xyColor = coordinates[id];
                var matId = (int)(materials[id].r);
                var v = (float)((xyColor.r  + xyColor.g/ 255.0f));
                var u = (float)((xyColor.b + xyColor.a / 255.0f) );
                //(u,v) = (v, u);*/
                var (u, v, matId) = GetMatParams(x, y, width, height, coordinates, materials);
                var (u1, v1, matId1) = GetMatParams(x + 1, y, width, height, coordinates, materials);
                var (u2, v2, matId2) = GetMatParams(x + 1, y + 1, width, height, coordinates, materials);
                var (u3, v3, matId3) = GetMatParams(x, y + 1, width, height, coordinates, materials);
                //xyColor.r *= 30;

                //color = new Color(x / texture.width, y / texture.height, 1, 1);
                Material mat;
                if (matById.TryGetValue(matId, out mat)) {
                    var color = Color.black;
                    if (srcColors != null) {
                        int y1 = (int)((y / (float)height) * replaceTexture.height);
                        int x1 = (int)((x / (float)width) * replaceTexture.width);
                        color = srcColors[y1 * replaceTexture.width + x1];
                    }

                    UpdateMaterialTexture(mat);
                    var tex = (Texture2D)mat.mainTexture;
                    if (tex != null && tex.isReadable) {
                        updatedMaterials.Add(matId);


                        SetRGB(tex, (int)(u * tex.width), (int)(v * tex.height), color);

                        const float t = 0.2f;

                        if (matId2 == matId && matId == matId1 && Math.Abs(u - u1) < t && Math.Abs(u - u2) < t && Math.Abs(u1 - u2) < t
                                && Math.Abs(v - v1) < t && Math.Abs(v - v2) < t && Math.Abs(v1 - v2) < t) {
                            DrawTriangle(tex, color,
                                new Vector2Int((int)(u * tex.width), (int)(v * tex.height)),
                                new Vector2Int((int)(u1 * tex.width), (int)(v1 * tex.height)),
                                new Vector2Int((int)(u2 * tex.width), (int)(v2 * tex.height)));
                        }

                        if (matId2 == matId && matId == matId3 && Math.Abs(u - u3) < t && Math.Abs(u - u2) < t && Math.Abs(u3 - u2) < t
                                && Math.Abs(v - v3) < t && Math.Abs(v - v2) < t && Math.Abs(v3 - v2) < t) {
                            DrawTriangle(tex, color,
                                new Vector2Int((int)(u * tex.width), (int)(v * tex.height)),
                                new Vector2Int((int)(u2 * tex.width), (int)(v2 * tex.height)),
                                new Vector2Int((int)(u3 * tex.width), (int)(v3 * tex.height)));
                        }
                    }
                }
            }
        }

     //   Debug.Log("============================================ Updated materials: " + updatedMaterials.Count);

        foreach (var matiId in updatedMaterials) {
            var m = matById[matiId];

            try {
              //  Debug.Log("Mat: " + matiId + " " + m.name + " " + m.mainTexture.name);

               // byte[] bytes = ((Texture2D)m.mainTexture).EncodeToPNG();
              //  File.WriteAllBytes($"d://textures/{matiId}_{m.name}.png", bytes);

                ((Texture2D)m.mainTexture).Apply();
            } catch (Exception e) {
                Debug.LogError("EEEE:" + matiId + " " + m.name + " " + e);
            }
        }

        RestoreAllTextures();
    }

    private void SetRGB(Texture2D tex, int u, int v, Color c) {
        c.a = tex.GetPixel(u, v).a;
        //   c.a = 1;
        tex.SetPixel(u, v, c);
    }

    private (float, float, int) GetMatParams(int x, int y, int w, int h, Color[] coordinates, Color32[] materials) {
        x = Math.Min(x, w - 1);
        y = Math.Min(y, h - 1);
        int id = y * w + x;
        var xyColor = coordinates[id];
        var (r, g, b) = ((int)(materials[id].r), (int)(materials[id].g), (int)(materials[id].b));

        var matId = (r / 20) * 100 + (g / 20) * 10 + (b / 20);
        //var matId = (int)(materials[id].r);
        //var matIdG = (int)(materials[id].g);
        //var matIdB = (int)(materials[id].b);

        //if (matId != 0 && (matId  + 1 != matIdB || matId + 10 != matIdG)) {
        //Debug.LogError("Incorrect color material!:" + matId + " " + matIdG + " " +  matIdB + ":" + x + " " + y + ":" + materials[id]);
        //matId = matIdG - 10;
        //}
        var v = (float)((xyColor.r + xyColor.g / 255.0f));
        var u = (float)((xyColor.b + xyColor.a / 255.0f));

        return (u, v, matId);
        //return ((int)(u*tex.width), (int)(v*tex.height), matId);
    }



    void DrawTriangle(Texture2D texture, Color color, Vector2Int p1, Vector2Int p2, Vector2Int p3) {
        // Сортируем точки по координате y
        if (p1.y > p2.y) Swap(ref p1, ref p2);
        if (p1.y > p3.y) Swap(ref p1, ref p3);
        if (p2.y > p3.y) Swap(ref p2, ref p3);

        // Вычисляем коэффициенты прямых
        double k1 = (p1.y == p3.y) ? 0 : (double)(p3.x - p1.x) / (p3.y - p1.y);
        double k2 = (p1.y == p2.y) ? 0 : (double)(p2.x - p1.x) / (p2.y - p1.y);
        double k3 = (p3.y == p2.y) ? 0 : (double)(p3.x - p2.x) / (p3.y - p2.y);

        // Рисуем верхнюю половину треугольника
        for (int y = p1.y; y <= p2.y; y++) {
            int x1 = (int)(p1.x + k1 * (y - p1.y));
            int x2 = (int)(p1.x + k2 * (y - p1.y));
            if (x1 > x2) Swap(ref x1, ref x2);
            for (int x = x1; x <= x2; x++) {
                SetRGB(texture, x, y, color);
            }
        }

        // Рисуем нижнюю половину треугольника
        for (int y = p2.y; y <= p3.y; y++) {
            int x1 = (int)(p1.x + k1 * (y - p1.y));
            int x2 = (int)(p2.x + k3 * (y - p2.y));
            if (x1 > x2) Swap(ref x1, ref x2);
            for (int x = x1; x <= x2; x++) {
                SetRGB(texture, x, y, color);
            }
        }
    }

    void Swap(ref int a, ref int b) {
        int temp = a;
        a = b;
        b = temp;
    }

    void Swap(ref Vector2Int a, ref Vector2Int b) {
        (a, b) = (b, a);

    }

}
