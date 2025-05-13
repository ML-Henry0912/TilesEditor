// =============================================
// 檔案名稱：AxisGizmo.cs
// 1. 用於顯示與操作三軸（X, Y, Z）方向的 Gizmo。
// 2. 讓使用者可以拖曳某一軸進行精確的物件操作。
// 3. 使用 CapsuleCollider 進行滑鼠事件偵測，提供直覺的互動體驗。
// 4. 實作 ShouldBeActive 方法，根據 TransformGizmo 的設定決定是否顯示。
// 5. 本元件為 TransformGizmo 的子物件，請勿手動移除或更改父子結構。
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

        public void Initialize(Axis axisType, Color color, TransformGizmo gizmo)
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
        public override bool ShouldBeActive()
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
