// =============================================
// 檔案名稱：TransformGizmo.cs
// 功能說明：主控 Gizmo，整合軸、平面、旋轉等子 Gizmo，
//          處理物件的移動、旋轉、縮放與互動邏輯。
// =============================================
using System;
using UnityEngine;

public class TransformGizmo : MonoBehaviour
{
    public const string GIZMO_TAG = "GizmoHandle";

    public Transform target;
    public Camera cam;

    [Header("Translate Axis Enable")]
    public bool enableTranslateX = true;
    public bool enableTranslateY = true;
    public bool enableTranslateZ = true;

    [Header("Rotate Axis Enable")]
    public bool enableRotateX = true;
    public bool enableRotateY = true;
    public bool enableRotateZ = true;

    private AxisGizmo xHandle, yHandle, zHandle;
    private PlaneGizmo xyHandle, xzHandle, yzHandle;
    private RotateGizmo xRotateHandle, yRotateHandle, zRotateHandle;

    private Vector3 dragStartPos, objectStartPos;
    private AxisGizmo activeAxis;
    private PlaneGizmo activePlane;
    private RotateGizmo activeRotate;

    private Vector3 rotateStartPoint;
    private Quaternion objectStartRot;
    private Plane rotationPlane;

    private Action action;

    bool initialized = false;

    public void Initialize(Transform target, Camera cam)
    {
        this.target = target;
        this.cam = cam;
        CreateAllHandles();
        action = CheckMouseDown;
        initialized = true;
    }

    void Update()
    {
        if (!initialized || target == null || cam == null) return;

        transform.position = target.position;
        transform.rotation = target.rotation;

        UpdateHandleVisibility();
        action?.Invoke();
    }

    void UpdateHandleVisibility()
    {
        xHandle?.gameObject.SetActive(enableTranslateX);
        yHandle?.gameObject.SetActive(enableTranslateY);
        zHandle?.gameObject.SetActive(enableTranslateZ);

        // 只有兩軸都啟動才顯示對應的 plane
        xyHandle?.gameObject.SetActive(enableTranslateX && enableTranslateY);
        xzHandle?.gameObject.SetActive(enableTranslateX && enableTranslateZ);
        yzHandle?.gameObject.SetActive(enableTranslateY && enableTranslateZ);

        xRotateHandle?.gameObject.SetActive(enableRotateX);
        yRotateHandle?.gameObject.SetActive(enableRotateY);
        zRotateHandle?.gameObject.SetActive(enableRotateZ);
    }

