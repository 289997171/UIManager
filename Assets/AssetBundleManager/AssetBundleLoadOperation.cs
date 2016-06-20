using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace AssetBundles
{
    #region 资源异步加载请求基类

    /// <summary>
    /// 资源异步加载请求
    /// </summary>
    public abstract class AssetBundleLoadOperation : IEnumerator
    {
        public object Current
        {
            get { return null; }
        }

        public bool MoveNext()
        {
            return !IsDone();
        }


        public void Reset()
        {
        }

        abstract public bool Update();

        abstract public bool IsDone();

        abstract public float Process();
    }

    #endregion

    #region 场景加载

#if UNITY_EDITOR

    /// <summary>
    /// 模拟场景加载
    /// </summary>
    public class AssetBundleLoadLevelSimulationOperation : AssetBundleLoadOperation
    {
        // 异步请求
        AsyncOperation m_Operation = null;


        public AssetBundleLoadLevelSimulationOperation(string assetBundleName, string levelName, bool isAdditive)
        {
            string[] levelPaths = UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, levelName);

            if (levelPaths.Length == 0)
            {
                ///@TODO: The error needs to differentiate that an asset bundle name doesn't exist
                //        from that there right scene does not exist in the asset bundle...

                Debug.LogError("There is no scene with name \"" + levelName + "\" in " + assetBundleName);
                return;
            }

            if (isAdditive)
                m_Operation = UnityEditor.EditorApplication.LoadLevelAdditiveAsyncInPlayMode(levelPaths[0]);
            else
                m_Operation = UnityEditor.EditorApplication.LoadLevelAsyncInPlayMode(levelPaths[0]);
        }

        public override bool Update()
        {
            return false;
        }

        public override bool IsDone()
        {
            return m_Operation == null || m_Operation.isDone;
        }

        public override float Process()
        {
            if (m_Operation != null)
            {
                return m_Operation.progress;
            }
            return 0f;
        }
    }

#endif

    /// <summary>
    /// 真实远程加载场景
    /// </summary>
    public class AssetBundleLoadLevelOperation : AssetBundleLoadOperation
    {
        // 资源包名
        protected string m_AssetBundleName;
        // 场景名称
        protected string m_LevelName;
        // 以添加模式还是覆盖模式
        protected bool m_IsAdditive;
        // 下载错误信息
        protected string m_DownloadingError;
        // 场景下载异步请求
        protected AsyncOperation m_Request;

        public AssetBundleLoadLevelOperation(string assetbundleName, string levelName, bool isAdditive)
        {
            m_AssetBundleName = assetbundleName;
            m_LevelName = levelName;
            m_IsAdditive = isAdditive;
        }

        public override bool Update()
        {
            // 是否已经请求了
            if (m_Request != null)
                return false;

            // 获得场景资源
            LoadedAssetBundle bundle = AssetBundleManager.GetLoadedAssetBundle(m_AssetBundleName, out m_DownloadingError);

            // 如果场景资源不为空
            if (bundle != null)
            {
                // 添加模式
                if (m_IsAdditive)
                    m_Request = SceneManager.LoadSceneAsync(m_LevelName, LoadSceneMode.Additive); // Application.LoadLevelAdditiveAsync (m_LevelName);
                // 替换模式
                else
                    m_Request = SceneManager.LoadSceneAsync(m_LevelName, LoadSceneMode.Single); // Application.LoadLevelAsync (m_LevelName);

                // 默认为true，表示一旦场景加载完毕即刻切换场景，设置为false后不自动切换场景，在需要切换场景时，设置m_Request.allowSceneActivation = true;
                // m_Request.allowSceneActivation = false;
                // m_Request.progress 为下载进度
                return false;
            }
            else
                return true;
        }

        public override bool IsDone()
        {
            // Return if meeting downloading error.
            // m_DownloadingError might come from the dependency downloading.
            if (m_Request == null && m_DownloadingError != null)
            {
                Debug.LogError(m_DownloadingError);
                return true;
            }

            return m_Request != null && m_Request.isDone;
        }

        public override float Process()
        {
            if (m_Request != null)
            {
                return m_Request.progress;
            }
            return 0f;
        }
    }

    #endregion

    /// <summary>
    /// 普通资源加载基类
    /// </summary>
    public abstract class AssetBundleLoadAssetOperation : AssetBundleLoadOperation
    {
        public abstract T GetAsset<T>() where T : UnityEngine.Object;
    }

    #region 普通资源加载

    /// <summary>
    /// 模式普通资源加载
    /// </summary>
    public class AssetBundleLoadAssetOperationSimulation : AssetBundleLoadAssetOperation
    {
        Object m_SimulatedObject;

        public AssetBundleLoadAssetOperationSimulation(Object simulatedObject)
        {
            m_SimulatedObject = simulatedObject;
        }

        public override T GetAsset<T>()
        {
            return m_SimulatedObject as T;
        }

        public override bool Update()
        {
            return false;
        }

        public override bool IsDone()
        {
            return true;
        }

        public override float Process()
        {
            return 1f;
        }
    }

    /// <summary>
    /// 真实普通资源加载
    /// </summary>
    public class AssetBundleLoadAssetOperationFull : AssetBundleLoadAssetOperation
    {
        // 资源报名
        protected string m_AssetBundleName;
        // 资源名
        protected string m_AssetName;
        // 异常错误信息
        protected string m_DownloadingError;
        // 资源类型
        protected System.Type m_Type;
        // 资源下载异步请求
        protected AssetBundleRequest m_Request = null;

        public AssetBundleLoadAssetOperationFull(string bundleName, string assetName, System.Type type)
        {
            m_AssetBundleName = bundleName;
            m_AssetName = assetName;
            m_Type = type;
        }

        public override T GetAsset<T>()
        {
            if (m_Request != null && m_Request.isDone)
                return m_Request.asset as T;
            else
                return null;
        }

        // Returns true if more Update calls are required.
        public override bool Update()
        {
            if (m_Request != null)
                return false;

            LoadedAssetBundle bundle = AssetBundleManager.GetLoadedAssetBundle(m_AssetBundleName, out m_DownloadingError);
            if (bundle != null)
            {
                ///@TODO: When asset bundle download fails this throws an exception...
                m_Request = bundle.m_AssetBundle.LoadAssetAsync(m_AssetName, m_Type);
                // 默认为true，表示一旦场景加载完毕即刻切换场景，设置为false后不自动切换场景，在需要切换场景时，设置m_Request.allowSceneActivation = true;
                // m_Request.allowSceneActivation = false;
                // m_Request.progress 为下载进度
                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool IsDone()
        {
            // Return if meeting downloading error.
            // m_DownloadingError might come from the dependency downloading.
            if (m_Request == null && m_DownloadingError != null)
            {
                Debug.LogError(m_DownloadingError);
                return true;
            }

            return m_Request != null && m_Request.isDone;
        }

        public override float Process()
        {
            if (m_Request != null)
            {
                return m_Request.progress;
            }
            return 0f;
        }
    }

    #endregion

    #region 资源依赖总览加载

    public class AssetBundleLoadManifestOperation : AssetBundleLoadAssetOperationFull
    {
        public AssetBundleLoadManifestOperation(string bundleName, string assetName, System.Type type)
            : base(bundleName, assetName, type)
        {
        }

        public override bool Update()
        {
            base.Update();

            if (m_Request != null && m_Request.isDone)
            {
                AssetBundleManager.AssetBundleManifestObject = GetAsset<AssetBundleManifest>();
                return false;
            }
            else
                return true;
        }
    }

    #endregion
}