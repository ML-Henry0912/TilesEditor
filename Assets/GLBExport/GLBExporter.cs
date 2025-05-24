using UnityEngine;
using UniGLTF;
using System.Runtime.InteropServices;

public static class GLBExporter
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DownloadFile(string fileName, byte[] data, int length);
#endif

    /// <summary>
    /// 將指定物件（含子物件）導出GLB並下載，支援自訂檔名
    /// </summary>
    /// <param name="root">要導出的GameObject</param>
    /// <param name="fileName">下載檔名，預設為export.glb</param>
    public static void ExportAndDownload(GameObject root, string fileName = "export.glb")
    {
        var exportData = ExportAsBuiltInRP(root, new GltfExportSettings());
        var bytes = exportData.ToGlbBytes();

#if UNITY_WEBGL && !UNITY_EDITOR
        DownloadFile(fileName, bytes, bytes.Length);
#else
        // Editor或Standalone下直接存檔
        System.IO.File.WriteAllBytes(fileName, bytes);
        Debug.Log($"GLB已匯出到專案根目錄: {fileName}");
#endif
    }

    // 內部導出方法，仿照TestGltf.ExportAsBuiltInRP
    private static ExportingGltfData ExportAsBuiltInRP(GameObject gameObject, GltfExportSettings exportSettings = null)
    {
        var data = new ExportingGltfData();
        using (var exporter = new gltfExporter(
            data,
            exportSettings ?? new GltfExportSettings(),
            null, // progress: 可傳null
            null, // animationExporter: 可傳null（WebGL下不支援EditorAnimationExporter）
            new BuiltInGltfMaterialExporter(),
            null // textureSerializer: 可傳null
        ))
        {
            exporter.Prepare(gameObject);
            exporter.Export();
        }
        return data;
    }
} 