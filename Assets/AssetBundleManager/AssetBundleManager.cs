using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

/*
 	In this demo, we demonstrate:
	1.	Automatic asset bundle dependency resolving & loading.
		It shows how to use the manifest assetbundle like how to get the dependencies etc.
	2.	Automatic unloading of asset bundles (When an asset bundle or a dependency thereof is no longer needed, the asset bundle is unloaded)
	3.	Editor simulation. A bool defines if we load asset bundles from the project or are actually using asset bundles(doesn't work with assetbundle variants for now.)
		With this, you can player in editor mode without actually building the assetBundles.
	4.	Optional setup where to download all asset bundles
	5.	Build pipeline build postprocessor, integration so that building a player builds the asset bundles and puts them into the player data (Default implmenetation for loading assetbundles from disk on any platform)
	6.	Use WWW.LoadFromCacheOrDownload and feed 128 bit hash to it when downloading via web
		You can get the hash from the manifest assetbundle.
	7.	AssetBundle variants. A prioritized list of variants that should be used if the asset bundle with that variant exists, first variant in the list is the most preferred etc.
*/

namespace AssetBundles
{
    // 加载assetBundle包含引用计数,可用于自动卸载assetBundles的依赖。
    public class LoadedAssetBundle
    {
        // 资源报
        public AssetBundle m_AssetBundle;

        // 引用次数
        public int m_ReferencedCount;

        public LoadedAssetBundle(AssetBundle assetBundle)
        {
            m_AssetBundle = assetBundle;
            m_ReferencedCount = 1;
        }
    }

    // Class takes care of loading assetBundle and its dependencies automatically, loading variants automatically.
    // 该类负责加载资源及其依赖项，自动加载变体。
    public class AssetBundleManager : MonoBehaviour
    {
        // 日志模式，显示所有/仅显示异常
        public enum LogMode
        {
            All,
            JustErrors
        };

        // 日志类型
        public enum LogType
        {
            Info,
            Warning,
            Error
        };

        static LogMode m_LogMode = LogMode.All;

        // 资源下载地址（根地址）
        static string m_BaseDownloadingURL = "";
        // 如不同分辨率的同名资源
        static string[] m_ActiveVariants = {};

        // 资源依赖总览
        static AssetBundleManifest m_AssetBundleManifest = null;
#if UNITY_EDITOR
        // 模拟
        static int m_SimulateAssetBundleInEditor = -1;
        const string kSimulateAssetBundles = "SimulateAssetBundles";
#endif

        // 已下载的资源
        static Dictionary<string, LoadedAssetBundle> m_LoadedAssetBundles = new Dictionary<string, LoadedAssetBundle>();
        // 下载中的请求
        static Dictionary<string, WWW> m_DownloadingWWWs = new Dictionary<string, WWW>();
        // 失败的情况
        static Dictionary<string, string> m_DownloadingErrors = new Dictionary<string, string>();
        // 队列中的请求
        static List<AssetBundleLoadOperation> m_InProgressOperations = new List<AssetBundleLoadOperation>();
        // 依赖关系
        static Dictionary<string, string[]> m_Dependencies = new Dictionary<string, string[]>();

        /// <summary>
        /// 设置日志模式
        /// </summary>
        public static LogMode logMode
        {
            get { return m_LogMode; }
            set { m_LogMode = value; }
        }

        // The base downloading url which is used to generate the full downloading url with the assetBundle names.
        /// <summary>
        /// 基础url用于生成完整的url assetBundle名称。
        /// </summary>
        public static string BaseDownloadingURL
        {
            get { return m_BaseDownloadingURL; }
            set { m_BaseDownloadingURL = value; }
        }

        // Variants which is used to define the active variants.
        /// <summary>
        /// 变量用于定义活跃的变体
        /// </summary>
        public static string[] ActiveVariants
        {
            get { return m_ActiveVariants; }
            set { m_ActiveVariants = value; }
        }

