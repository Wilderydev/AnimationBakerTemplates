using System.Collections.Generic;
using System.Linq;
using AnimationBakerTemplates.Base;
using UnityEngine;

namespace AnimationBakerTemplates.GPU
{
    public class GPUAnimationBake : MonoBehaviour
    {
        public string directName = "GPU";
        public GPUAnimationBakeData baker;
        public SkinnedMeshRenderer[] skins;
        public AnimationClip clip;
        public Texture mainTexture;

        // data-set
        internal readonly List<Texture2D> m_PositionTextures = new List<Texture2D>();
        internal readonly List<Texture2D> m_NormalTextures = new List<Texture2D>();
        internal readonly List<Texture2D> m_TangensTextures = new List<Texture2D>();
        // GPU runtime shader
        private static readonly int s_MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int s_PosTex = Shader.PropertyToID("_PosTex");
        private static readonly int s_NmlTex = Shader.PropertyToID("_NmlTex");
        private static readonly int s_Length = Shader.PropertyToID("_Length");
        private static readonly int s_Loop = Shader.PropertyToID("_Loop");
        // Compute Shader
        private static readonly int s_VertCount = Shader.PropertyToID("VertCount");
        private static readonly int s_Info = Shader.PropertyToID("Info");
        private static readonly int s_OutPosition = Shader.PropertyToID("OutPosition");
        private static readonly int s_OutNormal = Shader.PropertyToID("OutNormal");
        private static readonly int s_OutTangent = Shader.PropertyToID("OutTangent");

        [ContextMenu("Bake")]
        public void BakeModel()
        {
            foreach (var skin in skins) BakeSingleMesh(skin);
            GPUAnimation.CreateFromBake(this);
        }

        private void BakeSingleMesh(SkinnedMeshRenderer skin)
        {
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
            var kernel = baker.infoTexGen.FindKernel("CSMain");
            baker.infoTexGen.GetKernelThreadGroupSizes(kernel, out var x, out var y, out _);
            baker.infoTexGen.SetInt(s_VertCount, vCount);
            baker.infoTexGen.SetBuffer(kernel, s_Info, buffer);
            baker.infoTexGen.SetTexture(kernel, s_OutPosition, pRt);
            baker.infoTexGen.SetTexture(kernel, s_OutNormal, nRt);
            baker.infoTexGen.SetTexture(kernel, s_OutTangent, tRt);
            baker.infoTexGen.Dispatch(kernel, vCount / (int)x + 1, frames / (int)y + 1, 1);
            buffer.Release();
            // create asset
            var posTex = RenderTextureToTexture2D.Convert(pRt);
            posTex.name = pRt.name;
            var normTex = RenderTextureToTexture2D.Convert(nRt);
            normTex.name = nRt.name;
            var tanTex = RenderTextureToTexture2D.Convert(tRt);
            tanTex.name = tRt.name;
            m_PositionTextures.Add(posTex);
            m_NormalTextures.Add(normTex);
            m_TangensTextures.Add(tanTex);
            // if (clip.wrapMode == WrapMode.Loop)
            // {
            //     mat.SetFloat(s_Loop, 1f);
            //     mat.EnableKeyword("ANIM_LOOP");
            // }
        }

    }
}