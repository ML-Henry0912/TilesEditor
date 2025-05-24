using UnityEngine;

public class GLBExportTestStarter : MonoBehaviour
{
    public GameObject rootObject;

    void OnGUI()
    {
        // 設定按鈕大小與位置（畫面中央，寬200高60）
        int width = 200;
        int height = 60;
        int x = (Screen.width - width) / 2;
        int y = (Screen.height - height) / 2;
        if (GUI.Button(new Rect(x, y, width, height), "導出GLB檔案"))
        {
            if (rootObject != null)
            {
                GLBExporter.ExportAndDownload(rootObject);
            }
            else
            {
                Debug.LogWarning("請在Inspector指定rootObject");
            }
        }
    }
} 