        // AssetBundleManifest object which can be used to load the dependecies and check suitable assetBundle variants.
        /// <summary>
        /// AssetBundleManifest对象可用于加载依赖清单并检查合适assetBundle变体。
        /// </summary>
        public static AssetBundleManifest AssetBundleManifestObject
        {
            set { m_AssetBundleManifest = value; }
        }

        private static void Log(LogType logType, string text)
        {
            if (logType == LogType.Error)
                Debug.LogError("[AssetBundleManager] " + text);
            else if (m_LogMode == LogMode.All)
                Debug.Log("[AssetBundleManager] " + text);
        }

#if UNITY_EDITOR
        // Flag to indicate if we want to simulate assetBundles in Editor without building them actually.
        /// <summary>
        /// 设置是否在开发模式下，使用模拟模式
        /// </summary>
        public static bool SimulateAssetBundleInEditor
        {
            get
            {
                if (m_SimulateAssetBundleInEditor == -1)
                    m_SimulateAssetBundleInEditor = EditorPrefs.GetBool(kSimulateAssetBundles, true) ? 1 : 0;

                return m_SimulateAssetBundleInEditor != 0;
            }
            set
            {
                int newValue = value ? 1 : 0;
                if (newValue != m_SimulateAssetBundleInEditor)
                {
                    m_SimulateAssetBundleInEditor = newValue;
                    EditorPrefs.SetBool(kSimulateAssetBundles, value);
                }
            }
        }


#endif

        /// <summary>
        /// 获得StreamingAssetsPath
        /// </summary>
        /// <returns></returns>
        private static string GetStreamingAssetsPath()
        {
            if (Application.isEditor)
                return "file://" + System.Environment.CurrentDirectory.Replace("\\", "/");
                    // Use the build output folder directly.
            else if (Application.isWebPlayer)
                return System.IO.Path.GetDirectoryName(Application.absoluteURL).Replace("\\", "/") + "/StreamingAssets";
            else if (Application.isMobilePlatform || Application.isConsolePlatform)
                return Application.streamingAssetsPath;
            else // For standalone player.
                return "file://" + Application.streamingAssetsPath;
        }

        /// <summary>
        /// 设置源资源目录
        /// </summary>
        /// <param name="relativePath"></param>
        public static void SetSourceAssetBundleDirectory(string relativePath)
        {
            BaseDownloadingURL = GetStreamingAssetsPath() + relativePath;
        }

        /// <summary>
        /// 设置源资源URL地址
        /// </summary>
        /// <param name="absolutePath"></param>
        public static void SetSourceAssetBundleURL(string absolutePath)
        {
            BaseDownloadingURL = absolutePath + Utility.GetPlatformName() + "/";
        }

        /// <summary>
        /// 设置开发模式下资源服务器地址，对应“AssetBundleManager/Resources/AssetBundleServerURL”文件配置
        /// </summary>
        public static void SetDevelopmentAssetBundleServer()
        {
#if UNITY_EDITOR
            // If we're in Editor simulation mode, we don't have to setup a download URL
            // 如果在编辑器模式，且处于模式模式，不设置下载url地址
            if (SimulateAssetBundleInEditor)
                return;
#endif

            TextAsset urlFile = Resources.Load("AssetBundleServerURL") as TextAsset;
            string url = (urlFile != null) ? urlFile.text.Trim() : null;
            if (url == null || url.Length == 0)
            {
                Debug.LogError("Development Server URL could not be found.");
                //如果未配置，可以取消以下注释，使用本地url地址
                //AssetBundleManager.SetSourceAssetBundleURL("http://localhost:7888/" + UnityHelper.GetPlatformName() + "/");
            }
            else
            {
                // 设置源资源URL地址
                AssetBundleManager.SetSourceAssetBundleURL(url);
            }
        }

