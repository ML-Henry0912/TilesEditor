// =============================================
// 檔案名稱：AxisGizmo.cs
// 1. 用於顯示與操作三軸（X, Y, Z）方向的 Gizmo。
// 2. 讓使用者可以拖曳某一軸進行精確的物件操作。
// 3. 本元件支援安全重複初始化，Initialize 可多次呼叫以覆蓋狀態，不會產生重複資源。
// 4. 所有 Gizmo 材質由 ScriptableObject（GizmoMaterials）統一管理，於建立時指定，顏色與透明度（80%）以 MaterialPropertyBlock 設定，避免記憶體浪費與提升一致性。
// 5. 所有互動偵測皆以數學計算為主，不依賴 Collider，確保精確度與效能。
// 6. 本元件為 TransformGizmo 的子物件，請勿手動移除或更改父子結構。
// =============================================
using UnityEngine;

namespace TilesEditor
{
    public class AxisGizmo : GizmoBase
    {
        public enum Axis { X, Y, Z }
        public Axis axis;

        [HideInInspector] public Vector3 WorldDirection;

        Camera cam;
        float length;
        float thickness;

        public void Initialize(Axis axisType, Color color, TransformGizmo gizmo, float length, float thickness)
        {
            axis = axisType;
            baseColor = color;
            SetMaterialColor(color);

            switch (axis)
            {
                case Axis.X: WorldDirection = Vector3.right; break;
                case Axis.Y: WorldDirection = Vector3.up; break;
                case Axis.Z: WorldDirection = Vector3.forward; break;
            }
            this.gizmo = gizmo;
            this.cam = gizmo.cam;
            this.length = length;
            this.thickness = thickness;
        }

        public bool IsMouseOnGizmo(Vector3 axisOrigin, Vector3 axisDir)
        {
            Vector3 a = axisOrigin - axisDir * length * 0.5f;
            Vector3 b = axisOrigin + axisDir * length * 0.5f;
            Vector2 screenA = cam.WorldToScreenPoint(a);
            Vector2 screenB = cam.WorldToScreenPoint(b);
            Vector2 mouse = Input.mousePosition;
            float t = Mathf.Clamp01(Vector2.Dot(mouse - screenA, screenB - screenA) / (screenB - screenA).sqrMagnitude);
            Vector2 closest = screenA + t * (screenB - screenA);
            float dist = (mouse - closest).magnitude;
            return dist < thickness;
        }

        // 判斷此 handle 是否該顯示
        public override bool ShouldBeVisible()
        {
            if (gizmo == null) return false;
            switch (axis)
            {
                case Axis.X: return gizmo.translateX;
                case Axis.Y: return gizmo.translateY;
                case Axis.Z: return gizmo.translateZ;
                default: return false;
            }
        }
    }
}
