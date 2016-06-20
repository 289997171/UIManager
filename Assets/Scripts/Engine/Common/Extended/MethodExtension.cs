using UnityEngine;

namespace Engine.Common.Extended
{
    /// <summary>
    /// 类的成员属性扩展
    /// </summary>
	public static class MethodExtension
    {
        /// <summary>
        /// Gets the or add component.
        /// </summary>
        /// <returns>The or add component.</returns>
        /// <param name="go">Go.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T ret = go.GetComponent<T>();
            if (null == ret)
                ret = go.AddComponent<T>();
            return ret;
        }

    }
}