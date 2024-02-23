using UnityEngine;

namespace AnimationBakerTemplates.GPU
{
    [System.Serializable]
    public class GPUAnimationData : ScriptableObject
    {
        public bool enableLooping = true;
        public float loop = 0;
        public float length = 0;
        public Shader shader;
        public Texture main;
        public Material materialTemplate;
        public Material[] materialTemplateObject;
        public Texture2D[] tangMaterialTexture2D;
        public Texture2D[] positionMaterialTexture2D;
        public Texture2D[] normalMaterialTexture2D;
    }
}