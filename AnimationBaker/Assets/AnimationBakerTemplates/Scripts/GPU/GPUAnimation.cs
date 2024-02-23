using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimationBakerTemplates.GPU
{
    public class GPUAnimation : MonoBehaviour
    {

        internal GameObject rendererGameObject;
        // serialize
        [SerializeField] private float loop = 0;
        [SerializeField] private float length = 0;
        [SerializeField] private bool enableLooping = false;
        public Material materialTemplate;
        public GPUAnimationData data;
        // internal usage
        private Texture m_MainTexture;
        private Material[] m_MaterialTemplateObject;
        private Texture2D[] m_PixelMaterialTexture2D = {};
        private Texture2D[] m_PositionMaterialTexture2D = {};
        private Texture2D[] m_NormalMaterialTexture2D = {};
        //
        private static readonly int s_MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int s_PosTex = Shader.PropertyToID("_PosTex");
        private static readonly int s_NmlTex = Shader.PropertyToID("_NmlTex");
        private static readonly int s_Length = Shader.PropertyToID("_Length");
        private static readonly int s_Loop = Shader.PropertyToID("_Loop");
        //
        private List<MeshRenderer> m_Renderers;
        private const double Tolerance = 0.0001d;

        #region Property
        internal float loopProperty
        {
            get => loop;
            set
            {
                if (Math.Abs(loop - value) > Tolerance)
                {
                    SetLoop(value);
                }
                loop = value;
            }
        } 
        internal float lengthProperty
        {
            get => length;
            set
            {
                if (Math.Abs(length - value) > Tolerance)
                {
                    SetLength(value);
                }
                length = value;
            }
        }
        #endregion

        public void Load(Texture main, Texture2D[] position, Texture2D[] normal, Texture2D[] tang)
        {
            m_MainTexture = main;
            m_PositionMaterialTexture2D = position;
            m_NormalMaterialTexture2D = normal;
            m_PixelMaterialTexture2D = tang;
            data.materialTemplateObject = m_MaterialTemplateObject;
            loop = data.loop;
            length = data.length;
            enableLooping = data.enableLooping;
            data.materialTemplate = materialTemplate;
            data.main = m_MainTexture;
            data.normalMaterialTexture2D = m_NormalMaterialTexture2D;
            data.positionMaterialTexture2D = m_PositionMaterialTexture2D;
            data.tangMaterialTexture2D = m_PixelMaterialTexture2D;
        }
        public void Init(MeshRenderer rendererMesh)
        {
            MaterialCopy();
            m_Renderers.Add(rendererMesh);
            MaterialCopyToRenderer();
            
        }
        public void Init(List<MeshRenderer> renderers)
        {
            m_MaterialTemplateObject = new Material[renderers.Count];
            MaterialCopy();
            m_Renderers = renderers;
            MaterialCopyToRenderer();
        }
        private void MaterialCopyToRenderer()
        {
            for (int i = 0; i < m_Renderers.Count; i++)
                m_Renderers[i].material = data.materialTemplateObject[i];
        }
        private void MaterialCopy()
        {
            for (int i = 0; i < m_MaterialTemplateObject.Length; i++)
            {
                m_MaterialTemplateObject[i] = CreateAnimatedObjects.CopyMaterial(materialTemplate);
                
                m_MaterialTemplateObject[i].SetTexture(s_MainTex, m_MainTexture);
                m_MaterialTemplateObject[i].SetTexture(s_PosTex, m_PositionMaterialTexture2D[i] );
                m_MaterialTemplateObject[i].SetTexture(s_NmlTex, m_NormalMaterialTexture2D[i]);

                m_MaterialTemplateObject[i].SetFloat(s_Loop, loop );
                if (enableLooping)
                { 
                    m_MaterialTemplateObject[i].SetFloat(s_Loop, 1f);
                    m_MaterialTemplateObject[i].EnableKeyword("ANIM_LOOP");
                }
            }
            data.materialTemplateObject = m_MaterialTemplateObject;
            loopProperty = loop;
            lengthProperty = length;
        }
        private void SetLoop(float value)
        {
            foreach (var t in m_MaterialTemplateObject)
            {
                t.SetFloat(s_Loop, value);
            }
        }
        private void SetLength(float value)
        {
            foreach (var t in m_MaterialTemplateObject)
            {
                t.SetFloat(s_Length, value);
            }
        }
        public static void CreateFromBake(GPUAnimationBake gpuAnimationBake)
        {
            GPUAnimationCreatorExtend.CreateFrom(gpuAnimationBake);
        }
    }
}