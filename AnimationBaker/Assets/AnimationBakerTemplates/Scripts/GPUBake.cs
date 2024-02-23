using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimationBakerTemplates.Base;
using UnityEditor;
using UnityEngine;

namespace AnimationBakerTemplates.Scripts
{
    public struct VertInfo
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 tangent;
    }
    public class GPUBake : MonoBehaviour
    {
        
        public ComputeShader infoTexGen;
        public Shader playShader;
        public AnimationClip clip;
        public Material template;

        private readonly List<Texture2D> _positionTextures = new List<Texture2D>();
        private readonly List<Texture2D> _normalTextures = new List<Texture2D>();
        private readonly List<Texture2D> _tangensTextures = new List<Texture2D>();
        
        
        private static readonly int s_MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int s_PosTex = Shader.PropertyToID("_PosTex");
        private static readonly int s_NmlTex = Shader.PropertyToID("_NmlTex");
        private static readonly int s_Length = Shader.PropertyToID("_Length");
        private static readonly int s_Loop = Shader.PropertyToID("_Loop");

        [ContextMenu("bake texture")]
        void Bake()
        {
            var skins = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skin in skins)
            {
                BakeMeshRender(skin);
            }
        }
        private void BakeMeshRender(SkinnedMeshRenderer skin)
        {
            var folderPath = Path.Combine("Assets", name);
            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder("Assets", name);
            var localFolder = Path.Combine(folderPath, $"{name}_{skin.name}"); 
            if (!AssetDatabase.IsValidFolder(localFolder))
                AssetDatabase.CreateFolder(folderPath, $"{name}_{skin.name}");
            
            var vCount = skin.sharedMesh.vertexCount;
            var texWidth = Mathf.NextPowerOfTwo(vCount);
            var mesh = new Mesh();
            var frames = Mathf.NextPowerOfTwo((int)(clip.length / 0.05f));
            var dt = clip.length / frames;
            var infoList = new List<VertInfo>();
            var pRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            pRt.name = $"{name}_{skin.name}.posTex";
            var nRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            nRt.name = $"{name}_{skin.name}.normTex";
            var tRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            tRt.name = $"{name}_{skin.name}.tanTex";
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
            
            // compute by buffer
            var buffer = new ComputeBuffer(infoList.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertInfo)));
            buffer.SetData(infoList.ToArray());

            var kernel = infoTexGen.FindKernel("CSMain");
            infoTexGen.GetKernelThreadGroupSizes(kernel, out var x, out var y, out _);
            infoTexGen.SetInt("VertCount", vCount);
            infoTexGen.SetBuffer(kernel, "Info", buffer);
            infoTexGen.SetTexture(kernel, "OutPosition", pRt);
            infoTexGen.SetTexture(kernel, "OutNormal", nRt);
            infoTexGen.SetTexture(kernel, "OutTangent", tRt);
            infoTexGen.Dispatch(kernel, vCount / (int)x + 1, frames / (int)y + 1, 1);

            buffer.Release();
            
            // create asset
            
            var posTex = RenderTextureToTexture2D.Convert(pRt);
            var normTex = RenderTextureToTexture2D.Convert(nRt);
            var tanTex = RenderTextureToTexture2D.Convert(tRt);
            
            Material mat =  CreateAnimatedObjects.CopyMaterial(template);
            mat.shader = playShader;
            mat.SetTexture(s_MainTex, skin.sharedMaterial.mainTexture);
            mat.SetFloat(s_Length, clip.length);
            if (clip.wrapMode == WrapMode.Loop)
            {
                mat.SetFloat(s_Loop, 1f);
                mat.EnableKeyword("ANIM_LOOP");
            }

            
            mat = SaveAsset(posTex, localFolder,pRt.name, mat, 1);
            mat = SaveAsset(normTex, localFolder,nRt.name, mat, 2);
            mat = SaveAsset(tanTex, localFolder,tRt.name, mat, 3);
            AssetDatabase.CreateAsset(mat, Path.Combine(localFolder, name) + ".animTex.asset");
        }

        private Material SaveAsset(Texture2D texture2D, string localFolder, string nameOfTexture, Material material, int mask = 1)
        {
            if (mask == 1)
            {
                _positionTextures.Add(texture2D);
                material.SetTexture(s_PosTex, texture2D);
            }
            else if (mask == 2)
            {
                _normalTextures.Add(texture2D);
                material.SetTexture(s_NmlTex, texture2D);
            }
            else if (mask == 3)
            {
                _tangensTextures.Add(texture2D);
            }
            
            AssetDatabase.CreateAsset(texture2D, Path.Combine(localFolder, nameOfTexture) + ".asset");

            return material;
        }

    }
}