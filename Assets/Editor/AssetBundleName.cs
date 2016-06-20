using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AssetBundleName : Editor
{

    // TODO 需要与BundlesManager 的 EnumBundleType对应
    //    public enum EnumBundleType
    //    {
    //        Common = 0,      //Common bundle
    //        Picture,         //Icon,Background bundle
    //        Music,           //音乐 bundle
    //        Wing,            //翅膀 bundle
    //        SelectRole,      //选角用到 bundle
    //        Scene,           //场景 bundle
    //        Shader,          //常驻内存shader bundle
    //        Weapon,          //武器 bundle
    //        WeaponEffect,    //武器 bundle 特效
    //        UI,              //UI bundle
    //        UIEffect,        //UI bundle 特效
    //        TaskEffect,      //任务 bundle 特效
    //        NPC,             //NPC bundle
    //        BattleEffect,    //战斗专用 effect 特效
    //        RoleEffect,      //战斗使用的对应角色 prefab
    //        Monster,         //怪物 bundle
    //        Multi,           //多人副本：战斗bundle
    //        Raid,            //关卡：战斗 bundle
    //        Tower,           //爬塔：战斗 bundle 
    //        Reward,          //悬赏战斗 bundle
    //        Scenario,        //剧情战斗 bundle
    //        Boss,            //Boss战斗 bundle 
    //        Pet,             //宠物模型 bundle
    //        Config,          //配置文件 bundle
    //        Other = 1000,    //没有划分类别 bundle，使用Resources.Load
    //    }


    // TODO 需要与BundlesManager 的 EnumBundleType对应
    public static List<string> AssetBundleNames = new List<string>()
    {
        "Common",          //Common bundle
        "Picture",         //Icon,Background bundle
        "Music",           //音乐 bundle
        "Wing",            //翅膀 bundle
        "SelectRole",      //选角用到 bundle
        "Scene",           //场景 bundle
        "Shader",          //常驻内存shader bundle
        "Weapon",          //武器 bundle
        "WeaponEffect",    //武器 bundle 特效
        "UI",              //UI bundle
        "UIEffect",        //UI bundle 特效
        "TaskEffect",      //任务 bundle 特效
        "NPC",             //NPC bundle
        "BattleEffect",    //战斗专用 effect 特效
        "RoleEffect",      //战斗使用的对应角色 prefab
        "Monster",         //怪物 bundle
        "Multi",           //多人副本：战斗bundle
        "Raid",            //关卡：战斗 bundle
        "Tower",           //爬塔：战斗 bundle 
        "Reward",          //悬赏战斗 bundle
        "Scenario",        //剧情战斗 bundle
        "Boss",            //Boss战斗 bundle 
        "Pet",             //宠物模型 bundle
        "Config",          //配置文件 bundle
        "Other",           //没有划分类别 bundle，使用Resources.Load
    };


    public static string sourcePath = Application.dataPath + "/Resources";

    // [MenuItem("Assets/AssetBundles/Auto Name")]
    [MenuItem("Assets/AssetBundles/0.自动设置包名")]
    public static void AutoSetAseetBundleNames()
    {
        ClearAssetBundlesName();

        Debug.Log("--------设置资源包名开始--------");
        ScanDirectorys(sourcePath);
        Debug.Log("所有资源包类型数量：" + AssetDatabase.GetAllAssetBundleNames().Length);
        Debug.Log("--------设置资源包名结束--------");
    }

    private static void ClearAssetBundlesName()
    {
        Debug.Log("--------重设资源包名开始--------");
        int length = AssetDatabase.GetAllAssetBundleNames().Length;
        Debug.Log("所有资源包类型数量：" + length);
        string[] oldAssetBundleNames = new string[length];
        for (int i = 0; i < length; i++)
        {
            oldAssetBundleNames[i] = AssetDatabase.GetAllAssetBundleNames()[i];
        }

        for (int j = 0; j < oldAssetBundleNames.Length; j++)
        {
            AssetDatabase.RemoveAssetBundleName(oldAssetBundleNames[j], true);
        }
        length = AssetDatabase.GetAllAssetBundleNames().Length;
        Debug.Log("所有资源包类型数量：" + length);
        Debug.Log("--------重设资源包名结束--------");
    }

    private static void ScanDirectorys(string directory, string specialName = null)
    {
        DirectoryInfo folder = new DirectoryInfo(directory);
        FileSystemInfo[] files = folder.GetFileSystemInfos();
        int length = files.Length;
        for (int i = 0; i < length; i++)
        {
            if (files[i] is DirectoryInfo)
            {

                if (specialName == null && AssetBundleNames.Contains(files[i].Name))
                {
                    Debug.Log("directory name = " + files[i].Name);
                    ScanDirectorys(files[i].FullName, files[i].Name + "");
                }
                else
                {
                    ScanDirectorys(files[i].FullName, specialName);
                }
            }
            else
            {
                if (!files[i].Name.EndsWith(".meta"))
                {
                    SetName(files[i].FullName, specialName);
                }
            }
        }
    }


    private static void SetName(string fileName, string specialName = null)
    {
        string assetName;

        string asset = fileName.Replace("\\", "/");
        string assetPath1 = "Assets" + asset.Substring(Application.dataPath.Length);

        if (specialName != null)
        {
            assetName = specialName + ".unity3d";
        }
        else
        {
            string assetPath2 = asset.Substring(Application.dataPath.Length + 1);

            assetName = assetPath2.Substring(assetPath2.IndexOf("/", StringComparison.Ordinal) + 1);
            assetName = assetName.Replace(Path.GetExtension(assetName), ".unity3d");
        }

        assetName = assetName.ToLower();
        Debug.Log("assetName = " + assetName);

        // 在代码中给资源设置AssetBundleName
        AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath1);
        assetImporter.assetBundleName = assetName;
    }
}
