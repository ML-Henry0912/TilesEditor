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

    /// <summary>
    /// 子類別要實作自己的初始材質
    /// </summary>
    protected abstract Material CreateDefaultMaterial();
}