    void CheckMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 先判斷旋轉 Gizmo ...（原本的程式碼）
            float ringRadius = 1.0f * 1.2f;
            Vector3 center = target.position;
            bool found = false;
            if (enableRotateY && yRotateHandle != null && yRotateHandle.IsMouseOnGizmo(center, transform.up, ringRadius))
            {
                activeRotate = yRotateHandle;
                rotationPlane = new Plane(transform.up, center);
                found = true;
            }
            else if (enableRotateX && xRotateHandle != null && xRotateHandle.IsMouseOnGizmo(center, transform.right, ringRadius))
            {
                activeRotate = xRotateHandle;
                rotationPlane = new Plane(transform.right, center);
                found = true;
            }
            else if (enableRotateZ && zRotateHandle != null && zRotateHandle.IsMouseOnGizmo(center, transform.forward, ringRadius))
            {
                activeRotate = zRotateHandle;
                rotationPlane = new Plane(transform.forward, center);
                found = true;
            }
            if (found)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (rotationPlane.Raycast(ray, out float enter))
                {
                    rotateStartPoint = ray.GetPoint(enter);
                    objectStartRot = target.rotation;
                    action = OnDragRotate;
                }
                return;
            }

            // 軸向 Gizmo 數學判斷（優先於 Collider）
            if (enableTranslateX && xHandle != null && xHandle.IsMouseOnAxisGizmo(target.position, transform.right))
            {
                activeAxis = xHandle;
                Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
                dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
                objectStartPos = target.position;
                action = OnDragAxis;
                return;
            }
            if (enableTranslateY && yHandle != null && yHandle.IsMouseOnAxisGizmo(target.position, transform.up))
            {
                activeAxis = yHandle;
                Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
                dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
                objectStartPos = target.position;
                action = OnDragAxis;
                return;
            }
            if (enableTranslateZ && zHandle != null && zHandle.IsMouseOnAxisGizmo(target.position, transform.forward))
            {
                activeAxis = zHandle;
                Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
                dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
                objectStartPos = target.position;
                action = OnDragAxis;
                return;
            }

            // 平面 Gizmo 數學判斷
            if (enableTranslateX && enableTranslateY && xyHandle != null && xyHandle.IsMouseOnPlaneGizmo(target.position, transform.right, transform.up))
            {
                activePlane = xyHandle;
                Plane dragPlane = activePlane.GetDragPlane(transform, target.position);
                dragStartPos = cam.ScreenPointToRay(Input.mousePosition).GetPoint(0f); // 只要有點到即可
                objectStartPos = target.position;
                action = OnDragPlane;
                return;
            }
            if (enableTranslateX && enableTranslateZ && xzHandle != null && xzHandle.IsMouseOnPlaneGizmo(target.position, transform.right, transform.forward))
            {
                activePlane = xzHandle;
                Plane dragPlane = activePlane.GetDragPlane(transform, target.position);
                dragStartPos = cam.ScreenPointToRay(Input.mousePosition).GetPoint(0f);
                objectStartPos = target.position;
                action = OnDragPlane;
                return;
            }
            if (enableTranslateY && enableTranslateZ && yzHandle != null && yzHandle.IsMouseOnPlaneGizmo(target.position, transform.up, transform.forward))
            {
                activePlane = yzHandle;
                Plane dragPlane = activePlane.GetDragPlane(transform, target.position);
                dragStartPos = cam.ScreenPointToRay(Input.mousePosition).GetPoint(0f);
                objectStartPos = target.position;
                action = OnDragPlane;
                return;
            }
        }
        else
        {
            xHandle?.ResetColor(); yHandle?.ResetColor(); zHandle?.ResetColor();
            xyHandle?.ResetColor(); xzHandle?.ResetColor(); yzHandle?.ResetColor();
            xRotateHandle?.ResetColor(); yRotateHandle?.ResetColor(); zRotateHandle?.ResetColor();

            // Hover 效果：旋轉環優先
            float ringRadius = 1.0f * 1.2f;
            Vector3 center = target.position;
            bool hoverFound = false;
            if (enableRotateY && yRotateHandle != null && yRotateHandle.IsMouseOnGizmo(center, transform.up, ringRadius))
            {
                yRotateHandle.SetMaterialColor(Color.yellow);
                hoverFound = true;
            }
            else if (enableRotateX && xRotateHandle != null && xRotateHandle.IsMouseOnGizmo(center, transform.right, ringRadius))
            {
                xRotateHandle.SetMaterialColor(Color.yellow);
                hoverFound = true;
            }
            else if (enableRotateZ && zRotateHandle != null && zRotateHandle.IsMouseOnGizmo(center, transform.forward, ringRadius))
            {
                zRotateHandle.SetMaterialColor(Color.yellow);
                hoverFound = true;
            }
            // 軸向 hover
            else if (enableTranslateX && xHandle != null && xHandle.IsMouseOnAxisGizmo(target.position, transform.right))
            {
                xHandle.SetMaterialColor(Color.yellow);
                hoverFound = true;
            }
            else if (enableTranslateY && yHandle != null && yHandle.IsMouseOnAxisGizmo(target.position, transform.up))
            {
                yHandle.SetMaterialColor(Color.yellow);
                hoverFound = true;
            }
            else if (enableTranslateZ && zHandle != null && zHandle.IsMouseOnAxisGizmo(target.position, transform.forward))
            {
                zHandle.SetMaterialColor(Color.yellow);
                hoverFound = true;
            }
            // 平面 hover
            if (enableTranslateX && enableTranslateY && xyHandle != null && xyHandle.IsMouseOnPlaneGizmo(target.position, transform.right, transform.up))
            {
                xyHandle.SetMaterialColor(Color.yellow);
                hoverFound = true;
            }
            else if (enableTranslateX && enableTranslateZ && xzHandle != null && xzHandle.IsMouseOnPlaneGizmo(target.position, transform.right, transform.forward))
            {
                xzHandle.SetMaterialColor(Color.yellow);
                hoverFound = true;
            }
            else if (enableTranslateY && enableTranslateZ && yzHandle != null && yzHandle.IsMouseOnPlaneGizmo(target.position, transform.up, transform.forward))
            {
                yzHandle.SetMaterialColor(Color.yellow);
                hoverFound = true;
            }
            if (hoverFound) return;
        }
    }

    void OnDragAxis()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Input.GetMouseButton(0) && activeAxis != null)
        {
            Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
            Vector3 current = GetClosestPointOnAxis(ray, objectStartPos, axisDir);
            Vector3 delta = Vector3.Project(current - dragStartPos, axisDir);
            if (delta.magnitude < 100f)
                target.position = objectStartPos + delta;
        }

        if (Input.GetMouseButtonUp(0)) EndDrag();
    }

    void OnDragPlane()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Input.GetMouseButton(0) && activePlane != null)
        {
            Plane dragPlane = activePlane.GetDragPlane(transform, target.position);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 currentPoint = ray.GetPoint(enter);
                Vector3 delta = currentPoint - dragStartPos;
                if (delta.magnitude < 100f)
                    target.position = objectStartPos + delta;
            }
        }

        if (Input.GetMouseButtonUp(0)) EndDrag();
    }

    void OnDragRotate()
    {
        if (Input.GetMouseButton(0) && activeRotate != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (rotationPlane.Raycast(ray, out float enter))
            {
                Vector3 currentPoint = ray.GetPoint(enter);
                Vector3 startDir = (rotateStartPoint - target.position).normalized;
                Vector3 currentDir = (currentPoint - target.position).normalized;

                Quaternion deltaRotation = Quaternion.FromToRotation(startDir, currentDir);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

                // 保證旋轉方向符合原始軸向
                if (Vector3.Dot(axis, activeRotate.WorldAxis) < 0f)
                    angle = -angle;

                target.rotation = objectStartRot * Quaternion.AngleAxis(angle, activeRotate.WorldAxis);
            }
        }

        if (Input.GetMouseButtonUp(0)) EndDrag();
    }

    void EndDrag()
    {
        activeAxis?.ResetColor();
        activePlane?.ResetColor();
        activeRotate?.ResetColor();

        activeAxis = null;
        activePlane = null;
        activeRotate = null;
        action = CheckMouseDown;
    }

    private Vector3 GetClosestPointOnAxis(Ray ray, Vector3 axisOrigin, Vector3 axisDir)
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

    private void CreateAllHandles()
    {
        xHandle = CreateAxisHandle("X_Handle", new Vector3(0.5f, 0, 0), Quaternion.Euler(0, 0, -90), Color.red, AxisGizmo.Axis.X);
        yHandle = CreateAxisHandle("Y_Handle", new Vector3(0, 0.5f, 0), Quaternion.identity, Color.green, AxisGizmo.Axis.Y);
        zHandle = CreateAxisHandle("Z_Handle", new Vector3(0, 0, 0.5f), Quaternion.Euler(90, 0, 0), Color.blue, AxisGizmo.Axis.Z);

        xyHandle = CreatePlaneHandle("XY_Handle", new Vector3(0.25f, 0.25f, 0), Quaternion.identity, new Color(1, 1, 0, 0.3f), PlaneGizmo.PlaneType.XY);
        xzHandle = CreatePlaneHandle("XZ_Handle", new Vector3(0.25f, 0, 0.25f), Quaternion.Euler(90, 0, 0), new Color(1, 0, 1, 0.3f), PlaneGizmo.PlaneType.XZ);
        yzHandle = CreatePlaneHandle("YZ_Handle", new Vector3(0, 0.25f, 0.25f), Quaternion.Euler(0, -90, 0), new Color(0, 1, 1, 0.3f), PlaneGizmo.PlaneType.YZ);

        xRotateHandle = CreateRotateHandle("X_Rotate", Vector3.zero, Quaternion.Euler(0, 0, 90), Color.red, RotateGizmo.Axis.X);
        yRotateHandle = CreateRotateHandle("Y_Rotate", Vector3.zero, Quaternion.identity, Color.green, RotateGizmo.Axis.Y);
        zRotateHandle = CreateRotateHandle("Z_Rotate", Vector3.zero, Quaternion.Euler(90, 0, 0), Color.blue, RotateGizmo.Axis.Z);
    }

    private AxisGizmo CreateAxisHandle(string name, Vector3 localPos, Quaternion localRot, Color color, AxisGizmo.Axis axis)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.tag = GIZMO_TAG;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
        var axisGizmo = go.AddComponent<AxisGizmo>();
        axisGizmo.Initialize(axis, color, cam, 1.0f, 16f);
        return axisGizmo;
    }

    private PlaneGizmo CreatePlaneHandle(string name, Vector3 localPos, Quaternion localRot, Color color, PlaneGizmo.PlaneType type)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.tag = GIZMO_TAG;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = Vector3.one * 0.25f;
        var planeGizmo = go.AddComponent<PlaneGizmo>();
        planeGizmo.Initialize(type, color, cam, 0.25f);
        // 背面同理...
        return planeGizmo;
    }

    private RotateGizmo CreateRotateHandle(string name, Vector3 localPos, Quaternion localRot, Color color, RotateGizmo.Axis axis)
    {
        GameObject go = new GameObject(name);
        go.tag = GIZMO_TAG;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = Vector3.one * 1.2f;
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.mesh = TorusMeshGenerator.Generate(1.0f, 0.05f, 64, 12);
        var gizmo = go.AddComponent<RotateGizmo>();
        gizmo.Initialize(axis, color, cam, 16f);
        return gizmo;
    }

    public static class TorusMeshGenerator
    {
        public static Mesh Generate(float ringRadius = 1f, float tubeRadius = 0.05f, int segments = 64, int sides = 12)
        {
            Mesh mesh = new Mesh();
            mesh.name = "FullTorus";

            int vertexCount = segments * sides;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[segments * sides * 6];

            float segmentStep = Mathf.PI * 2f / segments;

            for (int seg = 0; seg < segments; seg++)
            {
                float theta = segmentStep * seg;
                Vector3 ringCenter = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta)) * ringRadius;

                for (int side = 0; side < sides; side++)
                {
                    float phi = (float)side / sides * Mathf.PI * 2f;
                    Vector3 normal = new Vector3(
                        Mathf.Cos(phi) * Mathf.Cos(theta),
                        Mathf.Sin(phi),
                        Mathf.Cos(phi) * Mathf.Sin(theta)
                    );

                    int index = seg * sides + side;
                    vertices[index] = ringCenter + normal * tubeRadius;
                    normals[index] = normal;
                    uvs[index] = new Vector2((float)seg / segments, (float)side / sides);
                }
            }

            for (int seg = 0; seg < segments; seg++)
            {
                int nextSeg = (seg + 1) % segments;
                for (int side = 0; side < sides; side++)
                {
                    int nextSide = (side + 1) % sides;

                    int current = seg * sides + side;
                    int next = nextSeg * sides + side;
                    int currentNext = seg * sides + nextSide;
                    int nextNext = nextSeg * sides + nextSide;

                    int triIndex = (seg * sides + side) * 6;
                    triangles[triIndex] = current;
                    triangles[triIndex + 1] = next;
                    triangles[triIndex + 2] = currentNext;

                    triangles[triIndex + 3] = next;
                    triangles[triIndex + 4] = nextNext;
                    triangles[triIndex + 5] = currentNext;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
