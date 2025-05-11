using UnityEngine;

// =============================================
// 檔案名稱：GizmoBase.cs
// 功能說明：Gizmo 的基底類別，提供共用屬性與方法，
//          供其他 Gizmo（如軸、平面、旋轉等）繼承使用。
// =============================================
// [RequireComponent(typeof(Collider))]
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
