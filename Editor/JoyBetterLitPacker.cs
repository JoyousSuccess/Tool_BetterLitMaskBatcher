using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Text.RegularExpressions;

namespace Joymagine.Procure {

#region [class] 

    [CustomEditor(typeof(JoyBetterLitPacker), true)]
    public class JoyBetterLitPackerEditorDrawer : Editor {

        public override void OnInspectorGUI() {
            JoyBetterLitPacker litPacker = (JoyBetterLitPacker)target;

            EditorGUILayout.HelpBox("# of Smoothness images determines # of full textures, populate all list with equal # of images! (+ has null checks)", MessageType.Warning, true);
            EditorGUILayout.HelpBox("make a folder inside the inputs folder named 'MaskBl'", MessageType.Warning, true);
            EditorGUILayout.HelpBox("rough = tick the invert bool; Also name _r or _s for rough / smooth", MessageType.Warning, true);
            var mList = serializedObject.FindProperty("metalList");
            EditorGUILayout.PropertyField(mList, new GUIContent("Metallic Maps"), true);

            var aoList = serializedObject.FindProperty("aoList");
            EditorGUILayout.PropertyField(aoList, new GUIContent("Ambient Occlusion Maps"), true);

            var dList = serializedObject.FindProperty("detailList");
            EditorGUILayout.PropertyField(dList, new GUIContent("Detail Maps"), true);

            var sList = serializedObject.FindProperty("smoothList");
            EditorGUILayout.PropertyField(sList, new GUIContent("Smoothness Maps"), true);

            var invertSBool = serializedObject.FindProperty("allSmoothIsRoughnessMaps");
            EditorGUILayout.PropertyField(invertSBool, new GUIContent("All Smooth Are Rough Maps"), true);

            if (GUILayout.Button("Do Mask Pack")) {
                litPacker.metalList = TexFromProperty(mList);
                litPacker.aoList = TexFromProperty(aoList);
                litPacker.detailList = TexFromProperty(dList);
                litPacker.smoothList = TexFromProperty(sList);
                litPacker.allSmoothIsRoughnessMaps = invertSBool.boolValue;

                litPacker.DoSave();
            }



            EditorGUILayout.Space(40);
                
                

            EditorGUILayout.HelpBox("make a folder inside the inputs folder named 'Mats'", MessageType.Warning, true);
            var matAlbedoList = serializedObject.FindProperty("mat_albedoList");
            EditorGUILayout.PropertyField(matAlbedoList, new GUIContent("Albedos"), true);

            var matNormalList = serializedObject.FindProperty("mat_normalList");
            EditorGUILayout.PropertyField(matNormalList, new GUIContent("Normal Maps"), true);

            var matMaskList = serializedObject.FindProperty("mat_maskList");
            EditorGUILayout.PropertyField(matMaskList, new GUIContent("Mask Maps"), true);

            if (GUILayout.Button("Fix NormalMaps")) {
                litPacker.mat_normalList = TexFromProperty(matNormalList);
                litPacker.Do_FixNormalMaps();
            }

            if (GUILayout.Button("Make BetterLit Mats")) {
                litPacker.mat_albedoList = TexFromProperty(matAlbedoList);
                litPacker.mat_normalList = TexFromProperty(matNormalList);
                litPacker.mat_maskList = TexFromProperty(matMaskList);

                litPacker.Do_BetterLitMatFromTextures();
            }
        }


        private List<Texture2D> TexFromProperty(SerializedProperty property) {
            List<Texture2D> textureList = new List<Texture2D>();
            for (int i = 0; i < property.arraySize; i++) {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue is Texture2D texture) {
                    textureList.Add(texture);
                }
            }
            return textureList;
        }
    }

#endregion


class JoyBetterLitPacker : EditorWindow {

#region [silver]

    [MenuItem("Window/Better Lit Shader/Batch Pack Mask Mat")]
    static void PackMask() {
        JoyBetterLitPacker.DoMaskBatchWindow();
    }

#endregion

#region [enum] 

    enum Channel {
        Red = 0, Green, Blue, Alpha
    }

    public enum Mode {
        NormalRB,      // normal map plus channels in R/B
        FourChannel,   // four single channels
        ColorA,        // color plus data in alpha
        Detail         // HDRP style detail texture 
    }

#endregion

#region [refs] 

