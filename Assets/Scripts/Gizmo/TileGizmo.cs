using UnityEngine;

namespace TilesEditor
{
    public class TileGizmo : TransformGizmo
    {

        public void InitializeTile(Transform targetTile, Camera cam, GizmoMaterials materials)
        {
            base.Initialize(targetTile, cam, materials);

            translateX = true;
            translateY = true;
            translateZ = false;

            rotateX = false;
            rotateY = false;
            rotateZ = true;

            SetPlaneGizmoProperties(iGizmo.GizmoType.XY, new Vector3(0.0f, 0.0f, -0.6f));
            SetPlaneGizmoInvisible(iGizmo.GizmoType.XY);

            //DragPlane(PlaneGizmo.PlaneType.XY);

        }
    }
}