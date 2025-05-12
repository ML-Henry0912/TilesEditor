// =============================================
// 檔案名稱：AxisGizmo.cs
// 功能說明：用於顯示與操作三軸（X, Y, Z）方向的 Gizmo，
//          讓使用者可以拖曳某一軸進行精確的物件操作。
//          本元件支援安全重複初始化，Initialize 可多次呼叫以覆蓋狀態，不會產生重複資源。
// =============================================
using UnityEngine;

public class AxisGizmo : GizmoBase
{
    public enum Axis { X, Y, Z }
    public Axis axis;

    [HideInInspector] public Vector3 WorldDirection;

    Camera cam;
    float length;
    float thickness;

    public void Initialize(Axis axisType, Color color, Camera cam, float length, float thickness)
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
        this.cam = cam;
        this.length = length;
        this.thickness = thickness;
    }

    protected override Material CreateDefaultMaterial()
    {
        return new Material(Shader.Find("Unlit/Color"));
    }

    public bool IsMouseOnAxisGizmo(Vector3 axisOrigin, Vector3 axisDir)
    {
        Vector3 a = axisOrigin - axisDir * length * 0.5f;
        Vector3 b = axisOrigin + axisDir * length * 0.5f;
        Vector2 screenA = cam.WorldToScreenPoint(a);
        Vector2 screenB = cam.WorldToScreenPoint(b);
        Vector2 mouse = Input.mousePosition;
        float t = Mathf.Clamp01(Vector2.Dot(mouse - screenA, screenB - screenA) / (screenB - screenA).sqrMagnitude);
        Vector2 closest = screenA + t * (screenB - screenA);
        float dist = (mouse - closest).magnitude;
        return dist < thickness;
    }
}