        // Get loaded AssetBundle, only return vaild object when all the dependencies are downloaded successfully.
        /// <summary>
        /// 获取已加载的AssetBundle,只有在依赖项也下载成功后才返回有效对象。
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        static public LoadedAssetBundle GetLoadedAssetBundle(string assetBundleName, out string error)
        {
            if (m_DownloadingErrors.TryGetValue(assetBundleName, out error))
                return null;

            LoadedAssetBundle bundle = null;

            // 从已下载资源中获取
            m_LoadedAssetBundles.TryGetValue(assetBundleName, out bundle);
            if (bundle == null)
                return null;

            // No dependencies are recorded, only the bundle itself is required.
            // 获取资源的依赖项
            string[] dependencies = null;
            if (!m_Dependencies.TryGetValue(assetBundleName, out dependencies))
                return bundle;

            // Make sure all dependencies are loaded
            // 确保所有依赖项已经被下载了
            foreach (var dependency in dependencies)
            {
                // 如果有失败情况
                if (m_DownloadingErrors.TryGetValue(assetBundleName, out error))
                    return bundle;

                // Wait all the dependent assetBundles being loaded.
                // 等待所有依赖项下载成功
                LoadedAssetBundle dependentBundle;
                m_LoadedAssetBundles.TryGetValue(dependency, out dependentBundle);
                if (dependentBundle == null)
                    return null;
            }

            return bundle;
        }

        /// <summary>
        /// 初始化操作，异步加载资源依赖总览
        /// </summary>
        /// <returns></returns>
        static public AssetBundleLoadManifestOperation Initialize()
        {
            return Initialize(Utility.GetPlatformName());
        }


        // Load AssetBundleManifest.
        /// <summary>
        /// 初始化操作，异步加载资源依赖总览
        /// </summary>
        /// <param name="manifestAssetBundleName">平台名</param>
        /// <returns></returns>
        static public AssetBundleLoadManifestOperation Initialize(string manifestAssetBundleName)
        {
#if UNITY_EDITOR
            Log(LogType.Info, "Simulation Mode: " + (SimulateAssetBundleInEditor ? "Enabled" : "Disabled"));
#endif

            // 创建资源管理对象的U3D GameObject 设置为不自动销毁
            var go = new GameObject("AssetBundleManager", typeof(AssetBundleManager));
            DontDestroyOnLoad(go);

#if UNITY_EDITOR
            // If we're in Editor simulation mode, we don't need the manifest assetBundle.
            // 如果是编辑器模式，且处于模式模式，不需要加载资源依赖总览
            if (SimulateAssetBundleInEditor)
                return null;
#endif

            // 下载资源，以及其依赖信息
            LoadAssetBundle(manifestAssetBundleName, true);
            // 创建异步加载请求，并放入准备处理队列
            var operation = new AssetBundleLoadManifestOperation(manifestAssetBundleName, "AssetBundleManifest",
                typeof(AssetBundleManifest));
            m_InProgressOperations.Add(operation);

            return operation;
        }

        // Load AssetBundle and its dependencies.
        /// <summary>
        /// 下载资源，以及其依赖项
        /// </summary>
        /// <param name="assetBundleName">资源包名</param>
        /// <param name="isLoadingAssetBundleManifest">是否加载资源依赖根</param>
        static protected void LoadAssetBundle(string assetBundleName, bool isLoadingAssetBundleManifest = false)
        {
            Log(LogType.Info,
                "Loading Asset Bundle " + (isLoadingAssetBundleManifest ? "Manifest: " : ": ") + assetBundleName);

#if UNITY_EDITOR
            // If we're in Editor simulation mode, we don't have to really load the assetBundle and its dependencies.
            // 编辑模式且处于模式
            if (SimulateAssetBundleInEditor)
                return;
#endif

            // 验证是否已经初始化了
            if (!isLoadingAssetBundleManifest)
            {
                if (m_AssetBundleManifest == null)
                {
                    Debug.LogError("Please initialize AssetBundleManifest by calling AssetBundleManager.Initialize()");
                    return;
                }
            }

            // Check if the assetBundle has already been processed.
            // 检查是否已经处于下载队列里
            bool isAlreadyProcessed = LoadAssetBundleInternal(assetBundleName, isLoadingAssetBundleManifest);

            // Load dependencies.
            // 加载依赖
            if (!isAlreadyProcessed && !isLoadingAssetBundleManifest)
                LoadDependencies(assetBundleName);
        }

