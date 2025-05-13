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
        private Vector3 dragStartPos;
        private Vector3 objectStartPos;
        private Transform target;

        public void Initialize(GizmoType type, TransformGizmo gizmoRoot)
        {
            this.type = type;
            baseColor = gizmoRoot.gizmoColors[(int)type];
            SetMaterialColor(baseColor);
            cam = gizmoRoot.cam;
            target = gizmoRoot.target;

            base.gizmoRoot = gizmoRoot;

            gameObject.SetActive(ShouldBeActive());
        }

        public void OnDrag()
        {
            if (Input.GetMouseButton(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                Vector3 axisDir = transform.TransformDirection(Vector3.up).normalized;
                Vector3 current = GetClosestPointOnAxis(ray, objectStartPos, axisDir);
                Vector3 delta = Vector3.Project(current - dragStartPos, axisDir);
                if (delta.magnitude < 100f)
                    target.position = objectStartPos + delta;

            }
            else
            {
                ResetColor();
                gizmoRoot.EndDrag();

            }

        }

        public void OnHover()
        {
            if (Input.GetMouseButton(0))
            {
                gizmoRoot.OnDrag(this);
                Vector3 axisDir = transform.TransformDirection(Vector3.up).normalized;
                dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
                objectStartPos = target.position;
            }
            else if (!IsHovered())
            {
                ResetColor();
                gizmoRoot.EndDrag();
            }
        }

        public static Vector3 GetClosestPointOnAxis(Ray ray, Vector3 axisOrigin, Vector3 axisDir)
        {
            Vector3 p1 = ray.origin;
            Vector3 d1 = ray.direction;
            Vector3 p2 = axisOrigin;
            Vector3 d2 = axisDir;

            float a = Vector3.Dot(d1, d1);
            float b = Vector3.Dot(d1, d2);
            float e = Vector3.Dot(d2, d2);
            float d = a * e - b * b;

            if (Mathf.Abs(d) < 0.0001f)
                return axisOrigin;

            Vector3 r = p1 - p2;
            float c = Vector3.Dot(d1, r);
            float f = Vector3.Dot(d2, r);
            float s = (b * f - c * e) / d;

            return p1 + d1 * s;
        }
    }
}
