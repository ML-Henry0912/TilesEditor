using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class GizmoBase : MonoBehaviour
{
    public Color baseColor;
    protected Material material;

    public virtual void SetMaterialColor(Color color)
    {
        if (material == null)
        {
            var renderer = GetComponent<MeshRenderer>();
            material = CreateDefaultMaterial();
            renderer.material = material;
        }
        material.color = color;
    }

    public void ResetColor()
    {
        SetMaterialColor(baseColor);
    }

    protected abstract Material CreateDefaultMaterial();
}
