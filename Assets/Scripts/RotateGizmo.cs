using UnityEngine;

public class RotateGizmo : GizmoBase
{
    public enum Axis { X, Y, Z }
    public Axis axis;

    [HideInInspector]
    public Vector3 WorldAxis;

    public void Initialize(Axis axisType, Color color)
    {
        axis = axisType;
        baseColor = color;
        SetMaterialColor(color);

        switch (axis)
        {
            case Axis.X: WorldAxis = Vector3.right; break;
            case Axis.Y: WorldAxis = Vector3.up; break;
            case Axis.Z: WorldAxis = Vector3.forward; break;
        }
    }

    protected override Material CreateDefaultMaterial()
    {
        return new Material(Shader.Find("Unlit/Color"));
    }
}
