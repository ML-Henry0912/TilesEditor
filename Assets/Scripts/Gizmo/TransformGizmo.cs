// =============================================
// 檔案名稱：TransformGizmo.cs
// 1. 本類別作為主控 Gizmo，整合軸、平面、旋轉等子 Gizmo。
// 2. 負責處理物件的移動與旋轉互動邏輯。
// 3. 支援安全重複初始化，相同參數的初始化會被忽略。
// 4. 旋轉功能採用橢圓投影計算，確保在任意視角下都能精確操作。
// 5. 所有 Gizmo 材質由 ScriptableObject 統一管理，於建立時指定。
// 6. 支援個別軸向的啟用/禁用控制。
// 7. 所有 handle 必須為 TransformGizmo 的子物件，且不可手動移除或更改父子結構。
// =============================================
using System;
using UnityEngine;
using System.Collections.Generic;
using static TilesEditor.PlaneGizmo;

namespace TilesEditor
{
    public class TransformGizmo : MonoBehaviour
    {
        // === 常數定義 ===
        const float AXIS_HANDLE_OFFSET = 1.25f;
        const float PLANE_HANDLE_OFFSET = 0.55f;
        const float PLANE_HANDLE_SIZE = 0.75f;
        const float ROTATE_HANDLE_SCALE = 1.2f;
        const float AXIS_HANDLE_SCALE = 0.1f;
        const float AXIS_HANDLE_LENGTH = 0.6f;
        const float AXIS_HANDLE_THICKNESS = 16.0f;
        const float RING_RADIUS = 1.2f;

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

        private iGizmo[] allGizmos = new iGizmo[9];
        AxisGizmo xHandle { get => (AxisGizmo)allGizmos[0]; set => allGizmos[0] = value; }
        AxisGizmo yHandle { get => (AxisGizmo)allGizmos[1]; set => allGizmos[1] = value; }
        AxisGizmo zHandle { get => (AxisGizmo)allGizmos[2]; set => allGizmos[2] = value; }
        PlaneGizmo xyHandle { get => (PlaneGizmo)allGizmos[3]; set => allGizmos[3] = value; }
        PlaneGizmo xzHandle { get => (PlaneGizmo)allGizmos[4]; set => allGizmos[4] = value; }
        PlaneGizmo yzHandle { get => (PlaneGizmo)allGizmos[5]; set => allGizmos[5] = value; }
        RotateGizmo xRotHandle { get => (RotateGizmo)allGizmos[6]; set => allGizmos[6] = value; }
        RotateGizmo yRotHandle { get => (RotateGizmo)allGizmos[7]; set => allGizmos[7] = value; }
        RotateGizmo zRotHandle { get => (RotateGizmo)allGizmos[8]; set => allGizmos[8] = value; }

        public GizmoMaterials materials;

        Vector3 dragStartPos, objectStartPos;
        [Header("Active Gizmos")]
        [SerializeField] private AxisGizmo activeAxis;
        [SerializeField] private PlaneGizmo activePlane;
        [SerializeField] private RotateGizmo activeRotate;

        Vector3 rotateStartPoint;
        Quaternion objectStartRot;
        Plane rotationPlane;

        [Header("AAAA")]
        public Action action;

        bool initialized = false;

        public void Initialize(Transform target, Camera cam, GizmoMaterials materials)
        {
            // 檢查是否已經初始化過相同的目標
            if (initialized && this.target == target && this.cam == cam && this.materials == materials)
            {
                return;
            }

            this.target = target;
            this.cam = cam;
            this.materials = materials;

            // 先 Destroy 舊的 handle GameObject，避免記憶體 leak
            for (int _i = 0; _i < allGizmos.Length; _i++)
            {
                if (allGizmos[_i] != null)
                {
                    Destroy(allGizmos[_i].gameObject);
                    allGizmos[_i] = null;
                }
            }

            CreateAllHandles();
            initialized = true;
            action = CheckHover;
        }

        void Update()
        {
            if (!initialized || target == null || cam == null) return;

            transform.position = target.position;
            transform.rotation = target.rotation;

            UpdateHandleActive();

            action?.Invoke();
        }

