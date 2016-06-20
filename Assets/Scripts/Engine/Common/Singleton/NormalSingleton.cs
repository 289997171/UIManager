using UnityEngine;

namespace Engine.Common.Singleton
{
    /// <summary>
    ///     普通类的单例模式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class NormalSingleton<T> where T : NormalSingleton<T>, /*IManager,*/ new()
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
                    instance = new T();

                    // 如果单例类实现了IManager接口，那么自动执行Init()
                    if (instance is IManager)
                    {
                        instance.IsIManager = true;

                        if (!instance.InitSucceed)
                        {
                            instance.InitSucceed = ((IManager)instance).Init();

                            if (instance.InitSucceed)
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
                return instance;
            }
        }
    }
}