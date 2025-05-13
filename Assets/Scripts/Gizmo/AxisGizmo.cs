// =============================================
// 檔案名稱：AxisGizmo.cs
// 1. 用於顯示與操作三軸（X, Y, Z）方向的 Gizmo。
// 2. 讓使用者可以拖曳某一軸進行精確的物件操作。
// 3. 使用 CapsuleCollider 進行滑鼠事件偵測，提供直覺的互動體驗。
// 4. 實作 ShouldBeActive 方法，根據 TransformGizmo 的設定決定是否顯示。
// 5. 本元件為 TransformGizmo 的子物件，請勿手動移除或更改父子結構。
// =============================================
using UnityEngine;
using static TilesEditor.iGizmo;

namespace TilesEditor
{
    public class AxisGizmo : GizmoBase, iGizmo
    {
        [HideInInspector] public Vector3 WorldDirection;

        public void Initialize(GizmoType type, TransformGizmo gizmo)
        {
            this.type = type;
            baseColor = gizmo.gizmoColors[(int)type];
            SetMaterialColor(baseColor);
            cam = gizmo.cam;

            switch (this.type)
            {
                case GizmoType.X: WorldDirection = Vector3.right; break;
                case GizmoType.Y: WorldDirection = Vector3.up; break;
                case GizmoType.Z: WorldDirection = Vector3.forward; break;
            }
            this.gizmo = gizmo;

            gameObject.SetActive(ShouldBeActive());
        }


        public void ResetColor()
        {
            SetMaterialColor(baseColor);
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


        // 判斷此 handle 是否該顯示
        public bool ShouldBeActive()
        {
            if (gizmo == null) return false;
            switch (type)
            {
                case GizmoType.X: return gizmo.translateX;
                case GizmoType.Y: return gizmo.translateY;
                case GizmoType.Z: return gizmo.translateZ;
                default: return false;
            }
        }

        public void SetInvisible(bool value)
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = !value;
            }
        }

        public void OnDrag()
        {
            if (gizmo == null || gizmo.target == null || cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Vector3 axisDir = gizmo.transform.TransformDirection(WorldDirection).normalized;
            Vector3 current = gizmo.GetClosestPointOnAxis(ray, gizmo.target.position, axisDir);
            Vector3 delta = Vector3.Project(current - gizmo.dragStartPos, axisDir);
            if (delta.magnitude < 100f)
                gizmo.target.position = gizmo.objectStartPos + delta;
        }
    }
}
