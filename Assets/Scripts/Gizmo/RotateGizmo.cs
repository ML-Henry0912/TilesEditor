// =============================================
// 檔案名稱：RotateGizmo.cs
// 1. 用於旋轉操作的 Gizmo，顯示三個圓環。
// 2. 讓使用者可以分別對 X、Y、Z 軸進行精確旋轉。
// 3. 使用橢圓投影計算實現精確的滑鼠互動，支援任意視角操作。
// 4. 本元件為 TransformGizmo 的子物件，請勿手動移除或更改父子結構。
// =============================================
using UnityEngine;
using static TilesEditor.iGizmo;

namespace TilesEditor
{
    public class RotateGizmo : GizmoBase, iGizmo
    {
        [HideInInspector]
        Vector3 WorldAxis;
        private Vector3 rotateStartPoint;
        private Quaternion objectStartRot;
        private Plane rotationPlane;
        private Transform target;

        public void Initialize(GizmoType type, TransformGizmo gizmoRoot)
        {
            this.gizmoRoot = gizmoRoot;
            this.type = type;
            baseColor = gizmoRoot.gizmoColors[(int)type];
            SetMaterialColor(baseColor);
            target = gizmoRoot.target;

            switch (type)
            {
                case GizmoType.ROT_X: WorldAxis = Vector3.right; break;
                case GizmoType.ROT_Y: WorldAxis = Vector3.up; break;
                case GizmoType.ROT_Z: WorldAxis = Vector3.forward; break;
            }

            this.cam = gizmoRoot.cam;
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
            if (rotationPlane.Raycast(ray, out float enter))
            {
                Vector3 currentPoint = ray.GetPoint(enter);
                Vector3 startDir = (rotateStartPoint - target.position).normalized;
                Vector3 currentDir = (currentPoint - target.position).normalized;

                Quaternion deltaRotation = Quaternion.FromToRotation(startDir, currentDir);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

                if (Vector3.Dot(axis, WorldAxis) < 0f)
                    angle = -angle;

                target.rotation = objectStartRot * Quaternion.AngleAxis(angle, WorldAxis);

            }
        }

        public void OnHover()
        {
            if (Input.GetMouseButtonDown(0))
            {
                gizmoRoot.action = OnDrag;
                rotationPlane = new Plane(WorldAxis, target.position);
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (rotationPlane.Raycast(ray, out float enter))
                {
                    rotateStartPoint = ray.GetPoint(enter);
                    objectStartRot = target.rotation;
                }
            }
            else if (!IsHovered())
            {
                ResetColor();
                gizmoRoot.EndDrag();
            }
        }
    }

}