    string R, G, B, A;

    Texture2D texR;
    Texture2D texG;
    Texture2D texB;
    Texture2D texA;
    Texture2D texColor; 
    Texture2D texNormal;

    Mode mode = Mode.FourChannel;

    public List<Texture2D> metalList = new List<Texture2D>();
    public List<Texture2D> aoList = new List<Texture2D>(); 
    public List<Texture2D> detailList = new List<Texture2D>();
    public List<Texture2D> smoothList = new List<Texture2D>();
    public bool allSmoothIsRoughnessMaps = true;




    Editor editor;

    string path;
    static string lastDir;
    
    bool batchInProgress = false;

#endregion

#region [ui] 

    public static void DoMaskBatchWindow() {        //"Metalic", "Occlusion", "Detail", "Smoothness"
        JoyBetterLitPacker window = (JoyBetterLitPacker)EditorWindow.GetWindow<JoyBetterLitPacker>(false, "BetterLit Batch", true);
    }
     
    void OnGUI() {
        if (!editor) { editor = Editor.CreateEditor(this); }
        if (editor) { editor.OnInspectorGUI(); }
    }

    void OnInspectorUpdate() { Repaint(); }

#endregion

#region [blue] 

    TextureImporterCompression Uncompress(Texture2D tex) {
        var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
        var ret = ti.textureCompression;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
        return ret;
    }

    void Compress(Texture2D tex, TextureImporterCompression cmp) {
        var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
        ti.textureCompression = cmp;
        ti.SaveAndReimport();
    }

    Vector2 FindLargestTextureSize() {
        Vector2 largest = Vector2.zero;
        
        if (texR != null) {
            largest.x = texR.width;
            largest.y = texR.height;
        }
        if (texG != null) {
            if (texG.width > largest.x) {
                largest.x = texG.width;
            }
            if (texG.height > largest.y) {
                largest.y = texG.height;
            }
        }
        if (texB != null) {
            if (texB.width > largest.x) {
                largest.x = texB.width;
            }
            if (texB.height > largest.y) {
                largest.y = texB.height;
            }
        }
        if (texA != null) {
            if (texA.width > largest.x) {
                largest.x = texA.width;
            }
            if (texA.height > largest.y) {
                largest.y = texA.height;
            }
        }


        if (texNormal != null) {                      
            if (texNormal.width > largest.x) {
                largest.x = texNormal.width;
            }
            if (texNormal.height > largest.y) {
                largest.y = texNormal.height;
            }
        }
        if (texColor != null) {
            if (texColor.width > largest.x) {
                largest.x = texColor.width;
            }
            if (texColor.height > largest.y) {
                largest.y = texColor.height;
            }
        }
        return largest;
    }

    void ReadBackTexture(RenderTexture rt, Texture2D tex) {
        var old = RenderTexture.active;
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = old;
    }



    void ExtractChannel(RenderTexture rt, Texture2D tex, Channel dst, bool invert, bool grey = false) {
        Texture2D temp = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, true, mode != Mode.ColorA);
        ReadBackTexture(rt, temp);
        Color[] srcC = temp.GetPixels();
        Color[] destC = tex.GetPixels();
        if (grey) {
            for (int i = 0; i < srcC.Length; ++i) {
               float g = srcC[i].grayscale;
               srcC[i] = new Color(g, g, g, g);
            }
         }
        
        for (int i = 0; i < srcC.Length; ++i) {
            Color sc = srcC[i];
            Color dc = destC[i];

            dc[(int)dst] = sc[(int)dst];
            destC[i] = dc;
        }
        
        if (invert) {
            for (int i = 0; i < destC.Length; ++i) {
                Color destColor = destC[i];
                destColor[(int)dst] = 1.0f - destColor[(int)dst];
                destC[i] = destColor;
            }
        }
        tex.SetPixels(destC);
        DestroyImmediate(temp);
    }

#endregion

