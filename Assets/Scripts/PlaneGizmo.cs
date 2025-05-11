using UnityEngine;

public class PlaneGizmo : GizmoBase
{
    public enum PlaneType { XY, XZ, YZ }
    public PlaneType planeType;

    public void Initialize(PlaneType type, Color color)
    {
        planeType = type;
        baseColor = color;
        SetMaterialColor(color);
    }

    protected override Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Unlit/Transparent"));
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // 雙面可見
        return mat;
    }

    public Plane GetDragPlane(Transform gizmoRoot, Vector3 origin)
    {
        switch (planeType)
        {
            case PlaneType.XY: return new Plane(gizmoRoot.forward, origin);
            case PlaneType.XZ: return new Plane(gizmoRoot.up, origin);
            case PlaneType.YZ: return new Plane(gizmoRoot.right, origin);
            default: return new Plane(Vector3.up, origin);
        }
    }
}
