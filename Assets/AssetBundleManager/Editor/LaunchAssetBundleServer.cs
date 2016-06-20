using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;


namespace AssetBundles
{
    internal class LaunchAssetBundleServer : ScriptableSingleton<LaunchAssetBundleServer>
    {
        // const string kLocalAssetbundleServerMenu = "Assets/AssetBundles/Local AssetBundle Server";
        const string kLocalAssetbundleServerMenu = "Assets/AssetBundles/3.本地网络模式";

        // 本地资源服务器进程号
        [SerializeField] int m_ServerPID = 0;

        [MenuItem(kLocalAssetbundleServerMenu)]
        public static void ToggleLocalAssetBundleServer()
        {
            // 本地资源服务器是否运行中
            bool isRunning = IsRunning();
            if (!isRunning)
            {
                // 运行本地资源服务器
                Run();
            }
            else
            {
                // 关闭正在运行的本地资源服务器
                KillRunningAssetBundleServer();
            }
        }

        /// <summary>
        /// 是否运行本地资源服务器
        /// </summary>
        /// <returns></returns>
        [MenuItem(kLocalAssetbundleServerMenu, true)]
        public static bool ToggleLocalAssetBundleServerValidate()
        {
            bool isRunnning = IsRunning();
            Menu.SetChecked(kLocalAssetbundleServerMenu, isRunnning);
            return true;
        }

        /// <summary>
        /// 本地资源服务器是否运行中
        /// </summary>
        /// <returns></returns>
        static bool IsRunning()
        {
            if (instance.m_ServerPID == 0)
                return false;

            var process = Process.GetProcessById(instance.m_ServerPID);
            if (process == null)
                return false;

            return !process.HasExited;
        }

        /// <summary>
        /// 关闭本地资源服务器
        /// </summary>
        static void KillRunningAssetBundleServer()
        {
            // Kill the last time we ran
            try
            {
                if (instance.m_ServerPID == 0)
                    return;

                var lastProcess = Process.GetProcessById(instance.m_ServerPID);
                lastProcess.Kill();
                instance.m_ServerPID = 0;
            }
            catch
            {
            }
        }

        /// <summary>
        /// 运行本地资源服务器
        /// </summary>
        static void Run()
        {
            string pathToAssetServer = Path.Combine(Application.dataPath,
                "AssetBundleManager/Editor/AssetBundleServer.exe");
            string pathToApp = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));

            KillRunningAssetBundleServer();

            // 写入本地资源服务器地址
            BuildScript.WriteServerURL();

            string args = Path.Combine(pathToApp, "AssetBundles");
            args = string.Format("\"{0}\" {1}", args, Process.GetCurrentProcess().Id);
            ProcessStartInfo startInfo =
                ExecuteInternalMono.GetProfileStartInfoForMono(
                    MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), "4.0", pathToAssetServer, args, true);
            startInfo.WorkingDirectory = Path.Combine(System.Environment.CurrentDirectory, "AssetBundles");
            startInfo.UseShellExecute = false;
            Process launchProcess = Process.Start(startInfo);
            if (launchProcess == null || launchProcess.HasExited == true || launchProcess.Id == 0)
            {
                //Unable to start process
                UnityEngine.Debug.LogError("Unable Start AssetBundleServer process");
            }
            else
            {
                //We seem to have launched, let's save the PID
                instance.m_ServerPID = launchProcess.Id;
            }
        }
    }
}