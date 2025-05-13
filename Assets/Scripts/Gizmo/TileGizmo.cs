using UnityEngine;

namespace TilesEditor
{
    public class TileGizmo : TransformGizmo
    {
        public void InitializeTile(Transform targetTile, Camera cam, GizmoMaterials materials)
        {
            base.Initialize(targetTile, cam, materials);

            // 設定移動軸
            gizmoEnable[0] = true;  // X 軸
            gizmoEnable[1] = true;  // Y 軸
            gizmoEnable[2] = false; // Z 軸

            gizmoEnable[3] = true;
            gizmoEnable[4] = false;
            gizmoEnable[5] = false;

            // 設定旋轉軸
            gizmoEnable[6] = false; // X 軸旋轉
            gizmoEnable[7] = false; // Y 軸旋轉
            gizmoEnable[8] = true;  // Z 軸旋轉

            SetPlaneGizmoProperties(iGizmo.GizmoType.XY, new Vector3(0.0f, 0.0f, -0.6f));
            SetPlaneGizmoInvisible(iGizmo.GizmoType.XY);

            //DragPlane(PlaneGizmo.PlaneType.XY);

        }
    }
}