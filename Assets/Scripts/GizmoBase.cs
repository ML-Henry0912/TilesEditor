using UnityEngine;

// =============================================
// 檔案名稱：GizmoBase.cs
// 1. Gizmo 的基底類別，提供共用屬性與方法。
// 2. 供其他 Gizmo（如軸、平面、旋轉等）繼承使用。
// 3. 本元件支援安全重複初始化，Initialize 可多次呼叫以覆蓋狀態，不會產生重複資源。
// 4. 所有 Gizmo 共用同一份靜態材質，顏色與透明度（80%）以 MaterialPropertyBlock 設定，避免記憶體浪費。
// =============================================
public abstract class GizmoBase : MonoBehaviour
{
    public Color baseColor;
    [Tooltip("請在 Inspector 指定一個 Unlit/Color 或 Unlit/Texture 材質，避免 WebGL 找不到 Shader。")]
    public static Material sharedMaterial;
    protected MaterialPropertyBlock propertyBlock;

    public virtual void SetMaterialColor(Color color)
    {
        if (sharedMaterial == null)
        {
            Debug.LogError("GizmoBase.sharedMaterial 尚未指定，請在 Inspector 指定一個 Unlit/Color 或 Unlit/Texture 材質，否則 WebGL 可能無法顯示 Gizmo！");
            return;
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
            Debug.LogError("GizmoBase.sharedMaterial 尚未指定，請在 Inspector 指定一個 Unlit/Color 或 Unlit/Texture 材質，否則 WebGL 可能無法顯示 Gizmo！");
        }
        return sharedMaterial;
    }
}
