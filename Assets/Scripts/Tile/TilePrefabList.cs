using UnityEngine;

[CreateAssetMenu(fileName = "TilePrefabList", menuName = "Tiles/TilePrefabList")]
public class TilePrefabList : ScriptableObject
{
    [Header("所有可用的磁磚Prefab清單")]
    public GameObject[] tilePrefabs;

    public int count => tilePrefabs.Length;


}