        // Remaps the asset bundle name to the best fitting asset bundle variant.
        /// <summary>
        /// 切换Variant，如切换某个资源的分辨率
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        static protected string RemapVariantName(string assetBundleName)
        {
            // 获取所有变体资源包，从manifest获取所有带有variant的资源包
            // 每个AssetBundle都可以设置一个variant，其实就是一个后缀，实际AssetBundle的名字会添加这个后缀。如果有不同分辨率的同名资源，可以使用这个来做区分。
            string[] bundlesWithVariant = m_AssetBundleManifest.GetAllAssetBundlesWithVariant();

            string[] split = assetBundleName.Split('.');

            int bestFit = int.MaxValue;
            int bestFitIndex = -1;
            // Loop all the assetBundles with variant to find the best fit variant assetBundle.
            // 便利所有有variant的资源包，查找最匹配的variant资源
            for (int i = 0; i < bundlesWithVariant.Length; i++)
            {
                string[] curSplit = bundlesWithVariant[i].Split('.');
                if (curSplit[0] != split[0])
                    continue;

                int found = System.Array.IndexOf(m_ActiveVariants, curSplit[1]);

                // If there is no active variant found. We still want to use the first 
                if (found == -1)
                    found = int.MaxValue - 1;

                if (found < bestFit)
                {
                    bestFit = found;
                    bestFitIndex = i;
                }
            }

            if (bestFit == int.MaxValue - 1)
            {
                Debug.LogWarning(
                    "Ambigious asset bundle variant chosen because there was no matching active variant: " +
                    bundlesWithVariant[bestFitIndex]);
            }

            if (bestFitIndex != -1)
            {
                return bundlesWithVariant[bestFitIndex];
            }
            else
            {
                return assetBundleName;
            }
        }

        // Where we actuall call WWW to download the assetBundle.
        /// <summary>
        /// 使用WWW下载资源
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="isLoadingAssetBundleManifest"></param>
        /// <returns></returns>
        static protected bool LoadAssetBundleInternal(string assetBundleName, bool isLoadingAssetBundleManifest)
        {
            // Already loaded.
            // 是否已下载
            LoadedAssetBundle bundle = null;
            m_LoadedAssetBundles.TryGetValue(assetBundleName, out bundle);
            if (bundle != null)
            {
                // 引用次数+1
                bundle.m_ReferencedCount++;
                return true;
            }

            // @TODO: Do we need to consider the referenced count of WWWs?
            // In the demo, we never have duplicate WWWs as we wait LoadAssetAsync()/LoadLevelAsync() to be finished before calling another LoadAssetAsync()/LoadLevelAsync().
            // But in the real case, users can call LoadAssetAsync()/LoadLevelAsync() several times then wait them to be finished which might have duplicate WWWs.
            // TODO 例子中没有出现，同时加载的情况，实际使用中，用户是很可能同时下载多个资源的，支持多线程同时下载
            if (m_DownloadingWWWs.ContainsKey(assetBundleName))
                return true;

            WWW download = null;
            string url = m_BaseDownloadingURL + assetBundleName;

            // For manifest assetbundle, always download it as we don't have hash for it.
            // 如果是主依赖，总是重新加载，而不缓存
            if (isLoadingAssetBundleManifest)
                download = new WWW(url);
            else
            // 从缓存读取或新加载
                download = WWW.LoadFromCacheOrDownload(url, m_AssetBundleManifest.GetAssetBundleHash(assetBundleName), 0);

            // 添加到下载列表
            m_DownloadingWWWs.Add(assetBundleName, download);

            return false;
        }

