using System;
using UnityEngine;
using UnityEngine.Pool;  // ← 一定要加這一行

public class NativePoolExample : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    private ObjectPool<GameObject> pool;

    void Awake()
    {
        pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                return Instantiate(prefab);
            },
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            collectionCheck: true,
            defaultCapacity: 10,
            maxSize: 50
        );
    }

    public GameObject Get()
    {
        return pool.Get();
    }

    public void Release(GameObject go)
    {
        pool.Release(go);
    }

    // 範例：按下空格借出，再按下回車回收
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var go = Get();
            go.transform.position = Vector3.zero;
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            // 假設只回收剛才借出的
            Release(GameObject.FindWithTag("Pooled"));
        }
    }
}
