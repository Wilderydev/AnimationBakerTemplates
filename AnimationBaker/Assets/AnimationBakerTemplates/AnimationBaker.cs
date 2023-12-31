using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class AnimationBaker : MonoBehaviour
{

    public ComputeShader infoTexGen;
    public Shader playShader;
    public AnimationClip[] clips;
    public Material template;
    public string saverFolderName = "";
    public string saverObjectName = "";
    public string [] saverAnimationName;

    public struct VertInfo
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 tangent;
    }

    private void Reset()
    {
        var animation = GetComponent<Animation>();
        var animator = GetComponent<Animator>();

        if (animation != null)
        {
            clips = new AnimationClip[animation.GetClipCount()];
            var i = 0;
            foreach (AnimationState state in animation)
                clips[i++] = state.clip;
        }
        else if (animator != null)
            clips = animator.runtimeAnimatorController.animationClips;
    }

    [ContextMenu("bake texture")]
    void Bake()
    {
        if (saverObjectName == "") saverObjectName = name;

        var skin = GetComponentInChildren<SkinnedMeshRenderer>();
        var vCount = skin.sharedMesh.vertexCount;
        var texWidth = Mathf.NextPowerOfTwo(vCount);
        var mesh = new Mesh();

        int index = 0;
        foreach (var clip in clips)
        {

            try
            {
                if (saverAnimationName[index]== "") saverAnimationName[index] = clip.name;
 

                var frames = Mathf.NextPowerOfTwo((int)(clip.length / 0.05f));
                var dt = clip.length / frames;
                var infoList = new List<VertInfo>();

                var pRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
                pRt.name = string.Format("{0}.{1}.posTex", saverObjectName, saverAnimationName[index]);
                var nRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
                nRt.name = string.Format("{0}.{1}.normTex", saverObjectName, saverAnimationName[index]);
                var tRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
                tRt.name = string.Format("{0}.{1}.tanTex", saverObjectName, saverAnimationName[index]);
                foreach (var rt in new[] { pRt, nRt, tRt })
                {
                    rt.enableRandomWrite = true;
                    rt.Create();
                    RenderTexture.active = rt;
                    GL.Clear(true, true, Color.clear);
                }

                for (var i = 0; i < frames; i++)
                {
                    clip.SampleAnimation(gameObject, dt * i);
                    skin.BakeMesh(mesh);

                    var verexArry = mesh.vertices;
                    var normalArry = mesh.normals;
                    var tangentArray = mesh.tangents;
                    infoList.AddRange(Enumerable.Range(0, vCount)
                        .Select(idx => new VertInfo()
                        {
                            position = verexArry[idx],
                            normal = normalArry[idx],
                            tangent = tangentArray[idx],
                        })
                    );
                }
                var buffer = new ComputeBuffer(infoList.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertInfo)));
                buffer.SetData(infoList.ToArray());

                var kernel = infoTexGen.FindKernel("CSMain");
                uint x, y, z;
                infoTexGen.GetKernelThreadGroupSizes(kernel, out x, out y, out z);

                infoTexGen.SetInt("VertCount", vCount);
                infoTexGen.SetBuffer(kernel, "Info", buffer);
                infoTexGen.SetTexture(kernel, "OutPosition", pRt);
                infoTexGen.SetTexture(kernel, "OutNormal", nRt);
                infoTexGen.SetTexture(kernel, "OutTangent", tRt);
                infoTexGen.Dispatch(kernel, vCount / (int)x + 1, frames / (int)y + 1, 1);

                buffer.Release();

    #if UNITY_EDITOR
                var folderPath = Path.Combine("Assets", saverFolderName);
                if (!AssetDatabase.IsValidFolder(folderPath))
                    AssetDatabase.CreateFolder("Assets", saverFolderName);
                var dFolderPath = Path.Combine(folderPath, saverObjectName + "_Animation"); 
                if (!AssetDatabase.IsValidFolder(dFolderPath))
                    AssetDatabase.CreateFolder(folderPath, saverObjectName + "_Animation");
                var subFolderPath =Path.Combine(dFolderPath, saverObjectName + "_Texture_Assets");
                if (!AssetDatabase.IsValidFolder(subFolderPath))
                    AssetDatabase.CreateFolder(dFolderPath, saverObjectName + "_Texture_Assets");
                var posTex = RenderTextureToTexture2D.Convert(pRt);
                var normTex = RenderTextureToTexture2D.Convert(nRt);
                var tanTex = RenderTextureToTexture2D.Convert(tRt);
                Graphics.CopyTexture(pRt, posTex);
                Graphics.CopyTexture(nRt, normTex);
                Graphics.CopyTexture(tRt, tanTex);
                Material mat =  CreateAnimatedObjects.CopyMaterial(template);
                mat.shader = playShader;
                mat.SetTexture("_MainTex", skin.sharedMaterial.mainTexture);
                mat.SetTexture("_PosTex", posTex);
                mat.SetTexture("_NmlTex", normTex);
                mat.SetFloat("_Length", clip.length);
                if (clip.wrapMode == WrapMode.Loop)
                {
                    mat.SetFloat("_Loop", 1f);
                    mat.EnableKeyword("ANIM_LOOP");
                }
                var go = new GameObject(saverObjectName + "." + saverAnimationName[index]);
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                go.AddComponent<MeshFilter>().sharedMesh = skin.sharedMesh;
                AssetDatabase.CreateAsset(posTex, Path.Combine(subFolderPath, pRt.name + ".asset"));
                AssetDatabase.CreateAsset(normTex, Path.Combine(subFolderPath, nRt.name + ".asset"));
                AssetDatabase.CreateAsset(tanTex, Path.Combine(subFolderPath, tRt.name + ".asset"));
                var res = Path.Combine(subFolderPath, string.Format("{0}.{1}.animTex.asset", saverObjectName, saverAnimationName[index]));
                Debug.Log($"{Path.Combine(subFolderPath, string.Format("{0}.{1}.animTex.asset", saverObjectName, saverAnimationName[index]))}");
                AssetDatabase.CreateAsset(mat, res);
                PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(dFolderPath, go.name + ".prefab").Replace("\\", "/"));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                index++;
            }
            catch (Exception e)
            {
                Debug.Log($"<color=#F07427>Error with saverAnimationName[{index}] " + e.Message +"</color>");
            }
#endif
        }
    }
}
