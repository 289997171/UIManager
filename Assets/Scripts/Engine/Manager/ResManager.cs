using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using Engine.Common;
using Engine.Common.Singleton;


namespace Engine.Manager
{
    #region  保存资源信息，资源下载请求，下载完毕后回调等

    /// <summary>
    /// 预制体资源信息
    /// </summary>
    public class AssetInfo
    {
        // 已加载完成的资源对象（非实例化对象缓存）
        private UnityEngine.Object _assetObjectCache;

        /// <summary>
        /// 资源名称（完整路径的prefab，必须在Resources目录下）
        /// </summary>
        private string _assetName;

        /// <summary>
        /// 资源BundleName（如果涉及网络加载的需要设置）
        /// </summary>
        private string _assetBundleName;

        /// <summary>
        /// 资源类型
        /// </summary>
        private System.Type _assetType;

        // <summary>
        /// 是否长期保存在内存中
        /// </summary>
        private bool _isKeepInMemory;

        public AssetInfo(string assetName, string assetBundleName, Type assetType = null, bool isKeepInMemory = false)
        {
            this._assetName = assetName;
            this._assetBundleName = assetBundleName;
            this._assetType = assetType ?? typeof(GameObject);
            this._isKeepInMemory = isKeepInMemory;
        }

        public string AssetName
        {
            get { return _assetName; }
            set { _assetName = value; }
        }

        public string AssetBundleName
        {
            get { return _assetBundleName; }
            set { _assetBundleName = value; }
        }

        public Type AssetType
        {
            get { return _assetType; }
            set { _assetType = value; }
        }

        public bool IsKeepInMemory
        {
            get { return _isKeepInMemory; }
            set { _isKeepInMemory = value; }
        }

        private bool _isLoaded = false;

        /// <summary>
        /// 是否加载完成,加载成功与失败都需要判断
        /// </summary>
        public bool IsLoaded
        {
            set { _isLoaded = value; }
            get
            {
                if (_assetObjectCache != null) return true;
                return _isLoaded;
            }
        }

        public UnityEngine.Object AssetObjectCache
        {
            set { _assetObjectCache = value; }
            get
            {
                return _assetObjectCache;
            }
        }


    }

    #endregion


    public class ResManager : DDOSingleton<ResManager>, IManager
    {


        // 保存尝试载的资源，包含加载成功与加载失败的
        private readonly Dictionary<string, AssetInfo> _cacheAssetObjectInfos = new Dictionary<string, AssetInfo>();

        public Dictionary<string, AssetInfo> CacheAssetObjectInfos
        {
            get { return _cacheAssetObjectInfos; }
        }

        /// <summary>
        /// 网络资源
        /// 获得资源信息，如果没有该资源信息，返回null
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public AssetInfo GetOrAddAssetInfo(string assetName, string assetBundleName = null)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                Debug.LogError("Error: null assetName name.");
                return null;
            }

            string key = assetName;
            if (assetBundleName != null)
            {
                key = key + "@" + assetBundleName;
            }

            // Load Res....
            AssetInfo assetInfo;
            if (!_cacheAssetObjectInfos.TryGetValue(key, out assetInfo))
            {
                assetInfo = new AssetInfo(assetName, assetBundleName);
                _cacheAssetObjectInfos.Add(key, assetInfo);
            }

