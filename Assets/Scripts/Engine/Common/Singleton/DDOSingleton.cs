using UnityEngine;

namespace Engine.Common.Singleton
{
    /// <summary>
    ///     创建切换场景不自动销毁的单例对象
    ///     相对于UnitySingleton优点
    ///     1.不需要手动在Unity中创建对象
    ///     2.不需要手动在1创建的对象中添加组件（脚本）
    ///     3.自动创建一个DDOSingleton对象，并且自动添加组件（脚本）
    ///     DDO = DontDestroyOnLoad
    ///     全局创建一个切换场景不自动销毁的DDOSingleton对象
    ///     将一些单例组件添加到DDOSingleton对象中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DDOSingleton<T> : MonoBehaviour where T : DDOSingleton<T> /*, IManager*/
    {
        private static T instance;
        public bool InitSucceed;

        public bool IsIManager;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = GameObject.Find("DDOSingleton");
                    if (go == null)
                    {
                        go = new GameObject("DDOSingleton");

                        // 设置切换场景不自动销毁
                        DontDestroyOnLoad(go);
                    }

                    instance = go.AddComponent<T>();

                    //// 如果单例类实现了IManager接口，那么自动执行Init()
                    //if (instance is IManager)
                    //{
                    //    instance.IsIManager = true;

                    //    if (!instance.InitSucceed)
                    //    {
                    //        _instance.InitSucceed = ((IManager) _instance).Init();

                    //        if (_instance.InitSucceed)
                    //        {
                    //            Debug.Log(_instance.GetType() + " Init Succeed.");
                    //        }
                    //        else
                    //        {
                    //            Debug.LogError(_instance.GetType() + " Init Failed!");
                    //        }
                    //    }
                    //}

                    //instance = _instance;
                }
                return instance;
            }
        }


        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("AddComponent 添加单例组件，但该单例组件已经被初始化了");
#if UNITY_EDITOR
                Application.Quit();
#else
                Destroy(GetComponent<T>());
#endif
                return;
            }

            instance = GetComponent<T>();
            if (instance == null)
            {
                Debug.LogError("instance is null");
                InitSucceed = false;
            }
            else
            {
                // 设置切换场景不自动销毁
                DontDestroyOnLoad(instance.gameObject);

                // 如果单例类实现了IManager接口，那么自动执行Init()
                if (instance is IManager)
                {
                    IsIManager = true;
                    if (!InitSucceed)
                    {
                        // 执行初始化操作
                        InitSucceed = ((IManager) instance).Init();

                        if (InitSucceed)
                        {
                            Debug.Log(instance.GetType() + " Init Succeed.");
                        }
                        else
                        {
                            Debug.LogError(instance.GetType() + " Init Failed!");
                        }
                    }
                }
            }
        }


        /// <summary>
        ///     程序退出后销毁
        /// </summary>
        private void OnApplicationQuit()
        {
            instance = null;
        }
    }
}