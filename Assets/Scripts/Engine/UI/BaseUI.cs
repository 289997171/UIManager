using System.Collections.Generic;
using UnityEngine;

namespace Engine.UI
{
    /// <summary>
    ///     UI视图层
    /// </summary>
    public abstract class BaseUI : MonoBehaviour
    {
        #region 基础操作

        /// <summary>
        /// 显示界面
        /// </summary>
        /// <returns>是否显示成功</returns>
        public abstract bool OnShow();

        /// <summary>
        ///     返回true，需要自定义动画等相关操果，将界面移除到镜头外或将其设置active(false),而不是真正的销毁操作
        ///     返回false，直接销毁对象方式关闭
        /// </summary>
        /// <returns></returns>
        public abstract bool OnHide();

        #endregion


        //protected abstract void OnEnable();
        //{
        //    MessageCenter.Instance.AddListener("xxx", xxx);
        //}

        //protected abstract void OnDisable();
        //{
        //    MessageCenter.Instance.RemoveListener("xxx", xxx);
        //}

        #region Cache gameObject & transfrom

        private Transform _cachedTransform;

        /// <summary>
        ///     Gets the cached transform.
        /// </summary>
        /// <value>The cached transform.</value>
        public Transform CachedTransform
        {
            get
            {
                if (!_cachedTransform)
                {
                    _cachedTransform = transform;
                }
                return _cachedTransform;
            }
        }

        private GameObject _cachedGameObject;

        /// <summary>
        ///     Gets the cached game object.
        /// </summary>
        /// <value>The cached game object.</value>
        public GameObject CachedGameObject
        {
            get
            {
                if (!_cachedGameObject)
                {
                    _cachedGameObject = gameObject;
                }
                return _cachedGameObject;
            }
        }

        #endregion

        private BaseUIModule uiModule;

        /// <summary>
        /// 对应的数据模型
        /// </summary>
        public BaseUIModule UiModule
        {
            set { uiModule = value; }
            get
            {
                if (uiModule == null)
                {
                    if (this is SubUI)
                    {
                        uiModule = ((SubUI)this).ParentUi.UiModule;
                    }
                }
                return uiModule;
            }
        }

        protected Dictionary<int, SubUI> _SubUis = new Dictionary<int, SubUI>();

        #region 对子窗口的操作

        public void AddSubUI(int index, SubUI subUi)
        {
            _SubUis[index] = subUi;
        }

        // BaseUI 实际上也是一个简单的UIManager
        /// <summary>
        ///     已经打开的UI
        /// </summary>
        private readonly Dictionary<int, SubUI> _openedUIs = new Dictionary<int, SubUI>();

        /// <summary>
        ///     等待关闭的UI
        /// </summary>
        private readonly List<int> _waitCloseUIs = new List<int>();

        /// <summary>
        ///     等待打开的UI
        /// </summary>
        private readonly Queue<int> _waitOpenUIs = new Queue<int>();

        /// <summary>
        /// 可以根据子界面的具体内容，是否有主界面功能来响应启用该函数
        /// </summary>
        protected void DoUpdate()
        {
            // 处理等待关闭的UI
            if (_waitCloseUIs.Count > 0)
            {
                var uiType = _waitCloseUIs[0];
                _waitCloseUIs.Remove(uiType);
                _Close(uiType);
                return; // 必须等待所有面板关闭才能进行打开UI操作
            }

            // 从等待打开的UI
            if (_waitOpenUIs.Count > 0)
            {
                int uiType = _waitOpenUIs.Dequeue();
                _OpenUI(uiType);
            }
        }

        #endregion

        #region Close UI By int

        public void CloseUI(int _uiType)
        {
            if (_uiType == -1)
            {
                foreach (int uiType in _openedUIs.Keys)
                {
                    _waitCloseUIs.Add(_uiType);
                }
            }
            else
            {
                _waitCloseUIs.Add(_uiType);
            }
        }

        private void _Close(int _uiType)
        {
            if (_uiType == -1)
            {
                foreach (int uiType in _openedUIs.Keys)
                {
                    _waitCloseUIs.Add(_uiType);
                }
            }

            SubUI subUi = null;
            if (_openedUIs.TryGetValue(_uiType, out subUi))
            {
                _openedUIs.Remove(_uiType);
                if (!subUi.OnHide())
                {
                    subUi.CachedGameObject.SetActive(false);
                }
            }
            else
            {
                _openedUIs.Remove(_uiType);
                UnityEngine.Object.Destroy(subUi.gameObject);
            }
        }


        #region Open UI By int

        /// <summary>
        ///     Opens the U.
        /// </summary>
        /// <param name="uiType">User interface type.</param>
        public void OpenUI(int uiType)
        {
            // 如果要打开的界面处于等待关闭状态，那么直接从等待管理状态中删除
            if (_waitCloseUIs.Contains(uiType))
            {
                _waitCloseUIs.Remove(uiType);
                return;
            }


            SubUI baseUi;

            // 如果尚未打开该UI
            if (!_openedUIs.TryGetValue(uiType, out baseUi))
            {
                // 放入等待打开UI堆栈中
                _waitOpenUIs.Enqueue(uiType);
                // TODO xxxx
            }
            else
            {
                Debug.LogError("已经处于打开状态了：" + uiType);
            }
        }


        /// <summary>
        ///     加载UI资源，实例化，并且挂载对应的BaseUI子类组件
        /// </summary>
        /// <returns></returns>
        private void _OpenUI(int uiType)
        {

            SubUI subUi;
            if (_SubUis.TryGetValue(uiType, out subUi))
            {
                _openedUIs.Add(uiType, subUi);
                if (!subUi.OnShow())
                {
                    subUi.gameObject.SetActive(true);
                }
            }
        }

        #endregion
        #endregion
    }


    /// <summary>
    /// 子界面
    /// </summary>
    public abstract class SubUI : BaseUI
    {
        #region 父节点信息

        /// <summary>
        /// 父UI GameObject
        /// </summary>
        protected GameObject parentObject;

        /// <summary>
        /// 父UI Transform
        /// </summary>
        protected Transform parentTransform;

        /// <summary>
        /// 父UI
        /// </summary>
        protected BaseUI parentUI;

        public void SetParent(BaseUI baseUi, int index)
        {
            parentUI = baseUi;
            parentObject = parentUI.CachedGameObject;
            parentTransform = parentUI.CachedTransform;
            parentUI.AddSubUI(index, this);

            UiModule = parentUI.UiModule;
        }

        public GameObject ParentObject
        {
            get { return parentObject; }
            set { parentObject = value; }
        }

        public Transform ParentTransform
        {
            get { return parentTransform; }
            set { parentTransform = value; }
        }

        public BaseUI ParentUi
        {
            get { return parentUI; }
            set { parentUI = value; }
        }

        #endregion


    }
}