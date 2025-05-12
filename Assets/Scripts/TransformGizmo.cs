// =============================================
// 檔案名稱：TransformGizmo.cs
// 1. 本類別必須作為主控 Gizmo，整合軸、平面、旋轉等子 Gizmo。
// 2. 必須負責處理物件的移動、旋轉、縮放與互動邏輯。
// 3. 必須支援安全重複初始化，Initialize 可多次呼叫以覆蓋狀態，不會產生重複資源。
// 4. 旋轉功能目前採用 FromToRotation + Dot 判斷，『有穩定性風險，仍需改進』，若滑鼠拖曳跨越圓環180度或 Gizmo 有鏡像/縮放時，可能出現旋轉跳變或方向反轉。
// 5. 所有 Gizmo 必須共用同一份靜態材質，顏色與透明度（80%）需以 MaterialPropertyBlock 設定，避免記憶體浪費。
// 6. PlaneGizmo 必須自動產生正反兩面，確保雙面皆可顯示顏色與高光，反面僅作顯示不參與互動。
// 7. 所有互動偵測（軸、平面、旋轉）必須以數學計算為主，不依賴 Collider，確保精確度與效能。
// 8. 所有 handle 必須為 TransformGizmo 的子物件，且不可手動移除或更改父子結構，否則可能導致互動異常。
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
    public bool translateX = true;
    public bool translateY = true;
    public bool translateZ = true;

    [Header("Rotate Axis Enable")]
    public bool rotateX = true;
    public bool rotateY = true;
    public bool rotateZ = true;

    AxisGizmo xHandle, yHandle, zHandle;
    PlaneGizmo xyHandle, xzHandle, yzHandle;
    RotateGizmo xRotHandle, yRotHandle, zRotHandle;

    Vector3 dragStartPos, objectStartPos;
    AxisGizmo activeAxis;
    PlaneGizmo activePlane;
    RotateGizmo activeRotate;

    Vector3 rotateStartPoint;
    Quaternion objectStartRot;
    Plane rotationPlane;

    Action action;

    bool initialized = false;

    // === 常數定義 ===
    const float AXIS_HANDLE_OFFSET = 0.75f;
    const float PLANE_HANDLE_OFFSET = 0.55f;
    //const float PLANE_HANDLE_SIZE = 0.5f * 1.5f;
    const float PLANE_HANDLE_SIZE = 0.5f;
    const float ROTATE_HANDLE_SCALE = 1.2f;
    const float AXIS_HANDLE_SCALE = 0.1f;
    const float AXIS_HANDLE_LENGTH = 0.8f;
    const float AXIS_HANDLE_THICKNESS = 16.0f;

    // 1. 定義 Handle 配置結構
    public class GizmoHandleConfig
    {
        public GameObject handleObj;
        public Func<TransformGizmo, bool> visibleCondition;
    }

    List<GizmoHandleConfig> handleConfigs = new List<GizmoHandleConfig>();

    public void Initialize(Transform target, Camera cam)
    {
        this.target = target;
        this.cam = cam;
        if (xHandle == null || yHandle == null || zHandle == null ||
            xyHandle == null || xzHandle == null || yzHandle == null ||
            xRotHandle == null || yRotHandle == null || zRotHandle == null)
        {
            CreateAllHandles();
        }
        RegisterHandles();
        initialized = true;
        action = CheckHover;
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
    void CheckHover()
    {
        if (TryHoverHandle())
        {
            action = CheckMouseDown;
        }
    }

    // 狀態：Hover，檢查是否按下滑鼠進入拖曳
    void CheckMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (TryBeginDrag())
            {
                action = OnDrag;
            }
        }
        else if (!TryHoverHandle())
        {
            action = CheckHover;
        }
    }

    // 狀態：Drag，執行拖曳/旋轉
    void OnDrag()
    {
        if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
            return;
        }

        if (activeAxis != null)
        {
            DragAxis();
        }
        else if (activePlane != null)
        {
            DragPlane();
        }
        else if (activeRotate != null)
        {
            DragRotate();
        }
        else
        {
            action = CheckHover;
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
        if (rotateY && yRotHandle != null && yRotHandle.IsMouseOnGizmo(center, transform.up, ringRadius))
        {
            yRotHandle.SetMaterialColor(Color.yellow);
            hoverFound = true;
        }
        else if (rotateX && xRotHandle != null && xRotHandle.IsMouseOnGizmo(center, transform.right, ringRadius))
        {
            xRotHandle.SetMaterialColor(Color.yellow);
            hoverFound = true;
        }
        else if (rotateZ && zRotHandle != null && zRotHandle.IsMouseOnGizmo(center, transform.forward, ringRadius))
        {
            zRotHandle.SetMaterialColor(Color.yellow);
            hoverFound = true;
        }
        else if (translateX && xHandle != null && xHandle.IsMouseOnAxisGizmo(target.position, transform.right))
        {
            xHandle.SetMaterialColor(Color.yellow);
            hoverFound = true;
        }
        else if (translateY && yHandle != null && yHandle.IsMouseOnAxisGizmo(target.position, transform.up))
        {
            yHandle.SetMaterialColor(Color.yellow);
            hoverFound = true;
        }
        else if (translateZ && zHandle != null && zHandle.IsMouseOnAxisGizmo(target.position, transform.forward))
        {
            zHandle.SetMaterialColor(Color.yellow);
            hoverFound = true;
        }
        else if (translateX && translateY && xyHandle != null && xyHandle.IsMouseOnPlaneGizmo())
        {
            xyHandle.SetMaterialColor(Color.yellow);
            hoverFound = true;
        }
        else if (translateX && translateZ && xzHandle != null && xzHandle.IsMouseOnPlaneGizmo())
        {
            xzHandle.SetMaterialColor(Color.yellow);
            hoverFound = true;
        }
        else if (translateY && translateZ && yzHandle != null && yzHandle.IsMouseOnPlaneGizmo())
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
        if (rotateY && yRotHandle != null && yRotHandle.IsMouseOnGizmo(center, transform.up, ringRadius))
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
        else if (rotateX && xRotHandle != null && xRotHandle.IsMouseOnGizmo(center, transform.right, ringRadius))
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
        else if (rotateZ && zRotHandle != null && zRotHandle.IsMouseOnGizmo(center, transform.forward, ringRadius))
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
        else if (translateX && xHandle != null && xHandle.IsMouseOnAxisGizmo(target.position, transform.right))
        {
            activeAxis = xHandle;
            Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
            dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
            objectStartPos = target.position;
            return true;
        }
        else if (translateY && yHandle != null && yHandle.IsMouseOnAxisGizmo(target.position, transform.up))
        {
            activeAxis = yHandle;
            Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
            dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
            objectStartPos = target.position;
            return true;
        }
        else if (translateZ && zHandle != null && zHandle.IsMouseOnAxisGizmo(target.position, transform.forward))
        {
            activeAxis = zHandle;
            Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
            dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
            objectStartPos = target.position;
            return true;
        }
        else if (translateX && translateY && xyHandle != null && xyHandle.IsMouseOnPlaneGizmo())
        {
            activePlane = xyHandle;
            Plane dragPlane = activePlane.GetDragPlane(transform, target.position);
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!dragPlane.Raycast(ray, out float enter))
            {
                dragPlane = new Plane(-dragPlane.normal, dragPlane.distance);
                if (!dragPlane.Raycast(ray, out enter))
                    return false;
            }
            dragStartPos = ray.GetPoint(enter);
            objectStartPos = target.position;
            return true;
        }
        else if (translateX && translateZ && xzHandle != null && xzHandle.IsMouseOnPlaneGizmo())
        {
            activePlane = xzHandle;
            Plane dragPlane = activePlane.GetDragPlane(transform, target.position);
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!dragPlane.Raycast(ray, out float enter))
            {
                dragPlane = new Plane(-dragPlane.normal, dragPlane.distance);
                if (!dragPlane.Raycast(ray, out enter))
                    return false;
            }
            dragStartPos = ray.GetPoint(enter);
            objectStartPos = target.position;
            return true;
        }
        else if (translateY && translateZ && yzHandle != null && yzHandle.IsMouseOnPlaneGizmo())
        {
            activePlane = yzHandle;
            Plane dragPlane = activePlane.GetDragPlane(transform, target.position);
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (!dragPlane.Raycast(ray, out float enter))
            {
                dragPlane = new Plane(-dragPlane.normal, dragPlane.distance);
                if (!dragPlane.Raycast(ray, out enter))
                    return false;
            }
            dragStartPos = ray.GetPoint(enter);
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
        action = CheckHover;
    }

    Vector3 GetClosestPointOnAxis(Ray ray, Vector3 axisOrigin, Vector3 axisDir)
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

    void CreateAllHandles()
    {
        if (xHandle == null)
            xHandle = CreateAxisHandle("X_Handle", new Vector3(AXIS_HANDLE_OFFSET, 0.0f, 0.0f), Quaternion.Euler(0.0f, 0.0f, -90.0f), Color.red, AxisGizmo.Axis.X);
        if (yHandle == null)
            yHandle = CreateAxisHandle("Y_Handle", new Vector3(0.0f, AXIS_HANDLE_OFFSET, 0.0f), Quaternion.identity, Color.green, AxisGizmo.Axis.Y);
        if (zHandle == null)
            zHandle = CreateAxisHandle("Z_Handle", new Vector3(0.0f, 0.0f, AXIS_HANDLE_OFFSET), Quaternion.Euler(90.0f, 0.0f, 0.0f), Color.blue, AxisGizmo.Axis.Z);

        if (xyHandle == null)
            xyHandle = CreatePlaneHandle("XY_Handle", new Vector3(PLANE_HANDLE_OFFSET, PLANE_HANDLE_OFFSET, 0.0f), Quaternion.identity, new Color(1.0f, 1.0f, 0.0f, 0.3f), PlaneGizmo.PlaneType.XY, PLANE_HANDLE_SIZE);
        if (xzHandle == null)
            xzHandle = CreatePlaneHandle("XZ_Handle", new Vector3(PLANE_HANDLE_OFFSET, 0.0f, PLANE_HANDLE_OFFSET), Quaternion.Euler(90.0f, 0.0f, 0.0f), new Color(1.0f, 0.0f, 1.0f, 0.3f), PlaneGizmo.PlaneType.XZ, PLANE_HANDLE_SIZE);
        if (yzHandle == null)
            yzHandle = CreatePlaneHandle("YZ_Handle", new Vector3(0.0f, PLANE_HANDLE_OFFSET, PLANE_HANDLE_OFFSET), Quaternion.Euler(0.0f, -90.0f, 0.0f), new Color(0.0f, 1.0f, 1.0f, 0.3f), PlaneGizmo.PlaneType.YZ, PLANE_HANDLE_SIZE);

        if (xRotHandle == null)
            xRotHandle = CreateRotateHandle("X_Rotate", Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 90.0f), Color.red, RotateGizmo.Axis.X);
        if (yRotHandle == null)
            yRotHandle = CreateRotateHandle("Y_Rotate", Vector3.zero, Quaternion.identity, Color.green, RotateGizmo.Axis.Y);
        if (zRotHandle == null)
            zRotHandle = CreateRotateHandle("Z_Rotate", Vector3.zero, Quaternion.Euler(90.0f, 0.0f, 0.0f), Color.blue, RotateGizmo.Axis.Z);
    }

    void RegisterHandles()
    {
        handleConfigs.Clear();
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = xHandle?.gameObject,
            visibleCondition = g => g.translateX
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = yHandle?.gameObject,
            visibleCondition = g => g.translateY
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = zHandle?.gameObject,
            visibleCondition = g => g.translateZ
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = xyHandle?.gameObject,
            visibleCondition = g => g.translateX && g.translateY
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = xzHandle?.gameObject,
            visibleCondition = g => g.translateX && g.translateZ
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = yzHandle?.gameObject,
            visibleCondition = g => g.translateY && g.translateZ
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = xRotHandle?.gameObject,
            visibleCondition = g => g.rotateX
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = yRotHandle?.gameObject,
            visibleCondition = g => g.rotateY
        });
        handleConfigs.Add(new GizmoHandleConfig
        {
            handleObj = zRotHandle?.gameObject,
            visibleCondition = g => g.rotateZ
        });
    }

    AxisGizmo CreateAxisHandle(string name, Vector3 localPos, Quaternion localRot, Color color, AxisGizmo.Axis axis)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.tag = GIZMO_TAG;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = new Vector3(AXIS_HANDLE_SCALE, AXIS_HANDLE_LENGTH, AXIS_HANDLE_SCALE);
        var axisGizmo = go.AddComponent<AxisGizmo>();
        axisGizmo.Initialize(axis, color, cam, AXIS_HANDLE_THICKNESS, AXIS_HANDLE_THICKNESS);
        return axisGizmo;
    }

    PlaneGizmo CreatePlaneHandle(string name, Vector3 localPos, Quaternion localRot, Color color, PlaneGizmo.PlaneType type, float size)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.tag = GIZMO_TAG;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = Vector3.one * size;
        var planeGizmo = go.AddComponent<PlaneGizmo>();
        planeGizmo.Initialize(type, color, cam, size);

        // 產生反面，只加 MeshRenderer，不加 PlaneGizmo 與 Collider
        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Quad);
        back.name = name + "_Back";
        back.tag = GIZMO_TAG;
        back.transform.SetParent(go.transform);
        back.transform.localPosition = Vector3.zero;
        back.transform.localRotation = Quaternion.Euler(0, 180, 0);
        back.transform.localScale = Vector3.one;
        // 移除 Collider
        var backCollider = back.GetComponent<Collider>();
        if (backCollider != null) DestroyImmediate(backCollider);
        // 設定共用材質與顏色
        var backRenderer = back.GetComponent<MeshRenderer>();
        if (backRenderer != null)
        {
            backRenderer.sharedMaterial = GizmoBase.sharedMaterial;
            var block = new MaterialPropertyBlock();
            Color backColor = color; backColor.a = 0.8f;
            block.SetColor("_Color", backColor);
            backRenderer.SetPropertyBlock(block);
        }
        return planeGizmo;
    }

    RotateGizmo CreateRotateHandle(string name, Vector3 localPos, Quaternion localRot, Color color, RotateGizmo.Axis axis)
    {
        GameObject go = new GameObject(name);
        go.tag = GIZMO_TAG;
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = Vector3.one * ROTATE_HANDLE_SCALE;
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mf.mesh = TorusMeshGenerator.Generate(1.0f, 0.05f, 64, 12);
        var gizmo = go.AddComponent<RotateGizmo>();
        gizmo.Initialize(axis, color, cam, AXIS_HANDLE_THICKNESS);
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
