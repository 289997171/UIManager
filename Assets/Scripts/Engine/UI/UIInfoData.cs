using System;

namespace Engine.UI
{
    /// <summary>
    ///     Enum user interface type.
    ///     UI面板类型
    /// </summary>
    public enum EnumUIType
    {
        /// <summary>
        ///     The none.
        /// </summary>
        None = -1,

        // TODO 需要添加
        TestOne,
        TestTwo,
        TestThree,
        WaitingUI,          //显示网络等待的mask，转菊花！
        DialogUI,
        SeverUI,
        LoginUI,            //登录UI
        ChoseUI,
        FightUI,            //战斗UI
        MAXTYPE
    }


    public class UIInfoData
    {
        public UIInfoData(EnumUIType uiType, string uiAssetName, string uiAssetBundleName, Type uiViewType, Type uiModuleType,
            EnumUIType[] closeUiTypes)
        {
            UiType = uiType;
            UiAssetName = uiAssetName;
            UiAssetBundleName = uiAssetBundleName;
            UiViewType = uiViewType;
            UiModuleType = uiModuleType;
            CloseUiTypes = closeUiTypes;
        }

        /// <summary>
        ///     UI枚举类型
        /// </summary>
        /// <value>The type of the user interface.</value>
        public EnumUIType UiType { get; private set; }

        /// <summary>
        ///     UI对应的资源名称（prefab资源路径/AssetName）
        /// </summary>
        /// <value>The path.</value>
        public string UiAssetName { get; private set; }

        /// <summary>
        ///     UI对应的资源包名AssetBundleName(如果不涉及到网络相关的，可以不指定)
        /// </summary>
        public string UiAssetBundleName { get; set; }

        /// <summary>
        ///     UI对应的BaseUI子类型
        /// </summary>
        public Type UiViewType { get; private set; }

        /// <summary>
        ///     UI对应的BaseUI子类型
        /// </summary>
        public Type UiModuleType { get; private set; }

        /// <summary>
        ///     打开窗口时，需要关闭的其他窗口
        /// </summary>
        public EnumUIType[] CloseUiTypes { get; private set; }

        /// <summary>
        /// 非配置属性，是状态属性
        /// </summary>
        public bool IsHide { get; set; }


    }
}