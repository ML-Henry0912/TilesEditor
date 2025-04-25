using System.Collections.Generic;
using UnityEngine;

namespace HTools
{
    /// <summary>
    /// 具體要被池化的物件，繼承自 PoolObjectBase
    /// </summary>
    public class PoolObject : PoolObjectBase
    {
        // 如果需要清理或重設狀態，可 override 這個方法
        public override void OnReturnToPool()
        {
            // ex: 停止特效、重設參數等等
        }
    }

    /// <summary>
    /// 基底池化物件，持有對 ObjectPool 的引用，並提供回池方法
    /// </summary>
    public class PoolObjectBase : MonoBehaviour
    {
        private ObjectPool thePool;

        /// <summary>
        /// 初始化時由 ObjectPool 呼叫，注入自身 Pool 實例
        /// </summary>
        public virtual void Init(ObjectPool pool)
        {
            thePool = pool;
        }

        /// <summary>
        /// 當物件回到池中時可做的清理，子類別可 override
        /// </summary>
        public virtual void OnReturnToPool()
        {
            // 預設什麼都不做
        }

        /// <summary>
        /// 將此物件回收至池中
        /// </summary>
        public void ReturnToPool()
        {
            OnReturnToPool();
            gameObject.SetActive(false);
            thePool.ReturnObject(this as PoolObject);
        }
    }

    /// <summary>
    /// 物件池管理器，只處理 PoolObject 類型
    /// </summary>
    public class ObjectPool
    {
        private PoolObject prefab;
        private List<PoolObject> pool = new List<PoolObject>();

        /// <summary>
        /// 建構：傳入要池化的 prefab，與（可選）初始容量
        /// </summary>
        /// <param name="prefab">要複製的 PoolObject 預置</param>
        /// <param name="initialCount">預先建立的實例數量，預設 10</param>
        public ObjectPool(PoolObject prefab, int initialCount = 10)
        {
            this.prefab = prefab;
            for (int i = 0; i < initialCount; i++)
                CreateNewInstance(false);
        }

        /// <summary>
        /// 借出一個物件，若無現成可用，則自動擴充
        /// </summary>
        public PoolObject GetObject()
        {
            foreach (var obj in pool)
            {
                if (!obj.gameObject.activeSelf)
                {
                    obj.gameObject.SetActive(true);
                    return obj;
                }
            }
            // 沒有可用實例，新增一個再回傳
            return CreateNewInstance(true);
        }

        /// <summary>
        /// 將物件加入池中（內部呼叫）
        /// </summary>
        private PoolObject CreateNewInstance(bool active)
        {
            var instance = GameObject.Instantiate(prefab);
            instance.gameObject.SetActive(active);
            instance.Init(this);
            pool.Add(instance);
            return instance;
        }

        /// <summary>
        /// 回收物件到池中，會自動將 GameObject 設為 Inactive
        /// </summary>
        public void ReturnObject(PoolObject obj)
        {
            if (!pool.Contains(obj))
                pool.Add(obj);
            obj.gameObject.SetActive(false);
        }

        /// <summary>
        /// （可選）銷毀所有物件並清空池
        /// </summary>
        public void ClearPool()
        {
            foreach (var obj in pool)
                if (obj != null)
                    GameObject.Destroy(obj.gameObject);
            pool.Clear();
        }
    }
}
