using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimationBakerTemplates.GPU
{
    public static class GPUAnimationCreatorExtend
    {
        internal static void CreateFrom(GPUAnimationBake bake)
        {
            try
            {
                var folderPath = Path.Combine("Assets", bake.directName);
                if (!AssetDatabase.IsValidFolder(folderPath))
                    AssetDatabase.CreateFolder("Assets", bake.directName);
                var localFolder = Path.Combine(folderPath, $"{bake.clip.name}"); 
                if (!AssetDatabase.IsValidFolder(localFolder))
                    AssetDatabase.CreateFolder(folderPath, $"{bake.clip.name}");
                                
                GPUAnimationData data = ScriptableObject.CreateInstance<GPUAnimationData>();
                GameObject animation = new GameObject(bake.clip.name);
                GPUAnimation animationComponentData = animation.AddComponent<GPUAnimation>();
                GameObject[] meshes = new GameObject[bake.skins.Length];
                List<MeshRenderer> renderers = new List<MeshRenderer>();
                
                animation.transform.position = Vector3.zero;
                animationComponentData.data = data;
                animationComponentData.data.loop = 1;
                
                animationComponentData.data.length = bake.clip.length;
                // animationComponentData.data.enableLooping = bake.clip.wrapMode == WrapMode.Loop;
                animationComponentData.data.enableLooping = true;
                animationComponentData.data.shader = bake.baker.playShader;
                animationComponentData.materialTemplate = bake.baker.template;

                animationComponentData.Load(bake.mainTexture, bake.m_PositionTextures.ToArray(), bake.m_NormalTextures.ToArray(),
                    bake.m_TangensTextures.ToArray());
                

                for ( int i = 0 ; i < meshes.Length; i ++)
                {
                    meshes[i] = new GameObject($"mesh:[index={i}][name={bake.skins[i].sharedMesh.name}]");
                    var render = meshes[i].AddComponent<MeshRenderer>();
                    var filter = meshes[i].AddComponent<MeshFilter>();
                    
                    filter.mesh = bake.skins[i].sharedMesh;
                    renderers.Add(render);
                    meshes[i].transform.SetParent(animation.transform);
                    meshes[i].transform.position = Vector3.zero;
                    meshes[i].transform.rotation = Quaternion.Euler(-90, 0, 0);
                }

                animationComponentData.Init(renderers);
                

                
                for (int i = 0; i < animationComponentData.data.materialTemplateObject.Length; i++)
                {
                    AssetDatabase.CreateAsset(animationComponentData.data.materialTemplateObject[i], Path.Combine(localFolder,$"{animation.name}_{bake.skins[i].name}_Mat") + ".asset");
                    renderers[i].sharedMaterial = animationComponentData.data.materialTemplateObject[i];
                    Debug.Log(animationComponentData.data.name);
                    AssetDatabase.CreateAsset(animationComponentData.data.positionMaterialTexture2D[i], Path.Combine(localFolder,$"{animation.name}_{bake.skins[i].name}_Tex_Posistion") + ".asset");
                    AssetDatabase.CreateAsset(animationComponentData.data.normalMaterialTexture2D[i], Path.Combine(localFolder,$"{animation.name}_{bake.skins[i].name}_Tex_Normal") + ".asset");
                    AssetDatabase.CreateAsset(animationComponentData.data.tangMaterialTexture2D[i], Path.Combine(localFolder,$"{animation.name}_{bake.skins[i].name}_Tex_Tang") + ".asset");
                }
                AssetDatabase.CreateAsset(data, Path.Combine(localFolder,$"{animation.name}") + ".asset");
                PrefabUtility.SaveAsPrefabAsset(animation,  Path.Combine(localFolder, animation.name) + ".prefab");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                var gameObject = PrefabUtility.LoadPrefabContents(Path.Combine(localFolder, animation.name) + ".prefab");
                
                Object.Instantiate(gameObject, animation.transform.position, animation.transform.rotation);
                Object.DestroyImmediate(animation);
                Debug.Log($"<color=#2AE149>[successfully baked animation]\n[name]{bake.clip.name}]\n[path=<color=#E10092><b>{localFolder}</b></color>]</color>");
            }
            catch (Exception e)
            {
                Debug.Log($"<color=#E10016>[FILED baked animation]\n[name]{bake.clip.name}]\n[ERROR=<color=#E15A00><b>{e.ToString()}</b></color>]</color>");
                // ignored
            }
        }
    }
}