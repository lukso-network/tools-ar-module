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

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<int, Material> matById = new Dictionary<int, Material>();
    
    private System.Random rnd = new System.Random();
    private List<Material> repMat = new List<Material>();

    void Start() {
    }

    public void SwapAllTextures() {
        originalMaterials.Clear();
        matById.Clear();
        Renderer[] renderers = FindObjectsOfType<Renderer>();
        int count = 0;
        Dictionary<string, Material> matMap = new Dictionary<string, Material>();
        foreach (Renderer renderer in renderers) {
            originalMaterials[renderer] = renderer.materials;
            Material[] newMaterials = new Material[renderer.materials.Length];
            if (renderer.gameObject.GetComponent<TransparentMaterialRenderer>() != null ) {
                continue;
            }
            for (int i = 0; i < newMaterials.Length; i++) {
                var curMat = renderer.materials[i];
                if (false && matMap.ContainsKey(curMat.name)) {
                    var m = matMap[curMat.name];
                    newMaterials[i] = m;
                } else {
                    matById[count] = curMat;

                  
                    var newMat = new Material(materialIdShader);
                    //newMat.mainTexture = curMat.mainTexture;
                    matMap[curMat.name] = newMaterials[i] = newMat;
                    newMaterials[i].SetColor("_IdColor", new Color(count / 256.0f, (float)rnd.NextDouble(), ( count + 1)/ 256.0f, 1));
                   // int z = 0;
                  //  newMaterials[i].SetColor("_IdColor", new Color(z / 256.0f, (float)rnd.NextDouble(), (z + 1) / 256.0f, 1));
                    count += 1;
                }
            }
            renderer.materials = newMaterials;
        }
       // return;
        foreach (KeyValuePair<Renderer, Material[]> kvp in originalMaterials) {
            Renderer renderer = kvp.Key;
            Material[] materials = kvp.Value;

            foreach(var curMat in materials) {
                //UpdateMaterialTexture(curMat);
                //continue;
                var originalTexture = curMat.mainTexture as Texture2D;
                if (originalTexture != null) {
                    Texture2D copyTexture = new Texture2D(originalTexture.width, originalTexture.height);
                    
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

                    //if (!Graphics.ConvertTexture(originalTexture, copyTexture)) {
                    //  Debug.LogError("Can't create a copy of texture");
                    //}
                    copyTexture.Apply();
                    /*
                    var c = copyTexture.GetPixels32(0);
                    for(int i = 0; i < c.Length; ++i) {
                        c[i] = new Color32(10, 10, 10, 255);
                    }
                    copyTexture.SetPixels32(c);
                    copyTexture.Apply();*/
                    curMat.mainTexture = copyTexture;
                    repMat.Add(curMat);
                }
            }

        }

    }

    private void UpdateMaterialTexture(Material mat) {
        return;
        var prefix = "_replaced_lukso";
        var originalTexture = mat.mainTexture as Texture2D;
        if (!originalTexture || originalTexture.name.StartsWith(prefix)) {
            return;
        }
        ///if (!mat.mainTexture.isReadable && !mat.mainTexture.name.StartsWith(prefix)) {

        Texture2D copyTexture = new Texture2D(originalTexture.width, originalTexture.height);
        copyTexture.name = prefix + mat.mainTexture.name;

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
        foreach (KeyValuePair<Renderer, Material[]> kvp in originalMaterials) {
            Renderer renderer = kvp.Key;
            Material[] materials = kvp.Value;
            renderer.materials = materials;
        }
        originalMaterials.Clear();
    }

    public void Update() {
        if (Time.frameCount == 10) {
       //     CaptureAndSave();
        }
    }


    public void CaptureAndSave(Texture2D replaceTexture = null) {

        SwapAllTextures();
     //   return;
        //Shader.SetGlobalFloat("_ShowCoordinates", 1);
        //return;


        //RenderTexture renderTexture = captureCamera.targetTexture;
        //RenderTexture renderTexture = new RenderTexture(Screen.width *4, Screen.height *4 , 32);
        const int scale = 1;

        var prevRenderTexture = RenderTexture.active;
        RenderTexture renderTexture = new RenderTexture(Screen.width * scale, Screen.height * scale, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

        RenderTexture.active = renderTexture;


        Rect rect = new Rect(0, 0, renderTexture.width, renderTexture.height);
        //captureCamera.SetReplacementShader(replacementShader, null);

        captureCamera.targetTexture = renderTexture;
        Shader.SetGlobalFloat("_ShowCoordinates", 1);
        captureCamera.Render();

        texture.ReadPixels(rect, 0, 0);
        texture.Apply();
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes("d://rendered-coord.png", bytes);

        Color[] coordinates = texture.GetPixels();
        Shader.SetGlobalFloat("_ShowCoordinates", 0);
        captureCamera.Render();
        texture.ReadPixels(rect, 0, 0);
        texture.Apply();

        RenderTexture.active = prevRenderTexture;

        captureCamera.targetTexture = null;
        //captureCamera.SetReplacementShader(null, null);

        bytes = texture.EncodeToPNG();
        File.WriteAllBytes("d://rendered-mats.png", bytes);

        Color32[] materials = texture.GetPixels32();
        HashSet<int> updatedMaterials = new HashSet<int>();
        for (int y = 0; y < texture.height; ++y) {
            for (int x = 0; x < texture.width; ++x) {
                /*var xyColor = coordinates[id];
                var matId = (int)(materials[id].r);
                var v = (float)((xyColor.r  + xyColor.g/ 255.0f));
                var u = (float)((xyColor.b + xyColor.a / 255.0f) );
                //(u,v) = (v, u);*/
                var (u, v, matId) = GetMatParams(x, y, texture.width, texture.height, coordinates, materials);
                Material mat;
                if (!updatedMaterials.Contains(matId) && matById.TryGetValue(matId, out mat)) {
                    var tex = (Texture2D)mat.mainTexture;
                    if (tex != null) {
                        Debug.Log(matId + ":" + mat + " " + mat.mainTexture.name + " " + tex.format + " " + tex.graphicsFormat);
                        if ( false) {// && !tex.isReadable || (tex.format != TextureFormat.RGBA32 && tex.format != TextureFormat.ARGB32)) {
                            var newText = new Texture2D(mat.mainTexture.width, mat.mainTexture.height, TextureFormat.ARGB32, true);
                            //var newText = new Texture2D(tex.width, tex.height, tex.format, true);
                            bool res = Graphics.ConvertTexture(tex, newText);
                            Debug.Log("Convertion result:" + res);

                            bytes = newText.EncodeToPNG();
                            File.WriteAllBytes($"d://rendered-mats-{matId}.png", bytes);
                            newText.Apply(false, true);
                            mat.mainTexture = newText;
                        }
                        updatedMaterials.Add(matId);
                    }
                }
            }
        }

        var srcColors = replaceTexture != null ? replaceTexture.GetPixels() : null;
        bytes = replaceTexture.EncodeToPNG();
        File.WriteAllBytes("d://replace-texture.png", bytes);

        int id = 0;
        for (int y = 0; y < texture.height; ++y) {
            for (int x = 0; x < texture.width; ++x, ++id) {
                /*var xyColor = coordinates[id];
                var matId = (int)(materials[id].r);
                var v = (float)((xyColor.r  + xyColor.g/ 255.0f));
                var u = (float)((xyColor.b + xyColor.a / 255.0f) );
                //(u,v) = (v, u);*/
                var (u, v, matId) = GetMatParams(x, y, texture.width, texture.height, coordinates, materials);
                var (u1, v1, matId1) = GetMatParams(x + 1, y, texture.width, texture.height, coordinates, materials);
                var (u2, v2, matId2) = GetMatParams(x + 1, y + 1, texture.width, texture.height, coordinates, materials);
                var (u3, v3, matId3) = GetMatParams(x, y + 1, texture.width, texture.height, coordinates, materials);
                //xyColor.r *= 30;

                //color = new Color(x / texture.width, y / texture.height, 1, 1);
                Material mat;
                if (matById.TryGetValue(matId, out mat)) {
                    var color = Color.black;
                    if (srcColors != null) {
                        int y1 = (int)((y / (float)texture.height) * replaceTexture.height);
                        int x1 = (int)((x / (float)texture.width) * replaceTexture.width);
                        color = srcColors[y1 * replaceTexture.width + x1];
                    }

                    UpdateMaterialTexture(mat);
                    var tex = (Texture2D)mat.mainTexture;
                    if (tex != null && tex.isReadable) {//if (tex != null && tex.isReadable && (tex.format == TextureFormat.RGBA32 || tex.format == TextureFormat.ARGB32)) {
                        //texture.SetPixels(squarePosition.x, squarePosition.y, squareSize.x, squareSize.y, colors);
                        SetRGB(tex, (int)(u * tex.width), (int)(v * tex.height), color);

                        const float t = 0.2f;

                        if (matId2 == matId && matId == matId1 && Math.Abs(u - u1) < t && Math.Abs(u - u2) < t && Math.Abs(u1 - u2) < t
                                && Math.Abs(v - v1) < t && Math.Abs(v - v2) < t && Math.Abs(v1 - v2) < t) {
                            DrawTriangle(tex, color,
                            new Vector2Int((int)(u * tex.width), (int)(v * tex.height)),
                            //new Vector2Int((int)(u * tex.width+2), (int)(v * tex.height+0)),
                            //new Vector2Int((int)(u * tex.width), (int)(v * tex.height + 2)));
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

                        /*
                        DrawTriangle(tex, Color.green, 
                        new Vector2Int((int)(0.5 * tex.width), (int)(0.8 * tex.height)), 
                        new Vector2Int((int)(0.7 * tex.width), (int)(0.6 * tex.height)), 
                        new Vector2Int((int)(0.3 * tex.width), (int)(0.4f * tex.height)));
                        x = 100000;
                        y = 100000;
                        break;
                        */
                        //tex.
                        //tex.SetPixel((int)(u * tex.width), (int)(v * tex.height), Color.green, 1);
                        //tex.SetPixel((int)(u * tex.width), (int)(v * tex.height), Color.green, 2);
                        //tex.SetPixel((int)(u * tex.width), (int)(v * tex.height), Color.green, 3);

                        //texture.SetPixel(x, y, xyColor);// Color.red);
                        //texture.SetPixel(x, y, xyColor, 1);
                        //texture.SetPixel(x, y, xyColor, 2);
                        //texture.SetPixel(x, y, xyColor, 3);

                        //    tex.SetPixel((int)(u * tex.width)+1, (int)(v * tex.height), Color.red);

                        //   tex.SetPixel((int)(u * tex.height), (int)(v * tex.width), Color.red);
                        // tex.SetPixel((int)(v * tex.width), (int)(u * tex.height), Color.red);
                        //tex.SetPixel((int)(u * tex.height) + 1, (int)(v * tex.height), Color.green);
                    }
                }

            }
        }



        texture.Apply();
        bytes = texture.EncodeToPNG();
        File.WriteAllBytes("d://rendered2.png", bytes);



        //foreach(var m in matById.Values) {
        foreach (var matiId in updatedMaterials) {
            var m = matById[matiId];


            try {
                ((Texture2D)m.mainTexture).Apply();
            } catch (Exception e) {
                Debug.LogError("EEEE:" + matiId + " " + m.name + " " + e);
            }
        }



        RestoreAllTextures();
    }

    private void SetRGB(Texture2D tex, int u, int v, Color c) {
        c.a = tex.GetPixel(u, v).a;
        tex.SetPixel(u, v, c);
    }

    private (float, float, int) GetMatParams(int x, int y, int w, int h, Color[] coordinates, Color32[] materials) {
        x = Math.Min(x, w - 1);
        y = Math.Min(y, h - 1);
        int id = y * w + x;
        var xyColor = coordinates[id];
        var matId = (int)(materials[id].r);
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