        // Where we get all the dependencies and load them all.
        // 获得资源的所有依赖项，并且加载所有依赖项
        static protected void LoadDependencies(string assetBundleName)
        {
            if (m_AssetBundleManifest == null)
            {
                Debug.LogError("Please initialize AssetBundleManifest by calling AssetBundleManager.Initialize()");
                return;
            }

            // Get dependecies from the AssetBundleManifest object..
            // 通过资源依赖总览获得资源的依赖项
            string[] dependencies = m_AssetBundleManifest.GetAllDependencies(assetBundleName);
            if (dependencies.Length == 0)
                return;

            // 过滤依赖项，获得依赖项的分支名，如分辨率分支
            for (int i = 0; i < dependencies.Length; i++)
                dependencies[i] = RemapVariantName(dependencies[i]);

            // Record and load all dependencies.
            // 添加到依赖项管理中
            m_Dependencies.Add(assetBundleName, dependencies);
            for (int i = 0; i < dependencies.Length; i++)
                // 下载依赖项
                LoadAssetBundleInternal(dependencies[i], false);
        }

        // Unload assetbundle and its dependencies.
        /// <summary>
        /// 卸载资源以及其依赖项/
        /// </summary>
        /// <param name="assetBundleName"></param>
        static public void UnloadAssetBundle(string assetBundleName)
        {
#if UNITY_EDITOR
            // If we're in Editor simulation mode, we don't have to load the manifest assetBundle.
            if (SimulateAssetBundleInEditor)
                return;
#endif

            //Debug.Log(m_LoadedAssetBundles.Count + " assetbundle(s) in memory before unloading " + assetBundleName);
            //卸载网络资源
            UnloadAssetBundleInternal(assetBundleName);
            //卸载网络资源的依赖资源
            UnloadDependencies(assetBundleName);

            //Debug.Log(m_LoadedAssetBundles.Count + " assetbundle(s) in memory after unloading " + assetBundleName);
        }

        /// <summary>
        /// 卸载网络资源的依赖资源
        /// </summary>
        /// <param name="assetBundleName"></param>
        static protected void UnloadDependencies(string assetBundleName)
        {
            string[] dependencies = null;
            if (!m_Dependencies.TryGetValue(assetBundleName, out dependencies))
                return;

            // Loop dependencies.
            foreach (var dependency in dependencies)
            {
                UnloadAssetBundleInternal(dependency);
            }

            m_Dependencies.Remove(assetBundleName);
        }

        /// <summary>
        /// 卸载网络资源
        /// </summary>
        /// <param name="assetBundleName"></param>
        static protected void UnloadAssetBundleInternal(string assetBundleName)
        {
            string error;
            LoadedAssetBundle bundle = GetLoadedAssetBundle(assetBundleName, out error);
            if (bundle == null)
                return;

            // 引用次数-1
            if (--bundle.m_ReferencedCount == 0)
            {
                bundle.m_AssetBundle.Unload(false);
                m_LoadedAssetBundles.Remove(assetBundleName);

                Log(LogType.Info, assetBundleName + " has been unloaded successfully");
            }
        }