#region [teal] 

    public void DoSave() {
        string pathTex = AssetDatabase.GetAssetPath(smoothList[0]); 
        string dirPath = Path.Combine(Path.GetDirectoryName(pathTex), "MaskBl");

        batchInProgress = true;

        Debug.Log("000");
        for (int i = 0; i < smoothList.Count; i++) {
            
            if (i >= 0 && i < metalList.Count) {
                texR = metalList[i];
            } else {
                texR = metalList.Count > 0 ? metalList[0] : null;
            }

            if (i >= 0 && i < aoList.Count) {
                texG = aoList[i];
            } else {
                texG = aoList.Count > 0 ? aoList[0] : null;
            }

            if (i >= 0 && i < detailList.Count) {
                texB = detailList[i];
            } else {
                texB = detailList.Count > 0 ? detailList[0] : null;
            }

            if (i >= 0 && i < smoothList.Count) {
                texA = smoothList[i];
            } else {
                texA = smoothList.Count > 0 ? smoothList[0] : null;
            }
            Debug.Log("index : " + i);

            string textureName = smoothList[i].name;
            string pattern = @"(_[rs])(\.(png|tga|jpg|bmp))?$";         //remove "_r" or "_s" from the texture name, whether it's at the end or before an extension like ".png," ".tga," ".jpg," or ".bmp," .. use a regular expression to handle different cases
            string newName = Regex.Replace(textureName, pattern, "$2", RegexOptions.IgnoreCase);

            path = dirPath + "/" + newName + "_MaskBl.tga";

            Save();
        }
        batchInProgress = false;
        Debug.Log("DONE!!!");
    }

    void Save() {
        Vector2 largest = FindLargestTextureSize();
        if (largest.x < 1) {
            Debug.LogError("You need some textures in there");
            return;
        }

        Texture2D tex = new Texture2D((int)largest.x, (int)largest.y, TextureFormat.ARGB32, true, mode != Mode.ColorA);
        RenderTexture rt = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, mode == Mode.ColorA ?  RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
        Graphics.Blit(Texture2D.blackTexture, rt);


        if (mode == Mode.FourChannel) {
            if (texR != null) {
                var cmp = Uncompress(texR);
                Graphics.Blit(texR, rt);
                ExtractChannel(rt, tex, Channel.Red, false);
                Compress(texR, cmp);
            }
            if (texG != null) {
                var cmp = Uncompress(texG);
                Graphics.Blit(texG, rt);
                ExtractChannel(rt, tex, Channel.Green, false);
                Compress(texG, cmp);
            }
            if (texB != null) {
                var cmp = Uncompress(texB);
                Graphics.Blit(texB, rt);
                ExtractChannel(rt, tex, Channel.Blue, false);
                Compress(texB, cmp);
            }
            if (texA != null) {
                var cmp = Uncompress(texA);
                Graphics.Blit(texA, rt);
                ExtractChannel(rt, tex, Channel.Alpha, allSmoothIsRoughnessMaps);
                Compress(texA, cmp);
            }
        } 
        else {
            Debug.LogWarning("I only set this up for Mask, cause thats what i needed");
            return;
        }


        tex.Apply(true, false);
        var tga = tex.EncodeToTGA();
        System.IO.File.WriteAllBytes(path, tga);
        DestroyImmediate(rt);
        DestroyImmediate(tex);
        AssetDatabase.Refresh();
        path = path.Substring(path.IndexOf("Assets"));

        if (mode != Mode.ColorA) {
            AssetImporter ai = AssetImporter.GetAtPath(path);
            TextureImporter ti = (TextureImporter)ai;
            ti.sRGBTexture = mode == Mode.ColorA;
            ti.SaveAndReimport();
        }

        EditorApplication.delayCall += () => {
            Debug.Log("One Done");
        };

    }

#endregion

