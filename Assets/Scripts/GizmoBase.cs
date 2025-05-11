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
    public static Material sharedMaterial;
    protected MaterialPropertyBlock propertyBlock;

    public virtual void SetMaterialColor(Color color)
    {
        if (sharedMaterial == null)
        {
            sharedMaterial = new Material(Shader.Find("Unlit/Color"));
            sharedMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            sharedMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sharedMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            sharedMaterial.SetInt("_ZWrite", 0);
            sharedMaterial.DisableKeyword("_ALPHATEST_ON");
            sharedMaterial.EnableKeyword("_ALPHABLEND_ON");
            sharedMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            sharedMaterial.renderQueue = 3000;
        }
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();
        color.a = 0.8f; // 預設 80% 透明度
        propertyBlock.SetColor("_Color", color);
        var renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = sharedMaterial;
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    public void ResetColor()
    {
        SetMaterialColor(baseColor);
    }

    protected virtual Material CreateDefaultMaterial()
    {
        if (sharedMaterial == null)
        {
            sharedMaterial = new Material(Shader.Find("Unlit/Color"));
            sharedMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            sharedMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sharedMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            sharedMaterial.SetInt("_ZWrite", 0);
            sharedMaterial.DisableKeyword("_ALPHATEST_ON");
            sharedMaterial.EnableKeyword("_ALPHABLEND_ON");
            sharedMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            sharedMaterial.renderQueue = 3000;
        }
        return sharedMaterial;
    }
}