            return assetInfo;
        }


        #region 资源加载的五种方案

        // TODO 使用AssetInfo对象而不直接使用assetBundleName和assertName的原因是为了统一管理

        /// <summary>
        /// 本地资源加载,也就是项目Resources目录下的
        /// 同步加载
        /// </summary>
        /// <returns></returns>
        public UnityEngine.Object LoadAssetObject(string assetName) // assetName = Prefab/UI/LoginUI
        {
            AssetInfo assetInfo = GetOrAddAssetInfo(assetName);

            if (assetInfo.AssetObjectCache == null)
            {
                assetInfo.AssetObjectCache = Resources.Load(assetInfo.AssetName);
                assetInfo.IsLoaded = true;
                if (assetInfo.AssetObjectCache == null) Debug.Log("Resources Load Failure! _assetName:" + assetInfo.AssetName);
            }
            return assetInfo.AssetObjectCache;
        }

        /// <summary>
        /// 本地资源加载
        /// 异步运行，内部同步加载，获得资源对象，如果资源对象为空，尝试加载，并且通过回调返回加载的资源对象
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="loadedListener"></param>
        /// <returns></returns>
        public IEnumerator LoadAssetObjectCoroutine(string assetName, Action<UnityEngine.Object> loadedListener)
        {
            yield return null;

            AssetInfo assetInfo = GetOrAddAssetInfo(assetName);

            if (assetInfo.AssetObjectCache == null)
            {
                // TODO 即便并发创建N个，这里赋值只会执行一次，区别于LoadAssetObjectCoroutineAsync
                assetInfo.AssetObjectCache = Resources.Load(assetInfo.AssetName);
                assetInfo.IsLoaded = true;
                if (assetInfo.AssetObjectCache == null) Debug.Log("Resources Load Failure! _assetName:" + assetInfo.AssetName);
                loadedListener(assetInfo.AssetObjectCache);
            }
            loadedListener(assetInfo.AssetObjectCache);
        }

        /// <summary>
        /// 网络资源加载
        /// 异步运行，内部同步加载，获得资源对象，如果资源对象为空，尝试加载，并且通过回调返回加载的资源对象
        /// </summary>
        /// <param name="assetName">如： LoginMap</param>
        /// <param name="loadedListener">如： prefab/map/loginmap.unity3d</param>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        public IEnumerator LoadAssetObjectCoroutine(string assetName, string assetBundleName, Action<UnityEngine.Object> loadedListener)
        {
            yield return null;

            AssetInfo assetInfo = GetOrAddAssetInfo(assetName, assetBundleName);

            AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(assetInfo.AssetBundleName, assetInfo.AssetName, typeof(UnityEngine.Object));
            if (request == null)
                yield break;

            yield return StartCoroutine(request);
            assetInfo.AssetObjectCache = request.GetAsset<GameObject>();

            loadedListener(assetInfo.AssetObjectCache);
        }


        /// <summary>
        /// 本地资源加载
        /// 异步运行，内部异步加载，获得资源对象，如果资源对象为空，尝试加载，并且通过回调返回加载的资源对象，允许添加加载进度回调
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="loadedListener"></param>
        /// <param name="progressListener"></param>
        /// <returns></returns>
        public IEnumerator LoadAssetObjectCoroutineAsync(string assetName, Action<UnityEngine.Object> loadedListener = null, Action<float> progressListener = null)
        {
            yield return null;

            AssetInfo assetInfo = GetOrAddAssetInfo(assetName);

            // 资源已加载
            if (assetInfo.AssetObjectCache != null)
            {
                if (progressListener != null)
                {
                    progressListener(1.0f);

                    yield return null;
                }

                if (loadedListener != null) loadedListener(assetInfo.AssetObjectCache);
            }
            // 资源未加载
            else
            {
                // Object null. Not Load Resources
                ResourceRequest _resRequest = Resources.LoadAsync(assetInfo.AssetName);

                // 加载中
                while (_resRequest.progress < 0.9)
                {
                    if (progressListener != null) progressListener(_resRequest.progress);
                    // yield return new WaitForEndOfFrame();
                    yield return null;
                }

                // 加载中完成
                while (!_resRequest.isDone)
                {
                    if (progressListener != null) progressListener(_resRequest.progress);
                    // yield return new WaitForEndOfFrame();
                    yield return null;
                }

                // TODO 当并发初见N个，这里赋值只会执行多次，但之后的就不会重复赋值了，区别于LoadAssetObjectCoroutine
                assetInfo.AssetObjectCache = _resRequest.asset;
                assetInfo.IsLoaded = true;
                //Debug.Log("XXXX2");

                if (loadedListener != null) loadedListener(assetInfo.AssetObjectCache);
                yield return _resRequest;
            }
        }

        /// <summary>
        /// 网络资源加载
        /// 异步运行，内部异步加载，获得资源对象，如果资源对象为空，尝试加载，并且通过回调返回加载的资源对象，允许添加加载进度回调
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="assetName"></param>
        /// <param name="loadedListener"></param>
        /// <param name="progressListener"></param>
        /// <returns></returns>
        public IEnumerator LoadAssetObjectCoroutineAsync(string assetName, string assetBundleName, Action<UnityEngine.Object> loadedListener = null, Action<float> progressListener = null)
        {
            yield return null;

            AssetInfo assetInfo = GetOrAddAssetInfo(assetName, assetBundleName);

            // 资源已加载
            if (assetInfo.AssetObjectCache != null)
            {
                if (progressListener != null)
                {
                    progressListener(1.0f);

                    yield return null;
                }

                if (loadedListener != null) loadedListener(assetInfo.AssetObjectCache);
            }
            // 资源未加载
            else
            {
                AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(assetInfo.AssetBundleName, assetInfo.AssetName, typeof(UnityEngine.Object));
                if (request == null)
                    yield break;

                // 最多尝试100次（10秒）。防止资源不存在不停下载的情况 TODO 可能有BUG
                int maxTry = 0;
                while (request.Process() < 0.9f)
                {
                    if (request.Process() < 0.001f && maxTry++ > 100)
                    {
                        Debug.LogError("网络无法获得资源:" + assetInfo.AssetBundleName + " " + assetInfo.AssetName);
                    }
                    // TODO 进度条处理
                    Debug.Log("加载：" + request.Process() * 100 + "/100");
                    progressListener(request.Process());
                    yield return new WaitForSeconds(0.1f);
                }

                while (!request.IsDone())
                {
                    maxTry++;
                    Debug.Log("加载：" + request.Process() * 100 + "/100");
                    progressListener(request.Process());
                    yield return new WaitForSeconds(0.1f);
                }

                Debug.Log("加载：100/100");
                assetInfo.AssetObjectCache = request.GetAsset<GameObject>();
            }
        }

        #endregion


        #region 资源实例化的五种方式

        /// <summary>
        /// 本地资源加载
        /// 同步加载并实例化,只能用于加载本地资源
        /// </summary>
        /// <returns>The instance.</returns>
        /// <param name="assetName">assetName.</param>
        public UnityEngine.Object InstanceAssetObject(string assetName)
        {
            // 同步加载
            UnityEngine.Object assetObj = LoadAssetObject(assetName);
            if (assetObj == null) return null;

            return Instantiate(assetObj);
        }

        /// <summary>
        /// 本地资源加载
        /// 异步运行，内部同步加载并实例化
        /// 建议：对于小的资源，一次实例化多个实例化情况下使用
        /// </summary>
        /// <param name="assetName">assetName.</param>
        /// <param name="loadedListener">loadedListener.</param>
        public void InstanceAssetObjectCoroutine(string assetName, Action<UnityEngine.Object> loadedListener)
        {
            // 异步加载
            StartCoroutine
            (
                LoadAssetObjectCoroutine(assetName,
                    (assetObj) =>
                    {
                        if (assetObj != null)
                        {
                            loadedListener(UnityEngine.Object.Instantiate(assetObj));
                            // loadedListener(Instantiate(assetObj));
                        }
                        else
                        {
                            loadedListener(null);
                        }
                    }
                )
            );
        }

        /// <summary>
        /// 网络资源加载
        /// 异步运行，内部同步加载并实例化
        /// 建议：对于小的资源，一次实例化多个实例化情况下使用
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="assetName">assetName.</param>
        /// <param name="loadedListener">loadedListener.</param>
        public void InstanceAssetObjectCoroutine(string assetName, string assetBundleName, Action<UnityEngine.Object> loadedListener)
        {
            // 异步加载
            StartCoroutine
            (
                LoadAssetObjectCoroutine(assetName, assetBundleName,
                    (assetObj) =>
                    {
                        if (assetObj != null)
                        {
                            loadedListener(UnityEngine.Object.Instantiate(assetObj));
                            // loadedListener(Instantiate(assetObj));
                        }
                        else
                        {
                            loadedListener(null);
                        }
                    }
                )
            );
        }

        /// <summary>
        /// 本地资源加载
        /// 异步运行，内部同步加载并实例化
        /// 建议：对于大的资源，一次实例化一个实例化情况下使用
        /// 如果for循环，大批量的实例化，不建议使用该方式，建议使用InstanceAssetObject
        /// </summary>
        /// <param name="assetName">assetName.</param>
        /// <param name="loadedListener">loadedListener.</param>
        public void InstanceAssetObjectCoroutineAsync(string assetName, Action<UnityEngine.Object> loadedListener, Action<float> progressListener = null)
        {
            // 异步加载
            StartCoroutine
            (
                LoadAssetObjectCoroutineAsync(assetName,
                    (assetObj) =>
                    {
                        if (assetObj != null)
                        {
                            loadedListener(UnityEngine.Object.Instantiate(assetObj));
                            // loadedListener(Instantiate(assetObj));
                        }
                        else
                        {
                            loadedListener(null);
                        }
                    }
                    , progressListener
                )
            );
        }


        /// <summary>
        /// 网络资源加载
        /// 异步运行，内部同步加载并实例化
        /// 建议：对于大的资源，一次实例化一个实例化情况下使用
        /// 如果for循环，大批量的实例化，不建议使用该方式，建议使用InstanceAssetObject
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="assetName">assetName.</param>
        /// <param name="loadedListener">loadedListener.</param>
        public void InstanceAssetObjectCoroutineAsync(string assetName, string assetBundleName, Action<UnityEngine.Object> loadedListener, Action<float> progressListener = null)
        {
            // 异步加载
            StartCoroutine
            (
                LoadAssetObjectCoroutineAsync(assetName, assetBundleName,
                    (assetObj) =>
                    {
                        if (assetObj != null)
                        {
                            loadedListener(UnityEngine.Object.Instantiate(assetObj));
                            // loadedListener(Instantiate(assetObj));
                        }
                        else
                        {
                            loadedListener(null);
                        }
                    }
                    , progressListener
                )
            );
        }

        #endregion

        #region 加载并切换场景

        public IEnumerator LoadSceneAsync(string sceneName, string sceneAssetBundle, bool isAdditive = false, Action<float> progressListener = null)
        {
            AssetBundleLoadOperation request = AssetBundleManager.LoadLevelAsync(sceneAssetBundle, sceneName, isAdditive);
            if (request == null)
                yield break;


            if (progressListener == null)
            {
                // 方案一
                yield return StartCoroutine(request);
            }
            else
            {
                // 方案二 TODO 可能有BUG
                {
                    int maxTry = 0;
                    while (request.Process() < 0.9f)
                    {
                        if (request.Process() < 0.001f && maxTry++ > 100)
                        {
                            Debug.LogError("网络无法获得资源:" + sceneAssetBundle + " " + sceneName);
                        }
                        // TODO 进度条处理
                        Debug.Log("加载：" + request.Process() * 100 + "/100");
                        yield return new WaitForEndOfFrame();
                    }

                    while (!request.IsDone())
                    {
                        Debug.Log("加载：" + request.Process() * 100 + "/100");
                        yield return new WaitForEndOfFrame();
                    }

                    Debug.Log("加载：100/100");
                }
            }
        }

        #endregion

        #region 释放缓存

        /// <summary>
        /// 从资源库面释放一个资源
        /// </summary>
        /// <param name="assestName">资源名称</param>
        /// <param name="canRemoveKeepInMemory">是否可以去掉常驻内存里面的资源</param>
        public void Remove(string assestName, string assestBundleName, bool canRemoveKeepInMemory = false)
        {
            string key = assestName;
            if (assestBundleName != null)
            {
                key = key + "@" + assestBundleName;
            }
            if (!_cacheAssetObjectInfos.ContainsKey(key)) return;

            // 从常驻内存中删除该资源
            if (_cacheAssetObjectInfos[key].IsKeepInMemory)
            {
                if (canRemoveKeepInMemory)
                {
                    _cacheAssetObjectInfos[key] = null;
                    _cacheAssetObjectInfos.Remove(key);

                    if (assestBundleName != null)
                    {
                        AssetBundleManager.UnloadAssetBundle(assestBundleName);
                    }
                }
            }
            else
            {
                _cacheAssetObjectInfos[key] = null;
                _cacheAssetObjectInfos.Remove(key);
            }

            // 释放未使用的资源
            Resources.UnloadUnusedAssets();
        }


        /// <summary>
        /// 请谨慎操作 强制去掉所有资源缓存
        /// </summary>
        public void RemoveAll()
        {
            foreach (KeyValuePair<string, AssetInfo> pair in _cacheAssetObjectInfos)
            {
                AssetInfo assetInfo = _cacheAssetObjectInfos[pair.Key];
                if (assetInfo.AssetBundleName != null)
                {
                    AssetBundleManager.UnloadAssetBundle(assetInfo.AssetBundleName);
                }
                _cacheAssetObjectInfos[pair.Key] = null;
            }
            _cacheAssetObjectInfos.Clear();

            // 释放未使用的资源
            Resources.UnloadUnusedAssets();
        }

        #endregion


        #region ======队列加载模式======

        #region 控制属性

        /// <summary>
        /// cpu数量
        /// </summary>
        private int _processorCount;

        /// <summary>
        /// 当前正在加载的队列
        /// </summary>
        private List<RequestAsset> _loadingList = new List<RequestAsset>();

        /// <summary>
        /// 等待加载的队列
        /// </summary>
        private Queue<RequestAsset> _waitLoads = new Queue<RequestAsset>();

        /// <summary>
        /// 等待销毁的场景实体
        /// </summary>
        //private Queue<AreanRes> _waitDestroyAreanReses = new Queue<AreanRes>();

        #endregion


        public bool Init()
        {
            _processorCount = SystemInfo.processorCount;
            _processorCount = _processorCount < 1 ? 1 : _processorCount;
            _processorCount = _processorCount > 8 ? 8 : _processorCount;

            StartCoroutine(InitAssetBundleManager());
            return true;
        }

        /// <summary>
        /// 初始化Asse
        /// </summary>
        /// <returns></returns>
        private IEnumerator InitAssetBundleManager()
        {
            InitSucceed = false;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            AssetBundleManager.SetDevelopmentAssetBundleServer();
#else
		    // Use the following code if AssetBundles are embedded in the project for example via StreamingAssets folder etc:
		    AssetBundleManager.SetSourceAssetBundleURL(Application.dataPath + "/");
		    // Or customize the URL based on your deployment or configuration
		    //AssetBundleManager.SetSourceAssetBundleURL("http://www.MyWebsite/MyAssetBundles");
#endif
            var request = AssetBundleManager.Initialize();
            if (request != null)
                yield return StartCoroutine(request);

            InitSucceed = true;
        }

        public void Update()
        {
            // 检测当前正在下载的资源
            if (_loadingList.Count > 0)
            {
                for (int i = _loadingList.Count - 1; i >= 0; i--)
                {
                    // 如果下载完毕
                    if (_loadingList[i].IsLoaded)
                    {
                        // LoadFinish(_loadingList[i]); 不能在这里调用
                        _loadingList.RemoveAt(i);
                    }
                }
            }

            //            // 处理场景资源实例化对象销毁
            //            if (_loadingList.Count == 0)
            //            {
            //                if (_waitDestroyAreanReses.Count > 0)
            //                {
            //                    AreanRes areanRes = _waitDestroyAreanReses.Dequeue();
            //                    if (areanRes.InstGameObject != null)
            //                    {
            //                        UnityEngine.Object.Destroy(areanRes.InstGameObject);
            //                        areanRes.InstGameObject = null;
            //                    }
            //                }
            //            }
            //
            //            // 有等待下载的资源并且正下载的资源个数小于CPU个数
            //            //while (_waitLoads.Count > 0 && _waitLoads.Count < _processorCount - _loadingList.Count)
            //            if (_loadingList.Count == 0 && _waitDestroyAreanReses.Count == 0 && _waitLoads.Count > 0)
            //            {
            //                // 等待下载资源出队列，进入正在下载列表
            //                RequestAsset request = _waitLoads.Dequeue();
            //                _loadingList.Add(request);
            //
            //                if (request.AssetInfo.AssetBundleName != null)
            //                {
            //                    // 请求异步下载
            //                    StartCoroutine(LoadAssetObjectCoroutine(request.AssetInfo.AssetBundleName, request.AssetInfo.AssetName,
            //                        (_obj) =>
            //                        {
            //                            request.LoadedEvent(_obj);
            //                        })
            //                    );
            //                }
            //                else
            //                {
            //                    // 请求异步下载
            //                    StartCoroutine(LoadAssetObjectCoroutine(request.AssetInfo.AssetName,
            //                        (_obj) =>
            //                        {
            //                            request.LoadedEvent(_obj);
            //                        })
            //                    );
            //                }
            //            }

        }


        /// <summary>
        /// 本地资源加载
        /// 从Resources加载一个资源
        /// </summary>
        /// <param name="prefabName">资源名称</param>
        /// <param name="type">资源类型</param>
        /// <param name="loadedAction">加载完成后的回调</param>
        /// <param name="isKeepInMemory">是否长期保存在内存中</param>
        public void LoadAssetQueue(string assetName, string assetBundleName = null, LoadedEvent loadedEvent = null, Type type = null, bool isKeepInMemory = false)
        {
            // 是否已下载
            AssetInfo assetInfo = GetOrAddAssetInfo(assetName, assetBundleName);
            // 下载资源为空
            if (assetInfo.IsLoaded) // TODO 如果正在加载中怎么办？？？
            {
                if (loadedEvent != null) loadedEvent(assetInfo.AssetObjectCache);
                return;
            }

            // 检测当前资源是否已处于下载中
            for (int i = 0; i < _loadingList.Count; ++i)
            {
                RequestAsset request = _loadingList[i];
                if (assetBundleName == null && request.AssetInfo.AssetBundleName == null)
                {
                    if (request.AssetInfo.AssetName.Equals(assetName))
                    {
                        request.LoadedEvent += loadedEvent;
                        return;
                    }
                }
                else if (request.AssetInfo.AssetBundleName != null)
                {
                    string key1 = request.AssetInfo.AssetName + "@" + request.AssetInfo.AssetBundleName;
                    string key2 = assetName + "@" + assetBundleName;
                    if (key1.Equals(key2))
                    {
                        request.LoadedEvent += loadedEvent;
                        return;
                    }
                }

            }

            // 检测当前资源是否处于等待下载中
            foreach (RequestAsset request in _waitLoads)
            {
                if (assetBundleName == null && request.AssetInfo.AssetBundleName == null)
                {
                    if (request.AssetInfo.AssetName.Equals(assetName))
                    {
                        request.LoadedEvent += loadedEvent;
                        return;
                    }
                }
                else if (request.AssetInfo.AssetBundleName != null)
                {
                    string key1 = request.AssetInfo.AssetName + "@" + request.AssetInfo.AssetBundleName;
                    string key2 = assetName + "@" + assetBundleName;
                    if (key1.Equals(key2))
                    {
                        request.LoadedEvent += loadedEvent;
                        return;
                    }
                }
            }

            // 创建新的异步资源下载请求，设置回调监听，并保存到等待下载队列
            {
                RequestAsset request = new RequestAsset();
                request.LoadedEvent += loadedEvent;
                request.AssetInfo = assetInfo;
                request.AssetInfo.IsKeepInMemory = isKeepInMemory;
                _waitLoads.Enqueue(request);
            }
        }

        //        public void DesroryAssetInstQueue(AreanRes areanRes)
        //        { 
        //            //            if (areanRes.InstGameObject == null)
        //            //            {
        //            _waitDestroyAreanReses.Enqueue(areanRes);
        //            //            }
        //        }


        public UpdateProgressBarEvent CheckVersionProgressBarEvent;


    }

    public delegate void LoadedEvent(UnityEngine.Object assertObj);

    // 进度条
    public delegate void UpdateProgressBarEvent(float degree);

    public class RequestAsset
    {
        private LoadedEvent _loadedEvent;

        private AssetInfo _assetInfo;

        public RequestAsset()
        {
            Debug.Log("创建一个加载请求");
        }

        ~RequestAsset()
        {
            Debug.Log("销毁一个加载请求");
        }

        public LoadedEvent LoadedEvent
        {
            get { return _loadedEvent; }
            set { _loadedEvent = value; }
        }

        public AssetInfo AssetInfo
        {
            get { return _assetInfo; }
            set { _assetInfo = value; }
        }

        public bool IsLoaded { get { return _assetInfo.IsLoaded; } }
    }
    #endregion




}
