using Engine.Common.EventMessage;

namespace Engine.UI
{
    /// <summary>
    ///     数据模型层，一般情况下，一类数据模型只会创建一个对象，存放在UIModuleManager
    /// </summary>
    public abstract class BaseUIModule
    {
        #region 子类实现扩展

        /// <summary>
        ///     UIModuleManager注册数据模型层后，用于初始化操作
        ///     如，实现方法建议异步加载初始化数据（如发起网路请求，或读取配置获得初始数据）
        /// </summary>
        public abstract void OnRegister();
        //{
        //    MessageCenter.Instance.AddListener("xxx", xxx);
        //}

        /// <summary>
        ///     UIModuleManager卸载数据模型层后，用于销毁操作
        ///     如，一个数据模型被卸载了，可能会涉及到一些全局数据的销毁操作
        /// </summary>
        public abstract void OnRemove();
        //{
        //    MessageCenter.Instance.RemoveListener("xxx", xxx);
        //}

        #endregion
    }
}