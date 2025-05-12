using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace TilesEditor
{
    public class ExportManager : MonoBehaviour
    {
        public ExportManager Initialize()
        {
            return this;
        }

        public void ExportScene(string filePath)
        {
            Debug.Log($"匯出場景到: {filePath}");
        }

        public void ExportToJson(string filePath)
        {
            Debug.Log($"匯出 JSON 到: {filePath}");
        }
    }
}