using System.Collections.Generic;
using Engine.Common;
using Engine.Common.Singleton;
using Engine.UI;
using UnityEngine;

namespace Engine.Manager
{
    /// <summary>
    ///     UI数据模型管理器
    /// </summary>
    public class UIModuleManager : DDOSingleton<UIModuleManager>, IManager
    {
        private readonly Dictionary<EnumUIType, BaseUIModule> dicModules = new Dictionary<EnumUIType, BaseUIModule>();

        public bool Init()
        {
            // TODO 有些Moudle可能需要预先注册
            // add...
            // Register(EnumUIType.DialogUI);
            return true;
        }

        public BaseUIModule Register(EnumUIType uiType)
        {
            BaseUIModule uiModule;
            if (dicModules.TryGetValue(uiType, out uiModule))
            {
                // 已注册的，就返回null，防止数据模型层返回初始化
                Debug.Log("已经注册过了：" + uiType);
                return null;
            }
            UIInfoData uiInfoData = UIManager.Instance.DicUiInfoDatas[uiType];
            if (uiInfoData.UiModuleType != null)
            {
                uiModule = System.Activator.CreateInstance(uiInfoData.UiModuleType) as BaseUIModule;
                dicModules.Add(uiType, uiModule);
                return uiModule;
            }
            return uiModule;
        }

        /// <summary>
        ///     从容器中卸载
        /// </summary>
        /// <param name="uiType"></param>
        public void UnRegister(EnumUIType uiType)
        {
            BaseUIModule module;
            if (dicModules.TryGetValue(uiType, out module))
            {
                dicModules.Remove(uiType);
                module.OnRemove();
            }
            //            else
            //            {
            //                Debug.LogError("Key: " + uiType + " 尚未注册！");
            //            }
        }

        /// <summary>
        ///     卸载所有
        /// </summary>
        public void UnRegisterAll()
        {
            var _keyList = new List<EnumUIType>(dicModules.Keys);
            for (var i = 0; i < _keyList.Count; i++)
            {
                UnRegister(_keyList[i]);
            }
            dicModules.Clear();
        }

        public BaseUIModule GetUiModule(EnumUIType uiType)
        {
            BaseUIModule baseUiModule;
            if (dicModules.TryGetValue(uiType, out baseUiModule))
            {
                // 已注册的，就返回null，防止数据模型层返回初始化
                return baseUiModule;
            }
            return null;
        }
    }
}