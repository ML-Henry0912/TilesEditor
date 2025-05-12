// =============================================
// 檔案名稱：RotateGizmo.cs
// 1. 用於旋轉操作的 Gizmo，顯示三個圓環。
// 2. 讓使用者可以分別對 X、Y、Z 軸進行精確旋轉。
// 3. 本元件支援安全重複初始化，Initialize 可多次呼叫以覆蓋狀態，不會產生重複資源。
// 4. 所有 Gizmo 共用同一份靜態材質，顏色與透明度（80%）以 MaterialPropertyBlock 設定，避免記憶體浪費。
// 5. 所有互動偵測皆以數學計算為主，不依賴 Collider，確保精確度與效能。
// 6. 本元件為 TransformGizmo 的子物件，請勿手動移除或更改父子結構。
// =============================================
using UnityEngine;

public class RotateGizmo : GizmoBase
{
    public enum Axis { X, Y, Z }
    public Axis axis;

    [HideInInspector]
    public Vector3 WorldAxis;

    Camera cam;
    float thickness;

    public void Initialize(Axis axisType, Color color, Camera cam, float thickness)
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
        this.cam = cam;
        this.thickness = thickness;
    }

    protected override Material CreateDefaultMaterial()
    {
        return new Material(Shader.Find("Unlit/Color"));
    }

    /// <summary>
    /// 判斷滑鼠是否在螢幕上的旋轉 Gizmo 橢圓環上（不使用 Collider）。
    /// </summary>
    /// <param name="circleCenter">圓心（世界座標）</param>
    /// <param name="circleNormal">圓面法向量（世界座標）</param>
    /// <param name="radius">圓半徑</param>
    /// <returns>滑鼠是否在橢圓環上</returns>
    public bool IsMouseOnGizmo(Vector3 circleCenter, Vector3 circleNormal, float radius)
    {
        var ellipse = EllipseProjectionUtility.ProjectCircleToScreen(cam, circleCenter, circleNormal, radius);
        Vector2 mousePos = Input.mousePosition;
        Vector2 delta = mousePos - ellipse.screenCenter;
        Vector2 majorDir = ellipse.majorAxisDirection.normalized;
        Vector2 minorDir = new Vector2(-majorDir.y, majorDir.x);
        float x = Vector2.Dot(delta, majorDir);
        float y = Vector2.Dot(delta, minorDir);
        float a = ellipse.majorAxisLength;
        float b = ellipse.minorAxisLength;
        float ellipseValue = (x * x) / (a * a) + (y * y) / (b * b);
        float epsilon = thickness / Mathf.Max(a, b);
        return (ellipseValue > (1 - epsilon)) && (ellipseValue < (1 + epsilon));
    }
}


/// <summary>
/// 提供將任意 3D 空間中圓形投影至螢幕並計算橢圓焦點與主次軸資訊的工具。
/// </summary>
public static class EllipseProjectionUtility
{
    public struct EllipseFociOnScreen
    {
        public Vector2 screenCenter;                 // 圓心的螢幕座標
        public Vector2 majorAxisDirection;           // 橢圓主軸方向（螢幕空間）
        public float majorAxisLength;                // 主軸半長度
        public float minorAxisLength;                // 次軸半長度
    }

    /// <summary>
    /// 計算一個在任意方向的 3D 圓形，在螢幕上的投影橢圓的幾何焦點與軸資訊。
    /// </summary>
    /// <param name="cam">攝影機</param>
    /// <param name="circleCenter">圓心位置（世界空間）</param>
    /// <param name="circleNormal">圓面法向量（世界空間）</param>
    /// <param name="radius">圓半徑</param>
    /// <returns>橢圓主軸資訊</returns>
    public static EllipseFociOnScreen ProjectCircleToScreen(Camera cam, Vector3 circleCenter, Vector3 circleNormal, float radius)
    {
        EllipseFociOnScreen result = default;

        if (cam == null)
        {
            Debug.LogWarning("Camera is null.");
            return result;
        }

        Vector3 normal = circleNormal.normalized;

        // 建立圓面上的兩個正交向量（軸）
        Vector3 axis1 = Vector3.Cross(normal, Vector3.up).normalized;
        if (axis1 == Vector3.zero)
            axis1 = Vector3.Cross(normal, Vector3.right).normalized;

        Vector3 axis2 = Vector3.Cross(normal, axis1).normalized;

        // 在圓上取四個方向端點
        Vector3 worldA = circleCenter + axis1 * radius;
        Vector3 worldB = circleCenter - axis1 * radius;
        Vector3 worldC = circleCenter + axis2 * radius;
        Vector3 worldD = circleCenter - axis2 * radius;

        // 投影至螢幕空間
        Vector3 screenA = cam.WorldToScreenPoint(worldA);
        Vector3 screenB = cam.WorldToScreenPoint(worldB);
        Vector3 screenC = cam.WorldToScreenPoint(worldC);
        Vector3 screenD = cam.WorldToScreenPoint(worldD);
        Vector3 screenCenter3 = cam.WorldToScreenPoint(circleCenter);
        float screenDepth = screenCenter3.z;

        if (screenDepth <= 0f)
        {
            Debug.LogWarning("Circle is behind the camera.");
            return result;
        }

        Vector2 screenCenter = new Vector2(screenCenter3.x, screenCenter3.y);

        // 主軸與次軸
        float aLen = (screenA - screenB).magnitude;
        float bLen = (screenC - screenD).magnitude;
        float a = aLen / 2f;
        float b = bLen / 2f;
        Vector2 mainAxisDir = (screenA - screenB).normalized;

        if (b > a)
        {
            (a, b) = (b, a);
            mainAxisDir = (screenC - screenD).normalized;
        }

        // 輸出填值
        result.screenCenter = screenCenter;
        result.majorAxisDirection = mainAxisDir;
        result.majorAxisLength = a;
        result.minorAxisLength = b;

        return result;
    }
}

