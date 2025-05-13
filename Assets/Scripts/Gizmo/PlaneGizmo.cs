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
        public void Initialize(GizmoType type, TransformGizmo gizmo)
        {
            this.type = type;
            baseColor = gizmo.gizmoColors[(int)type];
            SetMaterialColor(baseColor);
            this.theGizmo = gizmo;
            this.cam = gizmo.cam;
            gameObject.SetActive(ShouldBeActive());
        }


        public void OnDrag()
        {
            if (theGizmo == null || theGizmo.target == null || cam == null) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Plane dragPlane = GetDragPlane(theGizmo.transform, theGizmo.target.position);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 currentPoint = ray.GetPoint(enter);
                Vector3 delta = currentPoint - theGizmo.dragStartPos;
                if (delta.magnitude < 100f)
                    theGizmo.target.position = theGizmo.objectStartPos + delta;
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
            throw new System.NotImplementedException();
        }
    }
}