        void Update()
        {
            // Collect all the finished WWWs.
            // 保存已完成的WWW请求
            var keysToRemove = new List<string>();
            foreach (var keyValue in m_DownloadingWWWs)
            {
                WWW download = keyValue.Value;

                // If downloading fails.
                // 如果WWW请求失败
                if (download.error != null)
                {
                    // 添加到下载错误列表
                    m_DownloadingErrors.Add(keyValue.Key,
                        string.Format("Failed downloading bundle {0} from {1}: {2}", keyValue.Key, download.url,
                            download.error));

                    keysToRemove.Add(keyValue.Key);
                    continue;
                }

                // If downloading succeeds.
                // 如果WWW请求成功
                if (download.isDone)
                {
                    // 获得资源
                    AssetBundle bundle = download.assetBundle;
                    // 如果获得资源为空
                    if (bundle == null)
                    {
                        // 添加到下载错误列表
                        m_DownloadingErrors.Add(keyValue.Key,
                            string.Format("{0} is not a valid asset bundle.", keyValue.Key));

                        keysToRemove.Add(keyValue.Key);
                        continue;
                    }

                    //Debug.Log("Downloading " + keyValue.Key + " is done at frame " + Time.frameCount);
                    // 添加到下载成功列表
                    m_LoadedAssetBundles.Add(keyValue.Key, new LoadedAssetBundle(download.assetBundle));

                    keysToRemove.Add(keyValue.Key);
                }
            }

            // Remove the finished WWWs.
            // 清楚所有下载完毕的（成功的失败的都包含）
            foreach (var key in keysToRemove)
            {
                WWW download = m_DownloadingWWWs[key];
                m_DownloadingWWWs.Remove(key);
                download.Dispose();
            }

            // Update all in progress operations
            // 处理等待处理的请求
            for (int i = 0; i < m_InProgressOperations.Count;)
            {
                if (!m_InProgressOperations[i].Update())
                {
                    m_InProgressOperations.RemoveAt(i);
                }
                else
                    i++;
            }
        }

        // Load asset from the given assetBundle.
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetBundleName">资源包名</param>
        /// <param name="assetName">资源名</param>
        /// <param name="type">资源类型</param>
        /// <returns></returns>
        static public AssetBundleLoadAssetOperation LoadAssetAsync(string assetBundleName, string assetName,
            System.Type type)
        {
            Log(LogType.Info, "Loading " + assetName + " from " + assetBundleName + " bundle");

            AssetBundleLoadAssetOperation operation = null;
#if UNITY_EDITOR
            // 是否是编辑模式，且为模式
            if (SimulateAssetBundleInEditor)
            {
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, assetName);
                if (assetPaths.Length == 0)
                {
                    Debug.LogError("There is no asset with name \"" + assetName + "\" in " + assetBundleName);
                    return null;
                }

                // @TODO: Now we only get the main object from the first asset. Should consider type also.
                // 本地获得资源依赖总览
                Object target = AssetDatabase.LoadMainAssetAtPath(assetPaths[0]);
                // 创建模拟请求
                operation = new AssetBundleLoadAssetOperationSimulation(target);
            }
            else
#endif
            {
                // 切换Variant
                assetBundleName = RemapVariantName(assetBundleName);
                // 下载资源，以及其依赖项
                LoadAssetBundle(assetBundleName);
                // 创建全资源请求
                operation = new AssetBundleLoadAssetOperationFull(assetBundleName, assetName, type);

                // 添加到等待处理
                m_InProgressOperations.Add(operation);
            }

            return operation;
        }

        // Load level from the given assetBundle.
        /// <summary>
        /// 异步加载场景资源
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="levelName"></param>
        /// <param name="isAdditive"></param>
        /// <returns></returns>
        static public AssetBundleLoadOperation LoadLevelAsync(string assetBundleName, string levelName, bool isAdditive)
        {
            Log(LogType.Info, "Loading " + levelName + " from " + assetBundleName + " bundle");

            AssetBundleLoadOperation operation = null;
#if UNITY_EDITOR
            if (SimulateAssetBundleInEditor)
            {
                operation = new AssetBundleLoadLevelSimulationOperation(assetBundleName, levelName, isAdditive);
            }
            else
#endif
            {
                assetBundleName = RemapVariantName(assetBundleName);
                LoadAssetBundle(assetBundleName);
                operation = new AssetBundleLoadLevelOperation(assetBundleName, levelName, isAdditive);

                m_InProgressOperations.Add(operation);
            }

            return operation;
        }
    } // End of AssetBundleManager.
}