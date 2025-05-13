// =============================================
// 檔案名稱：PlaneGizmo.cs
// 1. 用於在特定平面（如 XY、XZ、YZ）上進行拖曳操作的 Gizmo。
// 2. 讓使用者可以在 2D 平面上移動物件。
// 3. 支援設定完全透明模式，可用於特殊視覺效果。
// 4. 使用 BoxCollider 進行滑鼠事件偵測，提供直覺的互動體驗。
// 5. 本元件為 TransformGizmo 的子物件，請勿手動移除或更改父子結構。
// =============================================
using UnityEngine;
using static TilesEditor.iGizmo;

namespace TilesEditor
{
    public class PlaneGizmo : GizmoBase, iGizmo
    {
        private Transform target;
        private Vector3 dragStartPos;
        private Vector3 objectStartPos;

        public void Initialize(GizmoType type, TransformGizmo gizmoRoot)
        {
            this.type = type;
            baseColor = gizmoRoot.gizmoColors[(int)type];
            SetMaterialColor(baseColor);
            this.gizmoRoot = gizmoRoot;
            this.cam = gizmoRoot.cam;
            this.target = gizmoRoot.target;
            gameObject.SetActive(ShouldBeActive());
        }

        public void OnDrag()
        {
            if (Input.GetMouseButtonUp(0))
            {
                ResetColor();
                gizmoRoot.EndDrag();
                return;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane dragPlane = GetDragPlane(transform, target.position);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 currentPoint = ray.GetPoint(enter);
                Vector3 delta = currentPoint - dragStartPos;
                if (delta.magnitude < 100f)
                    target.position = objectStartPos + delta;
            }
        }

        public Plane GetDragPlane(Transform gizmoTransform, Vector3 center)
        {
            switch (type)
            {
                case GizmoType.XY: return new Plane(gizmoTransform.forward, center);
                case GizmoType.XZ: return new Plane(gizmoTransform.up, center);
                case GizmoType.YZ: return new Plane(gizmoTransform.right, center);
                default: return new Plane(Vector3.up, center);
            }
        }

        public void OnHover()
        {
            if (Input.GetMouseButtonDown(0))
            {
                gizmoRoot.action = OnDrag;
                Plane dragPlane = GetDragPlane(transform, target.position);
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (!dragPlane.Raycast(ray, out float enter))
                {
                    dragPlane = new Plane(-dragPlane.normal, dragPlane.distance);
                    if (!dragPlane.Raycast(ray, out enter))
                        return;
                }
                dragStartPos = ray.GetPoint(enter);
                objectStartPos = target.position;
            }
            else if (!IsHovered())
            {
                ResetColor();
                gizmoRoot.EndDrag();
            }
        }
    }
}
