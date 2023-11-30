using UnityEngine;
using System.IO;
using UnityEditor;

public class MaterialSaver : MonoBehaviour
{
    public Material materialToSave;
    public string saveFolderPath = "Assets/SavedMaterials/";
    public string saveMaterialName = "EnemySimple";
    public void SaveMaterial()
    {
        if (materialToSave == null)
        {
            Debug.LogError("Material not assigned!");
            return;
        }
        string materialName = saveMaterialName + ".mat"; 
        string positionTextureName = saveMaterialName + "AnimationPosition.asset"; 
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }
        string savePathMat = Path.Combine(saveFolderPath, materialName);
        Material savedMaterial = new Material(materialToSave);
        AssetDatabase.CreateAsset(savedMaterial, savePathMat);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Material saved at: " + savePathMat);
        string savePathText = Path.Combine(saveFolderPath, positionTextureName);
        Texture2D positionTexture = (Texture2D)materialToSave.GetTexture("_PosTex");
        AssetDatabase.CreateAsset(positionTexture, savePathText);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Material saved at: " + savePathText);
    }
}

