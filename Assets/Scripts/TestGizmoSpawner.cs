// =============================================
// 檔案名稱：TestGizmoSpawner.cs
// 1. 測試用腳本，負責在場景中產生並初始化 Gizmo 物件。
// 2. 方便開發與測試 Gizmo 功能。
// =============================================
using UnityEngine;

public class TestGizmoSpawner : MonoBehaviour
{
    public Camera cam;
    public Transform target;
    public GizmoMaterials materials;

    GameObject spawnedObj;
    TransformGizmo gizmo;

    void OnGUI()
    {
        float w = Screen.width * 0.4f;
        float h = Screen.height * 0.12f;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height * 0.1f;
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = (int)(h * 0.4f);
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;

        if (spawnedObj == null)
        {
            if (GUI.Button(new Rect(x, y, w, h), "產生帶 Gizmo 的 Cube", style))
            {
                spawnedObj = new GameObject();
                spawnedObj.transform.position = Vector3.zero;
                spawnedObj.transform.localScale = Vector3.one;
                gizmo = spawnedObj.AddComponent<TransformGizmo>();
                gizmo.Initialize(target, cam, materials);
            }
        }
        else
        {
            GUI.Label(new Rect(x, y, w, h), "已產生 Cube 並加上 TransformGizmo", style);
        }
    }
}