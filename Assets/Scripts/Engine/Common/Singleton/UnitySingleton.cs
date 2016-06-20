//using UnityEngine;
//
//namespace Engine.Common.Singleton
//{
//    /// <summary>
//    ///     创建切换场景不自动销毁的单例对象
//    ///     Unity组件类的单例模式，对比DDOSingleton
//    ///     1.需要手动在Unity中创建对象
//    ///     2.需要手动在1创建的对象中添加组件（脚本）
//    ///     3.一般在另外一个对象上AddComponent的形式添加
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    public abstract class UnitySingleton<T> : MonoBehaviour where T : Component /*, IManager*/
//    {
//        public bool InitSucceed;
//        public bool IsIManager;
//
//        public static T Instance { get; private set; }
//
//        private void Awake()
//        {
//            Instance = GetComponent<T>();
//            if (Instance == null)
//            {
//                Debug.LogError("instance is null");
//                InitSucceed = false;
//            }
//            else
//            {
//                // 设置切换场景不自动销毁
//                DontDestroyOnLoad(Instance.gameObject);
//
//                // 如果单例类实现了IManager接口，那么自动执行Init()
//                if (Instance is IManager)
//                {
//                    IsIManager = true;
//                    if (!InitSucceed)
//                    {
//                        // 执行初始化操作
//                        InitSucceed = ((IManager)Instance).Init();
//
//                        if (InitSucceed)
//                        {
//                            Debug.Log(Instance.GetType() + " Init Succeed.");
//                        }
//                        else
//                        {
//                            Debug.LogError(Instance.GetType() + " Init Failed!");
//                        }
//                    }
//                }
//            }
//        }
//
//        /// <summary>
//        ///     程序退出后销毁
//        /// </summary>
//        private void OnApplicationQuit()
//        {
//            Instance = null;
//        }
//    }
//}