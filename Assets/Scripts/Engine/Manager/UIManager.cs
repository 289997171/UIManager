using System;
using System.Collections;
using System.Collections.Generic;
using Engine.Common;
using Engine.Common.Singleton;
using Engine.UI;
using UI.Login;
using UnityEngine;

namespace Engine.Manager
{
    public class UIManager : DDOSingleton<UIManager>, IManager
    {
        /// <summary>
        ///     已经打开的UI
        /// </summary>
        private readonly Dictionary<EnumUIType, GameObject> _openedUIs = new Dictionary<EnumUIType, GameObject>();

        /// <summary>
        ///     已经打开的UI,但出于”伪关闭状态“，也就是active(false)或处于镜头外
        /// </summary>
        private readonly Dictionary<EnumUIType, GameObject> _hideUIs = new Dictionary<EnumUIType, GameObject>();

        /// <summary>
        ///     等待关闭的UI
        /// </summary>
        private readonly List<EnumUIType> _waitCloseUIs = new List<EnumUIType>();

        /// <summary>
        ///     等待打开的UI
        /// </summary>
        private readonly Queue<EnumUIType> _waitOpenUIs = new Queue<EnumUIType>();

        private bool _stopUpdate = false;

        private void Update()
        {
            if (_stopUpdate) return;

            // 处理等待关闭的UI
            if (_waitCloseUIs.Count > 0)
            {
                var uiType = _waitCloseUIs[0];
                _waitCloseUIs.Remove(uiType);
                _CloseUI(uiType);
                return; // 必须等待所有面板关闭才能进行打开UI操作
            }

            // 从等待打开的UI
            if (_waitOpenUIs.Count > 0)
            {
                EnumUIType uiType = _waitOpenUIs.Dequeue();
                StartCoroutine(_OpenUIAsync(uiType));
            }
        }



        #region 初始化操作

        private readonly Dictionary<EnumUIType, UIInfoData> _dicUiInfoDatas = new Dictionary<EnumUIType, UIInfoData>();

        public bool Init()
        {
            // TODO 添加
            _dicUiInfoDatas.Add(EnumUIType.TestOne,
                new UIInfoData(EnumUIType.TestOne, "Prefab/UI/TestOne", null, typeof(LoginUI), typeof(LoginUIModule),
                    new[] { EnumUIType.TestTwo }));

            return _dicUiInfoDatas.Count == (int)EnumUIType.MAXTYPE;
        }

        public Dictionary<EnumUIType, UIInfoData> DicUiInfoDatas
        {
            get { return _dicUiInfoDatas; }
        }

        #endregion

        #region Get UI & UIObject By EnunUIType 

        /// <summary>
        ///     获得对应已打开的UI，如果没有打开，将抛出异常
        /// </summary>
        /// <returns>The U.</returns>
        /// <param name="_uiType">_ui type.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public T GetOpenedUI<T>(EnumUIType _uiType) where T : BaseUI
        {
            var retObj = GetOpendUIObject(_uiType);
            if (retObj != null)
            {
                return retObj.GetComponent<T>();
            }
            return null;
        }

        /// <summary>
        ///     获得对应已打开的UI，如果没有打开，将抛出异常
        /// </summary>
        /// <returns>The user interface object.</returns>
        /// <param name="_uiType">_ui type.</param>
        public GameObject GetOpendUIObject(EnumUIType _uiType)
        {
            GameObject retObj = null;
            if (!_openedUIs.TryGetValue(_uiType, out retObj))
                throw new Exception("_openedUIs TryGetValue Failure! uiType :" + _uiType);
            return retObj;
        }

        #endregion

        #region Close UI By EnumUIType

        public void CloseUI(EnumUIType _uiType)
        {
            _waitCloseUIs.Add(_uiType);
        }

        private void _CloseUI(EnumUIType _uiType)
        {
            if (_uiType == EnumUIType.MAXTYPE)
            {
                foreach (EnumUIType uiType in _openedUIs.Keys)
                {
                    CloseUI(uiType);
                }
                return;
            }

            GameObject uiObj = null;
            if (!_openedUIs.TryGetValue(_uiType, out uiObj))
            {
                Debug.Log("_openedUIs TryGetValue Failure! uiType :" + _uiType);
                return;
            }


            BaseUI _baseUI = uiObj.GetComponent<BaseUI>();
            if (_baseUI != null)
            {
                _openedUIs.Remove(_uiType);

                if (!_baseUI.OnHide()) // 判断界面关闭是通过隐藏，还是直接销毁
                {
                    UnityEngine.Object.Destroy(uiObj);
                }
                else // 添加到隐藏界面中
                {
                    _hideUIs.Add(_uiType, uiObj);
                }
            }
            else
            {
                _openedUIs.Remove(_uiType);
                UnityEngine.Object.Destroy(uiObj);
            }
        }

