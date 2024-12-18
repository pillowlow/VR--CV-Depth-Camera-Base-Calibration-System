using UnityEngine;

public class MaterialChanger : MonoBehaviour
{
    [SerializeField] private Material targetMaterial;

    public void ChangeMaterialForAllChildren(Material newMaterial = null)
    {
        Material materialToUse = newMaterial ?? targetMaterial;
        
        if (materialToUse == null)
        {
            Debug.LogWarning("No material assigned!");
            return;
        }

        // Get all renderers in children (including inactive)
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>(true);
        
        foreach (Renderer renderer in childRenderers)
        {
            // Change all materials in the renderer
            Material[] materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = materialToUse;
            }
            renderer.sharedMaterials = materials;
        }
    }

    // Optional method to change to the assigned material in inspector
    public void ChangeToAssignedMaterial()
    {
        ChangeMaterialForAllChildren(targetMaterial);
    }
}