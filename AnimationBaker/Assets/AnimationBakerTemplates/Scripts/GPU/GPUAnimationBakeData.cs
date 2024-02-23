using UnityEngine;

namespace AnimationBakerTemplates.GPU
{
    [CreateAssetMenu(fileName = "GPUAnimationBakeData", menuName = "GPU/GPUAnimationBakeTemplate", order = 1)]
    public class GPUAnimationBakeData : ScriptableObject
    {
        public ComputeShader infoTexGen;
        public Material template;
        public Shader playShader;
    }
}