        void UpdateHandleActive()
        {
            for (int _i = 0; _i < allGizmos.Length; _i++)
            {
                iGizmo gizmo = allGizmos[_i];
                gizmo?.gameObject?.SetActive(gizmo?.ShouldBeActive() ?? false);
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

        protected void DragPlane(PlaneType type)
        {
            action = CheckDrag;

            switch (type)
            {
                case PlaneType.XY:
                    activePlane = xyHandle; break;
                case PlaneType.XZ:
                    activePlane = xzHandle; break;
                case PlaneType.YZ:
                    activePlane = yzHandle; break;
                default:
                    activePlane = null; break;
            }

        }

        // 狀態：Hover，檢查是否按下滑鼠進入拖曳
        void CheckMouseDown()
        {
            if (Input.GetMouseButtonDown(0))
            {
                CheckDrag();
            }
            else if (!TryHoverHandle())
            {
                action = CheckHover;
            }
        }

        // 嘗試 hover 到 handle，並處理 hover 效果
        bool TryHoverHandle()
        {
            foreach (var gizmo in allGizmos)
            {
                gizmo.ResetColor();
            }

            Vector3 center = target.position;
            bool hoverFound = false;
            if (rotateY && yRotHandle != null && yRotHandle.IsMouseOnGizmo(center, transform.up, RING_RADIUS))
            {
                yRotHandle.SetMaterialColor(Color.yellow);
                var mr = yRotHandle.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = materials.hoverYellow;
                hoverFound = true;
            }
            else if (rotateX && xRotHandle != null && xRotHandle.IsMouseOnGizmo(center, transform.right, RING_RADIUS))
            {
                xRotHandle.SetMaterialColor(Color.yellow);
                var mr = xRotHandle.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = materials.hoverYellow;
                hoverFound = true;
            }
            else if (rotateZ && zRotHandle != null && zRotHandle.IsMouseOnGizmo(center, transform.forward, RING_RADIUS))
            {
                zRotHandle.SetMaterialColor(Color.yellow);
                var mr = zRotHandle.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = materials.hoverYellow;
                hoverFound = true;
            }
            else if (translateX && xHandle != null && xHandle.IsHovered())
            {
                xHandle.SetMaterialColor(Color.yellow);
                var mr = xHandle.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = materials.hoverYellow;
                hoverFound = true;
            }
            else if (translateY && yHandle != null && yHandle.IsHovered())
            {
                yHandle.SetMaterialColor(Color.yellow);
                var mr = yHandle.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = materials.hoverYellow;
                hoverFound = true;
            }
            else if (translateZ && zHandle != null && zHandle.IsHovered())
            {
                zHandle.SetMaterialColor(Color.yellow);
                var mr = zHandle.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = materials.hoverYellow;
                hoverFound = true;
            }
            else if (translateX && translateY && xyHandle != null && xyHandle.IsMouseOnGizmo())
            {
                xyHandle.SetMaterialColor(Color.yellow);
                var mr = xyHandle.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = materials.hoverYellow;
                hoverFound = true;
            }
            else if (translateX && translateZ && xzHandle != null && xzHandle.IsMouseOnGizmo())
            {
                xzHandle.SetMaterialColor(Color.yellow);
                var mr = xzHandle.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = materials.hoverYellow;
                hoverFound = true;
            }
            else if (translateY && translateZ && yzHandle != null && yzHandle.IsMouseOnGizmo())
            {
                yzHandle.SetMaterialColor(Color.yellow);
                var mr = yzHandle.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = materials.hoverYellow;
                hoverFound = true;
            }
            return hoverFound;
        }

        void CheckDrag()
        {
            Vector3 center = target.position;
            if (rotateY && yRotHandle != null && yRotHandle.IsMouseOnGizmo(center, transform.up, RING_RADIUS))
            {
                activeRotate = yRotHandle;
                action = OnDragRotate;
                rotationPlane = new Plane(transform.up, center);
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (rotationPlane.Raycast(ray, out float enter))
                {
                    rotateStartPoint = ray.GetPoint(enter);
                    objectStartRot = target.rotation;
                    return;
                }
            }
            else if (rotateX && xRotHandle != null && xRotHandle.IsMouseOnGizmo(center, transform.right, RING_RADIUS))
            {
                activeRotate = xRotHandle;
                action = OnDragRotate;
                rotationPlane = new Plane(transform.right, center);
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (rotationPlane.Raycast(ray, out float enter))
                {
                    rotateStartPoint = ray.GetPoint(enter);
                    objectStartRot = target.rotation;
                    return;
                }
            }
            else if (rotateZ && zRotHandle != null && zRotHandle.IsMouseOnGizmo(center, transform.forward, RING_RADIUS))
            {
                activeRotate = zRotHandle;
                action = OnDragRotate;
                rotationPlane = new Plane(transform.forward, center);
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (rotationPlane.Raycast(ray, out float enter))
                {
                    rotateStartPoint = ray.GetPoint(enter);
                    objectStartRot = target.rotation;
                    return;
                }
            }
            else if (translateX && xHandle != null && xHandle.IsHovered())
            {
                activeAxis = xHandle;
                action = OnDragAxis;
                Vector3 axisDir = transform.TransformDirection(xHandle.WorldDirection).normalized;
                dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
                objectStartPos = target.position;
                return;
            }
            else if (translateY && yHandle != null && yHandle.IsHovered())
            {
                activeAxis = yHandle;
                action = OnDragAxis;
                Vector3 axisDir = transform.TransformDirection(yHandle.WorldDirection).normalized;
                dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
                objectStartPos = target.position;
                return;
            }
            else if (translateZ && zHandle != null && zHandle.IsHovered())
            {
                activeAxis = zHandle;
                action = OnDragAxis;
                Vector3 axisDir = transform.TransformDirection(zHandle.WorldDirection).normalized;
                dragStartPos = GetClosestPointOnAxis(cam.ScreenPointToRay(Input.mousePosition), target.position, axisDir);
                objectStartPos = target.position;
                return;
            }
            else if (translateX && translateY && xyHandle != null && xyHandle.IsMouseOnGizmo())
            {
                activePlane = xyHandle;
                action = OnDragPlane;
                Plane dragPlane = xyHandle.GetDragPlane(transform, target.position);
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (!dragPlane.Raycast(ray, out float enter))
                {
                    dragPlane = new Plane(-dragPlane.normal, dragPlane.distance);
                    if (!dragPlane.Raycast(ray, out enter))
                        return;
                }
                dragStartPos = ray.GetPoint(enter);
                objectStartPos = target.position;
                return;
            }
            else if (translateX && translateZ && xzHandle != null && xzHandle.IsMouseOnGizmo())
            {
                activePlane = xzHandle;
                action = OnDragPlane;
                Plane dragPlane = xzHandle.GetDragPlane(transform, target.position);
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (!dragPlane.Raycast(ray, out float enter))
                {
                    dragPlane = new Plane(-dragPlane.normal, dragPlane.distance);
                    if (!dragPlane.Raycast(ray, out enter))
                        return;
                }
                dragStartPos = ray.GetPoint(enter);
                objectStartPos = target.position;
                return;
            }
            else if (translateY && translateZ && yzHandle != null && yzHandle.IsMouseOnGizmo())
            {
                activePlane = yzHandle;
                action = OnDragPlane;
                Plane dragPlane = yzHandle.GetDragPlane(transform, target.position);
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (!dragPlane.Raycast(ray, out float enter))
                {
                    dragPlane = new Plane(-dragPlane.normal, dragPlane.distance);
                    if (!dragPlane.Raycast(ray, out enter))
                        return;
                }
                dragStartPos = ray.GetPoint(enter);
                objectStartPos = target.position;
                return;
            }

        }

        // 拖曳軸
        void OnDragAxis()
        {
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
                return;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Vector3 axisDir = transform.TransformDirection(activeAxis.WorldDirection).normalized;
            Vector3 current = GetClosestPointOnAxis(ray, objectStartPos, axisDir);
            Vector3 delta = Vector3.Project(current - dragStartPos, axisDir);
            if (delta.magnitude < 100f)
                target.position = objectStartPos + delta;
        }

        // 拖曳平面
        void OnDragPlane()
        {
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
                return;
            }

            if (activePlane == null)
                return;

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
        void OnDragRotate()
        {
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
                return;
            }

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
            xHandle = CreateAxisHandle("X_Handle", new Vector3(AXIS_HANDLE_OFFSET, 0.0f, 0.0f), Quaternion.Euler(0.0f, 0.0f, -90.0f), Color.red, AxisGizmo.Axis.X);
            yHandle = CreateAxisHandle("Y_Handle", new Vector3(0.0f, AXIS_HANDLE_OFFSET, 0.0f), Quaternion.identity, Color.green, AxisGizmo.Axis.Y);
            zHandle = CreateAxisHandle("Z_Handle", new Vector3(0.0f, 0.0f, AXIS_HANDLE_OFFSET), Quaternion.Euler(90.0f, 0.0f, 0.0f), Color.blue, AxisGizmo.Axis.Z);

            xyHandle = CreatePlaneHandle("XY_Handle", new Vector3(PLANE_HANDLE_OFFSET, PLANE_HANDLE_OFFSET, 0.0f), Quaternion.identity, new Color(1.0f, 1.0f, 0.0f, 0.3f), PlaneGizmo.PlaneType.XY, PLANE_HANDLE_SIZE);
            xzHandle = CreatePlaneHandle("XZ_Handle", new Vector3(PLANE_HANDLE_OFFSET, 0.0f, PLANE_HANDLE_OFFSET), Quaternion.Euler(90.0f, 0.0f, 0.0f), new Color(1.0f, 0.0f, 1.0f, 0.3f), PlaneGizmo.PlaneType.XZ, PLANE_HANDLE_SIZE);
            yzHandle = CreatePlaneHandle("YZ_Handle", new Vector3(0.0f, PLANE_HANDLE_OFFSET, PLANE_HANDLE_OFFSET), Quaternion.Euler(0.0f, -90.0f, 0.0f), new Color(0.0f, 1.0f, 1.0f, 0.3f), PlaneGizmo.PlaneType.YZ, PLANE_HANDLE_SIZE);

            xRotHandle = CreateRotateHandle("X_Rotate", Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 90.0f), Color.red, RotateGizmo.Axis.X);
            yRotHandle = CreateRotateHandle("Y_Rotate", Vector3.zero, Quaternion.identity, Color.green, RotateGizmo.Axis.Y);
            zRotHandle = CreateRotateHandle("Z_Rotate", Vector3.zero, Quaternion.Euler(90.0f, 0.0f, 0.0f), Color.blue, RotateGizmo.Axis.Z);


        }

