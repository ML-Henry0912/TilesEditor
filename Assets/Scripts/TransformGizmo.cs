// =============================================
// 檔案名稱：TransformGizmo.cs
// 功能說明：主控 Gizmo，整合軸、平面、旋轉等子 Gizmo，
//          處理物件的移動、旋轉、縮放與互動邏輯。
// =============================================
using System;
using UnityEngine;
using System.Collections.Generic;

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
    private RotateGizmo xRotHandle, yRotHandle, zRotHandle;

    private Vector3 dragStartPos, objectStartPos;
    private AxisGizmo activeAxis;
    private PlaneGizmo activePlane;
    private RotateGizmo activeRotate;

    private Vector3 rotateStartPoint;
    private Quaternion objectStartRot;
    private Plane rotationPlane;

    private Action action;

    bool initialized = false;

    // 1. 定義 Handle 配置結構
    public class GizmoHandleConfig
    {
        public GameObject handleObj;
        public Func<TransformGizmo, bool> visibleCondition;
    }

    private List<GizmoHandleConfig> handleConfigs = new List<GizmoHandleConfig>();

    public void Initialize(Transform target, Camera cam)
    {
        this.target = target;
        this.cam = cam;
        CreateAllHandles();
        RegisterHandles();
        initialized = true;
        action = StateIdle;
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
        foreach (var cfg in handleConfigs)
        {
            if (cfg.handleObj != null)
                cfg.handleObj.SetActive(cfg.visibleCondition(this));
        }
    }

    // 狀態：Idle，檢查是否 hover 到 handle
    void StateIdle()
    {
        if (TryHoverHandle())
        {
            action = StateHover;
        }
    }

    // 狀態：Hover，檢查是否按下滑鼠進入拖曳
    void StateHover()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (TryBeginDrag())
            {
                action = StateDrag;
            }
        }
        else if (!TryHoverHandle())
        {
            action = StateIdle;
        }
    }

    // 狀態：Drag，執行拖曳/旋轉
    void StateDrag()
    {
        if (activeAxis != null)
        {
            DragAxis();
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }
        else if (activePlane != null)
        {
            DragPlane();
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }
        else if (activeRotate != null)
        {
            DragRotate();
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }
        else
        {
            action = StateIdle;
        }
    }

    // 嘗試 hover 到 handle，並處理 hover 效果
    bool TryHoverHandle()
    {
        xHandle?.ResetColor(); yHandle?.ResetColor(); zHandle?.ResetColor();
        xyHandle?.ResetColor(); xzHandle?.ResetColor(); yzHandle?.ResetColor();
        xRotHandle?.ResetColor(); yRotHandle?.ResetColor(); zRotHandle?.ResetColor();

        float ringRadius = 1.0f * 1.2f;
        Vector3 center = target.position;
        bool hoverFound = false;
        if (enableRotateY && yRotHandle != null && yRotHandle.IsMouseOnGizmo(center, transform.up, ringRadius))
        {
            yRotHandle.SetMaterialColor(Color.yellow);
            hoverFound = true;
        }
        else if (enableRotateX && xRotHandle != null && xRotHandle.IsMouseOnGizmo(center, transform.right, ringRadius))
        {
            xRotHandle.SetMaterialColor(Color.yellow);
            hoverFound = true;
        }
        else if (enableRotateZ && zRotHandle != null && zRotHandle.IsMouseOnGizmo(center, transform.forward, ringRadius))
        {
            zRotHandle.SetMaterialColor(Color.yellow);
            hoverFound = true;
        }
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
        else if (enableTranslateX && enableTranslateY && xyHandle != null && xyHandle.IsMouseOnPlaneGizmo(target.position, transform.right, transform.up))
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
        return hoverFound;
    }

    // 嘗試開始拖曳，根據 hover 的 handle 決定拖曳類型
    bool TryBeginDrag()
    {
        float ringRadius = 1.0f * 1.2f;
        Vector3 center = target.position;
        if (enableRotateY && yRotHandle != null && yRotHandle.IsMouseOnGizmo(center, transform.up, ringRadius))
        {
            activeRotate = yRotHandle;
            rotationPlane = new Plane(transform.up, center);
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (rotationPlane.Raycast(ray, out float enter))
            {
                rotateStartPoint = ray.GetPoint(enter);
                objectStartRot = target.rotation;
                return true;
            }
        }
        else if (enableRotateX && xRotHandle != null && xRotHandle.IsMouseOnGizmo(center, transform.right, ringRadius))
        {
            activeRotate = xRotHandle;
            rotationPlane = new Plane(transform.right, center);
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (rotationPlane.Raycast(ray, out float enter))
            {
                rotateStartPoint = ray.GetPoint(enter);
                objectStartRot = target.rotation;
                return true;
            }
        }
        else if (enableRotateZ && zRotHandle != null && zRotHandle.IsMouseOnGizmo(center, transform.forward, ringRadius))
        {
            activeRotate = zRotHandle;
            rotationPlane = new Plane(transform.forward, center);
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (rotationPlane.Raycast(ray, out float enter))
            {
                rotateStartPoint = ray.GetPoint(enter);
                objectStartRot = target.rotation;
                return true;
            }
        }
        else if (enableTranslateX && xHandle != null && xHandle.IsMouseOnAxisGizmo(target.position, transform.right))
        {
            activeAxis = xHandle;
            Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
            dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
            objectStartPos = target.position;
            return true;
        }
        else if (enableTranslateY && yHandle != null && yHandle.IsMouseOnAxisGizmo(target.position, transform.up))
        {
            activeAxis = yHandle;
            Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
            dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
            objectStartPos = target.position;
            return true;
        }
        else if (enableTranslateZ && zHandle != null && zHandle.IsMouseOnAxisGizmo(target.position, transform.forward))
        {
            activeAxis = zHandle;
            Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
            dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
            objectStartPos = target.position;
            return true;
        }
        else if (enableTranslateX && enableTranslateY && xyHandle != null && xyHandle.IsMouseOnPlaneGizmo(target.position, transform.right, transform.up))
        {
            activePlane = xyHandle;
            dragStartPos = cam.ScreenPointToRay(Input.mousePosition).GetPoint(0f);
            objectStartPos = target.position;
            return true;
        }
        else if (enableTranslateX && enableTranslateZ && xzHandle != null && xzHandle.IsMouseOnPlaneGizmo(target.position, transform.right, transform.forward))
        {
            activePlane = xzHandle;
            dragStartPos = cam.ScreenPointToRay(Input.mousePosition).GetPoint(0f);
            objectStartPos = target.position;
            return true;
        }
        else if (enableTranslateY && enableTranslateZ && yzHandle != null && yzHandle.IsMouseOnPlaneGizmo(target.position, transform.up, transform.forward))
        {
            activePlane = yzHandle;
            dragStartPos = cam.ScreenPointToRay(Input.mousePosition).GetPoint(0f);
            objectStartPos = target.position;
            return true;
        }
        return false;
    }

    // 拖曳軸
    void DragAxis()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
        Vector3 current = GetClosestPointOnAxis(ray, objectStartPos, axisDir);
        Vector3 delta = Vector3.Project(current - dragStartPos, axisDir);
        if (delta.magnitude < 100f)
            target.position = objectStartPos + delta;
    }

    // 拖曳平面
    void DragPlane()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane dragPlane = activePlane.GetDragPlane(transform, target.position);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 currentPoint = ray.GetPoint(enter);
            Vector3 delta = currentPoint - dragStartPos;
            if (delta.magnitude < 100f)
                target.position = objectStartPos + delta;
        }
    }

    // 拖曳旋轉
    void DragRotate()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (rotationPlane.Raycast(ray, out float enter))
        {
            Vector3 currentPoint = ray.GetPoint(enter);
            Vector3 startDir = (rotateStartPoint - target.position).normalized;
            Vector3 currentDir = (currentPoint - target.position).normalized;

            Quaternion deltaRotation = Quaternion.FromToRotation(startDir, currentDir);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

            if (Vector3.Dot(axis, activeRotate.WorldAxis) < 0f)
                angle = -angle;

            target.rotation = objectStartRot * Quaternion.AngleAxis(angle, activeRotate.WorldAxis);
        }
    }

    void EndDrag()
    {
        activeAxis?.ResetColor();
        activePlane?.ResetColor();
        activeRotate?.ResetColor();

        activeAxis = null;
        activePlane = null;
        activeRotate = null;
        action = StateIdle;
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

        xRotHandle = CreateRotateHandle("X_Rotate", Vector3.zero, Quaternion.Euler(0, 0, 90), Color.red, RotateGizmo.Axis.X);
        yRotHandle = CreateRotateHandle("Y_Rotate", Vector3.zero, Quaternion.identity, Color.green, RotateGizmo.Axis.Y);
        zRotHandle = CreateRotateHandle("Z_Rotate", Vector3.zero, Quaternion.Euler(90, 0, 0), Color.blue, RotateGizmo.Axis.Z);
    }

    private void RegisterHandles()
    {
        handleConfigs.Clear();
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = xHandle?.gameObject,
            visibleCondition = g => g.enableTranslateX
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = yHandle?.gameObject,
            visibleCondition = g => g.enableTranslateY
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = zHandle?.gameObject,
            visibleCondition = g => g.enableTranslateZ
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = xyHandle?.gameObject,
            visibleCondition = g => g.enableTranslateX && g.enableTranslateY
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = xzHandle?.gameObject,
            visibleCondition = g => g.enableTranslateX && g.enableTranslateZ
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = yzHandle?.gameObject,
            visibleCondition = g => g.enableTranslateY && g.enableTranslateZ
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = xRotHandle?.gameObject,
            visibleCondition = g => g.enableRotateX
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = yRotHandle?.gameObject,
            visibleCondition = g => g.enableRotateY
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = zRotHandle?.gameObject,
            visibleCondition = g => g.enableRotateZ
        });
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
