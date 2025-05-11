using UnityEngine;

[RequireComponent(typeof(Collider), typeof(MeshRenderer))]
public abstract class GizmoBase : MonoBehaviour
{
    public Color baseColor;

    protected Material material;
    protected MeshRenderer meshRenderer;

    protected virtual void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        material = meshRenderer.material;
    }

    public virtual void SetMaterialColor(Color color)
    {
        if (material == null)
        {
            material = CreateDefaultMaterial();
            meshRenderer.material = material;
        }

        material.color = color;
    }

    public void ResetColor()
    {
        SetMaterialColor(baseColor);
    }

    protected abstract Material CreateDefaultMaterial();
}