        #endregion


        #region Open UI By EnumUIType

        /// <summary>
        ///     Opens the U.
        /// </summary>
        /// <param name="uiType">User interface type.</param>
        public void OpenUI(EnumUIType uiType)
        {
            // 如果要打开的界面处于等待关闭状态，那么直接从等待管理状态中删除
            if (_waitCloseUIs.Contains(uiType))
            {
                _waitCloseUIs.Remove(uiType);
                return;
            }

            UIInfoData uiInfoData = _dicUiInfoDatas[uiType];


            GameObject uiObject;
            // 界面没有真正销毁，只是出于隐藏状态
            if (_hideUIs.TryGetValue(uiType, out uiObject))
            {
                uiInfoData.IsHide = true;

                // 需要关闭那些界面
                var _closeUiTypes = uiInfoData.CloseUiTypes;
                if (_closeUiTypes != null)
                {
                    _waitCloseUIs.AddRange(_closeUiTypes);
                }

                _waitOpenUIs.Enqueue(uiType);
                return;
            }

            // 如果尚未打开该UI
            if (!_openedUIs.TryGetValue(uiType, out uiObject))
            {
                // 需要关闭那些界面
                var _closeUiTypes = uiInfoData.CloseUiTypes;
                if (_closeUiTypes != null)
                {
                    _waitCloseUIs.AddRange(_closeUiTypes);
                }

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
        private IEnumerator _OpenUIAsync(EnumUIType uiType)
        {
            yield return null;

            // TODO 初始化模型层
            BaseUIModule baseUiModule = UIModuleManager.Instance.Register(uiType);
            if (baseUiModule != null)
            {
                // 数据模型初始化加载
                baseUiModule.OnRegister();
                yield return null;
            }

            UIInfoData uiInfoData = _dicUiInfoDatas[uiType];

            BaseUI baseUi = null;
            GameObject uiObject = null;

#if UNITY_EDITOR
            // TODO 处理开发阶段，可能不使用预制体的情况
            baseUi = UnityEngine.Object.FindObjectOfType(uiInfoData.UiViewType) as BaseUI;
            if (baseUi != null)
            {
                Debug.LogError("开发者模式下找到了对应的UI");
                uiObject = baseUi.gameObject;
            }
            if (baseUi == null)
            {
#endif
                if (uiInfoData.IsHide) // 如果从隐藏状态改变
                {
                    uiInfoData.IsHide = false;
                    if (_hideUIs.TryGetValue(uiType, out uiObject))
                    {
                        _hideUIs.Remove(uiType);
                        // 初始化View层 
                        if (uiInfoData.UiViewType != null)
                        {
                            // 查找UI对应的BaseUI子类组件
                            baseUi = uiObject.GetComponent<BaseUI>();
                        }
                    }
                }
                else // 预制体创建
                {
                    uiObject = ResManager.Instance.InstanceAssetObject(uiInfoData.UiAssetName) as GameObject;
                    if (uiObject == null)
                    {
                        Debug.LogError("无法加载对应的UI资源：" + uiInfoData.UiAssetName);
                        yield break;
                    }

                    // 初始化View层 
                    if (uiInfoData.UiViewType != null)
                    {
                        // 查找UI对应的BaseUI子类组件
                        baseUi = uiObject.GetComponent<BaseUI>();
                        if (baseUi == null)
                        {
                            // 如果没有挂载BaseUI子类组件，自动挂载
                            baseUi = uiObject.AddComponent(uiInfoData.UiViewType) as BaseUI;
                        }
                    }
                }
#if UNITY_EDITOR
            }
#endif
            _openedUIs.Add(uiType, uiObject);

            if (baseUi != null)
            {
                yield return null;
                baseUi.UiModule = baseUiModule;
                baseUi.OnShow();
            }

        }

        #endregion
        

        
    }
}