        AxisGizmo CreateAxisHandle(string name, Vector3 localPos, Quaternion localRot, Color color, AxisGizmo.Axis axis)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = new Vector3(AXIS_HANDLE_SCALE, AXIS_HANDLE_LENGTH, AXIS_HANDLE_SCALE);

            var axisGizmo = go.AddComponent<AxisGizmo>();
            axisGizmo.Initialize(axis, color, this);
            // 指定材質
            var renderer = go.GetComponent<MeshRenderer>();
            if (axis == AxisGizmo.Axis.X) renderer.sharedMaterial = materials.xRed;
            else if (axis == AxisGizmo.Axis.Y) renderer.sharedMaterial = materials.yGreen;
            else if (axis == AxisGizmo.Axis.Z) renderer.sharedMaterial = materials.zBlue;
            return axisGizmo;
        }

        PlaneGizmo CreatePlaneHandle(string name, Vector3 localPos, Quaternion localRot, Color color, PlaneGizmo.PlaneType type, float size)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;

            // 根據平面類型設定正確的 scale
            go.transform.localScale = new Vector3(size, size, 0.05f); ;

            var planeGizmo = go.AddComponent<PlaneGizmo>();
            planeGizmo.Initialize(type, color, this);
            // 指定材質
            var renderer = go.GetComponent<MeshRenderer>();
            if (type == PlaneGizmo.PlaneType.XY) renderer.sharedMaterial = materials.xyYellow;
            else if (type == PlaneGizmo.PlaneType.XZ) renderer.sharedMaterial = materials.xzMagenta;
            else if (type == PlaneGizmo.PlaneType.YZ) renderer.sharedMaterial = materials.yzCyan;
            return planeGizmo;
        }

        RotateGizmo CreateRotateHandle(string name, Vector3 localPos, Quaternion localRot, Color color, RotateGizmo.Axis axis)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = Vector3.one * ROTATE_HANDLE_SCALE;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.mesh = TorusMeshGenerator.Generate(1.0f, 0.05f, 64, 12);
            var gizmo = go.AddComponent<RotateGizmo>();
            gizmo.Initialize(axis, color, this, AXIS_HANDLE_THICKNESS);
            // 指定材質
            if (axis == RotateGizmo.Axis.X) mr.sharedMaterial = materials.xRed;
            else if (axis == RotateGizmo.Axis.Y) mr.sharedMaterial = materials.yGreen;
            else if (axis == RotateGizmo.Axis.Z) mr.sharedMaterial = materials.zBlue;
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

        protected void SetPlaneGizmoProperties(PlaneGizmo.PlaneType type, Vector3? position = null)
        {
            PlaneGizmo targetGizmo = null;
            switch (type)
            {
                case PlaneGizmo.PlaneType.XY:
                    targetGizmo = xyHandle;
                    break;
                case PlaneGizmo.PlaneType.XZ:
                    targetGizmo = xzHandle;
                    break;
                case PlaneGizmo.PlaneType.YZ:
                    targetGizmo = yzHandle;
                    break;
            }

            if (targetGizmo == null) return;

            if (position.HasValue)
            {
                targetGizmo.transform.localPosition = position.Value;
            }
        }

        protected void SetPlaneGizmoInvisible(PlaneGizmo.PlaneType type)
        {
            switch (type)
            {
                case PlaneGizmo.PlaneType.XY:
                    xyHandle.SetInvisible(true);
                    break;
                case PlaneGizmo.PlaneType.XZ:
                    xzHandle.SetInvisible(true);
                    break;
                case PlaneGizmo.PlaneType.YZ:
                    yzHandle.SetInvisible(true);
                    break;
            }


        }

    }
}