#region [purple] 

    public List<Texture2D> mat_albedoList = new List<Texture2D>();
    public List<Texture2D> mat_normalList = new List<Texture2D>();
    public List<Texture2D> mat_maskList = new List<Texture2D>();

    string pathMat;

    public void Do_FixNormalMaps() {
        for (int i = 0; i < mat_normalList.Count; i++) {
            string pathN = AssetDatabase.GetAssetPath(mat_normalList[i]);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(pathN);            
            if (importer != null) {
                if (importer.textureType != TextureImporterType.NormalMap) {    // if not set as normal map .. make it normal map
                    importer.textureType = TextureImporterType.NormalMap;
                }
                if (importer.sRGBTexture == true) {                             //if set to sRGB .. make Linear  .. colorspace .. i guess it needs this lol
                    importer.sRGBTexture = false;
                }
                importer.SaveAndReimport();
            }
        }
    }

    public void Do_BetterLitMatFromTextures() {
        string pathTex = AssetDatabase.GetAssetPath(mat_albedoList[0]); 
        string dirPath = Path.Combine(Path.GetDirectoryName(pathTex), "Mats");

        for (int i = 0; i < mat_albedoList.Count; i++) {
            Material material = new Material(Shader.Find("Better Lit/Lit"));
            
            string textureName = mat_albedoList[i].name;
            string pattern = @"\.(png|tga|jpg|bmp)$";       // Matches file extensions at the end of the string
            string newName = Regex.Replace(textureName, pattern, "", RegexOptions.IgnoreCase);

            pathMat = dirPath + "/" + newName + ".mat";


            AssetDatabase.CreateAsset(material, pathMat);
            Material mat = material;

            float alpha = mat.color.a;
            mat.color = new Color(1, 1, 1, alpha);
            mat.SetTexture("_MainTex", mat_albedoList[i]);
            
            //mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", mat_normalList[i]);

            //mat.EnableKeyword("_PARALLAXMAP");
            mat.SetTexture("_MaskMap", mat_maskList[i]);
        }
    }

#endregion

}
}

    //EditorUtility.SaveFilePanel("Save Resulting Texture", lastDir, "", "tga");

    //string[] tempSubstrings = mat_albedoList[i].name.Split('_');                  
    //string newName = tempSubstrings[0];


    //! For Original File  ~~~ Compare this to Packages / BetterLitShader / Scripts / Editor / MiniTexturePacker.cs  (BetterLitShader 2021 ~ v. Unity 2021)

        //!Original Method
    // void ExtractChannel(RenderTexture rt, Texture2D tex, Channel src, Channel dst, bool invert, bool grey = false) {
    //     Texture2D temp = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, true, mode != Mode.ColorA);
    //     ReadBackTexture(rt, temp);
    //     Color[] srcC = temp.GetPixels();
    //     Color[] destC = tex.GetPixels();
    //     if (grey) {
    //         for (int i = 0; i < srcC.Length; ++i) {
    //             float g = srcC[i].grayscale;
    //             srcC[i] = new Color(g, g, g, g);
    //         }
    //     }
        
    //     for (int i = 0; i < srcC.Length; ++i) {
    //         Color sc = srcC[i];
    //         Color dc = destC[i];

    //         dc[(int)dst] = sc[(int)src];
    //         destC[i] = dc;
    //     }

    //     if (invert) {
    //         for (int i = 0; i < destC.Length; ++i) {
    //             Color dc = destC[i];
    //             dc[(int)dst] = 1.0f - dc[(int)src];
    //             destC[i] = dc;
    //         }
    //     }
    //     tex.SetPixels(destC);
    //     DestroyImmediate(temp);
    // }

    //! from Original Save
    // if (mode == Mode.FourChannel) {
    //         if (texR.tex != null) {
    //             var cmp = Uncompress(texR.tex);
    //             Graphics.Blit(texR.tex, rt);
    //             ExtractChannel(rt, tex, texR.channel, Channel.Red, texR.invert);
    //             Compress(texR.tex, cmp);
    //         }
    //         if (texG.tex != null) {
    //             var cmp = Uncompress(texG.tex);
    //             Graphics.Blit(texG.tex, rt);
    //             ExtractChannel(rt, tex, texG.channel, Channel.Green, texG.invert);
    //             Compress(texG.tex, cmp);
    //         }
    //         if (texB.tex != null) {
    //             var cmp = Uncompress(texB.tex);
    //             Graphics.Blit(texB.tex, rt);
    //             ExtractChannel(rt, tex, texB.channel, Channel.Blue, texB.invert);
    //             Compress(texB.tex, cmp);
    //         }
    //         if (texA.tex != null) {
    //             var cmp = Uncompress(texA.tex);
    //             Graphics.Blit(texA.tex, rt);
    //             ExtractChannel(rt, tex, texA.channel, Channel.Alpha, texA.invert);
    //             Compress(texA.tex, cmp);
    //         }
    //     } 