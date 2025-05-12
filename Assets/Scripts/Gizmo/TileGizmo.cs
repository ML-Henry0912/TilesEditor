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

            SetPlaneGizmoProperties(PlaneGizmo.PlaneType.XY, new Vector3(0.0f, 0.0f, -0.6f), new Color(0.0f, 0.0f, 0.0f, 0.0f));
            //UpdatePlaneGizmo(this.xyHandle, new Vector3(0.0f, 0.0f, -0.6f), new Color(1f, 1f, 1f, 0f));
        }
    }
}