using UnityEngine;

public class CreateAnimatedObjects : MonoBehaviour
{
    public GameObject objectPrefab;
    public Material Material;
    public Texture Texture;
    private MaterialSaver _saver;
    void Start()
    {

        for (int i = 0; i < 5; i++)
        {
            GameObject newObj = Instantiate(objectPrefab);
            newObj.transform.position = new Vector3(i * 2, 0, 0); 
            Renderer renderer = newObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material newMaterial = CopyMaterial(Material); 
                renderer.material = newMaterial;
                renderer.material.mainTexture = GetUniqueTexture();
            }
        }
    }
    Texture2D GetUniqueTexture()
    {
        return GetCopyOfTexture((Texture2D)Texture);
    }
    
    public static Texture2D GetCopyOfTexture(Texture2D originalTexture)
    {
        if (originalTexture == null)
        {
            Debug.LogError("Original texture is null.");
            return null;
        }
        Texture2D copyTexture = new Texture2D(originalTexture.width, originalTexture.height);
        copyTexture.SetPixels(originalTexture.GetPixels());
        copyTexture.Apply();
        return copyTexture;
    }
    
    public static Material CopyMaterial(Material originalMaterial)
    {
        if (originalMaterial == null)
        {
            Debug.LogError("Original material is null.");
            return null;
        }
        Material copiedMaterial = new Material(originalMaterial.shader); 
        copiedMaterial.CopyPropertiesFromMaterial(originalMaterial);
        return copiedMaterial;
    }
}