using UnityEngine;

namespace TilesEditor
{
    // =============================================
    // 檔案名稱：GizmoBase.cs
    // 1. 作為所有 Gizmo 元件的基礎類別。
    // 2. 提供共用的顏色管理與材質設定功能。
    // 3. 定義虛擬方法供子類別實作，包含顯示條件判斷。
    // 4. 使用 MaterialPropertyBlock 來設定顏色，避免材質實例化。
    // 5. 提供基礎的顏色重置功能。
    // =============================================
    public abstract class GizmoBase : MonoBehaviour
    {
        protected TransformGizmo gizmo;
        public Color baseColor;
        protected MaterialPropertyBlock propertyBlock;

        public virtual void SetMaterialColor(Color color)
        {
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
            color.a = 0.8f; // 預設 80% 透明度
            propertyBlock.SetColor("_Color", color);
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        public void ResetColor()
        {
            SetMaterialColor(baseColor);
        }

        // 可覆寫的顯示條件，預設為 true
        public virtual bool ShouldBeActive()
        {
            return true;
        }
    }
}
