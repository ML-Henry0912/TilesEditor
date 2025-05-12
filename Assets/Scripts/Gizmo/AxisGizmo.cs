// =============================================
// 檔案名稱：AxisGizmo.cs
// 1. 用於顯示與操作三軸（X, Y, Z）方向的 Gizmo。
// 2. 讓使用者可以拖曳某一軸進行精確的物件操作。
// 3. 使用 CapsuleCollider 進行滑鼠事件偵測，確保互動的準確性。
// 4. 本元件為 TransformGizmo 的子物件，請勿手動移除或更改父子結構。
// =============================================
using UnityEngine;

namespace TilesEditor
{
    public class AxisGizmo : GizmoBase
    {
        public enum Axis { X, Y, Z }
        public Axis axis;

        [HideInInspector] public Vector3 WorldDirection;

        private bool isHovered = false;

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

            // 確保有 Collider 元件
            var collider = GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<CapsuleCollider>();
            }

            // 設定 Collider 屬性
            // 考慮到 localScale 的影響，需要調整實際尺寸
            float actualLength = length * transform.localScale.y;
            float actualRadius = thickness * 0.5f * transform.localScale.x;

            collider.radius = actualRadius;
            collider.height = actualLength;
            collider.direction = 2; // Z軸方向
            collider.center = Vector3.zero;
        }

        private void OnMouseEnter()
        {
            isHovered = true;
            SetMaterialColor(Color.yellow);
        }

        private void OnMouseExit()
        {
            isHovered = false;
            SetMaterialColor(baseColor);
        }

        public bool IsHovered()
        {
            return isHovered;
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
