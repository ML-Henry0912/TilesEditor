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

    private GizmoBase xHandle, yHandle, zHandle;
    private GizmoBase xyHandle, xzHandle, yzHandle;
    private GizmoBase xRotateHandle, yRotateHandle, zRotateHandle;

    private Vector3 dragStartPos, objectStartPos;
    private AxisGizmo activeAxis;
    private PlaneGizmo activePlane;
    private RotateGizmo activeRotate;

    private Vector3 rotateStartPoint;
    private Quaternion objectStartRot;
    private Plane rotationPlane;

    private Action action;

    void Start()
    {
        CreateAllHandles();
        action = CheckMouseDown;
    }

    void Update()
    {
        if (target == null || cam == null) return;

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

        xRotateHandle?.gameObject.SetActive(enableRotateX);
        yRotateHandle?.gameObject.SetActive(enableRotateY);
        zRotateHandle?.gameObject.SetActive(enableRotateZ);
    }

    void CheckMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;
            if (!hit.collider.CompareTag(GIZMO_TAG)) return;

            if (hit.transform.TryGetComponent(out AxisGizmo axis))
            {
                activeAxis = axis;
                Vector3 axisDir = transform.TransformDirection(axis.WorldDirection).normalized;
                dragStartPos = GetClosestPointOnAxis(ray, target.position, axisDir);
                objectStartPos = target.position;
                action = OnDragAxis;
                return;
            }

            if (hit.transform.TryGetComponent(out PlaneGizmo plane))
            {
                activePlane = plane;
                Plane dragPlane = plane.GetDragPlane(transform, target.position);
                if (dragPlane.Raycast(ray, out float enter))
                {
                    dragStartPos = ray.GetPoint(enter);
                    objectStartPos = target.position;
                    action = OnDragPlane;
                    return;
                }
            }

            if (hit.transform.TryGetComponent(out RotateGizmo rotate))
            {
                activeRotate = rotate;
                Vector3 axisWorld = rotate.WorldAxis;
                rotationPlane = new Plane(axisWorld, target.position);

                if (rotationPlane.Raycast(ray, out float enter))
                {
                    rotateStartPoint = ray.GetPoint(enter);
                    objectStartRot = target.rotation;
                    action = OnDragRotate;
                }
            }
        }
        else
        {
            xHandle?.ResetColor(); yHandle?.ResetColor(); zHandle?.ResetColor();
            xyHandle?.ResetColor(); xzHandle?.ResetColor(); yzHandle?.ResetColor();
            xRotateHandle?.ResetColor(); yRotateHandle?.ResetColor(); zRotateHandle?.ResetColor();

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag(GIZMO_TAG))
            {
                if (hit.transform.TryGetComponent(out GizmoBase gizmo))
                    gizmo.SetMaterialColor(Color.yellow);
            }
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

    private GizmoBase CreateAxisHandle(string name, Vector3 localPos, Quaternion localRot, Color color, AxisGizmo.Axis axis)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.tag = GIZMO_TAG;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
        DestroyImmediate(go.GetComponent<Collider>());

        go.AddComponent<CapsuleCollider>().isTrigger = true;
        var axisGizmo = go.AddComponent<AxisGizmo>();
        axisGizmo.Initialize(axis, color);
        return axisGizmo;
    }

    private GizmoBase CreatePlaneHandle(string name, Vector3 localPos, Quaternion localRot, Color color, PlaneGizmo.PlaneType type)
    {
        // 正面
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.tag = GIZMO_TAG;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = Vector3.one * 0.25f;
        DestroyImmediate(go.GetComponent<Collider>());
        go.AddComponent<MeshCollider>().convex = true;
        var planeGizmo = go.AddComponent<PlaneGizmo>();
        planeGizmo.Initialize(type, color);

        // 背面
        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Quad);
        back.name = name + "_Back";
        back.tag = GIZMO_TAG;
        back.transform.SetParent(transform);
        back.transform.localPosition = localPos;
        back.transform.localRotation = localRot * Quaternion.Euler(0, 180, 0);
        back.transform.localScale = Vector3.one * 0.25f;
        DestroyImmediate(back.GetComponent<Collider>());
        back.AddComponent<MeshCollider>().convex = true;
        var planeGizmoBack = back.AddComponent<PlaneGizmo>();
        planeGizmoBack.Initialize(type, color);

        return planeGizmo;
    }

    private GizmoBase CreateRotateHandle(string name, Vector3 localPos, Quaternion localRot, Color color, RotateGizmo.Axis axis)
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

        go.AddComponent<MeshCollider>().convex = true;

        var gizmo = go.AddComponent<RotateGizmo>();
        gizmo.Initialize(axis, color);
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
