using UnityEngine;
using UnityEditor;
using System.Collections;

namespace AssetBundles
{
    public class AssetBundlesMenuItems
    {
        // const string kSimulationMode = "Assets/AssetBundles/Simulation Mode";
        const string kSimulationMode = "Assets/AssetBundles/2.本地模拟模式";

        [MenuItem(kSimulationMode)]
        public static void ToggleSimulationMode()
        {
            // 设置为是否模拟模式
            AssetBundleManager.SimulateAssetBundleInEditor = !AssetBundleManager.SimulateAssetBundleInEditor;
        }

        [MenuItem(kSimulationMode, true)]
        public static bool ToggleSimulationModeValidate()
        {
            // 设置为是否模拟模式
            Menu.SetChecked(kSimulationMode, AssetBundleManager.SimulateAssetBundleInEditor);
            return true;
        }

        // [MenuItem ("Assets/AssetBundles/Build AssetBundles")]
        [MenuItem("Assets/AssetBundles/1.打包资源")]
        static public void BuildAssetBundles()
        {
            // 执行资源打包
            BuildScript.BuildAssetBundles();
        }
    }
}