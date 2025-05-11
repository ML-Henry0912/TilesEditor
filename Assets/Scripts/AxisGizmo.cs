using UnityEngine;

public class AxisGizmo : GizmoBase
{
    public enum Axis { X, Y, Z }
    public Axis axis;

    [HideInInspector] public Vector3 WorldDirection;

    public void Initialize(Axis axisType, Color color)
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
    }

    protected override Material CreateDefaultMaterial()
    {
        return new Material(Shader.Find("Unlit/Color"));
    }

    public Plane GetDragPlane(Camera cam, Transform gizmoRoot, Vector3 origin)
    {
        Vector3 axisDir = gizmoRoot.TransformDirection(WorldDirection);
        Vector3 normal = Vector3.Cross(cam.transform.forward, axisDir);
        if (normal.sqrMagnitude < 0.001f) normal = cam.transform.up;
        return new Plane(normal.normalized, origin);
    }
}
