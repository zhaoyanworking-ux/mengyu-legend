#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChibiARPG.EditorTools
{
    public static class WebGLBuildMenu
    {
        const string OutputDir = "Build/WebGL";

        [MenuItem("萌域传说/构建 WebGL（手机浏览器 / Android）", false, 20)]
        public static void BuildCompressed()
        {
            BuildInternal(false);
        }

        [MenuItem("萌域传说/构建 WebGL（无压缩，便于本地测试）", false, 21)]
        public static void BuildUncompressed()
        {
            BuildInternal(true);
        }

        [MenuItem("萌域传说/打开构建输出目录", false, 22)]
        public static void OpenOutput()
        {
            Directory.CreateDirectory(OutputDir);
            EditorUtility.RevealInFinder(OutputDir);
        }

        static void BuildInternal(bool uncompressed)
        {
            if (EditorBuildSettings.scenes == null || EditorBuildSettings.scenes.Length == 0)
                DemoSceneMenu.GenerateScene();

            ApplyAndroidFriendlyWebGLSettings(uncompressed);

            Directory.CreateDirectory(OutputDir);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/QinglanTown.unity" },
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            UnityEngine.Debug.Log("WebGL 构建结束：" + report.summary.result + "  输出目录 " + Path.GetFullPath(OutputDir));
        }

        /// <summary>
        /// 针对 Android Chrome 等移动浏览器的 WebGL 设置：
        /// 控制内存增长、开启解压回退、关闭异常开销、限制质量、使用移动模板。
        /// 中端安卓请保持分辨率缩放，避免开启高 DPI 与硬阴影。
        /// </summary>
        public static void ApplyAndroidFriendlyWebGLSettings(bool uncompressed)
        {
            PlayerSettings.companyName = "YanZhao";
            PlayerSettings.productName = "萌域传说";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetMobileMTRendering(BuildTargetGroup.WebGL, false);
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            PlayerSettings.WebGL.compressionFormat = uncompressed
                ? WebGLCompressionFormat.Disabled
                : WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.debugSymbols = false;
            PlayerSettings.WebGL.template = "PROJECT:ChibiMobile";

#if UNITY_2021_2_OR_NEWER
            PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.WebGL.memoryGeometricGrowthStep = 0.2f;
#endif

            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;

            // 移动端质量：关闭重阴影，降低像素灯，利于中端 Android
            QualitySettings.names.ToString();
            QualitySettings.SetQualityLevel(Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1), true);
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.pixelLightCount = 1;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.billboardsFaceCameraPosition = true;
            QualitySettings.skinWeights = SkinWeights.TwoBones;
            QualitySettings.lodBias = 0.8f;
            QualitySettings.particleRaycastBudget = 4;
            QualitySettings.asyncUploadTimeSlice = 2;
            QualitySettings.asyncUploadBufferSize = 4;
            QualitySettings.resolutionScalingFixedDPIFactor = 1f;

            UnityEngine.Debug.Log(
                "已应用面向 Android Chrome 的 WebGL 设置：Gzip=" + (!uncompressed) +
                "，解压回退开启，内存几何增长 128–1024MB，无异常支持，关闭阴影，模板 ChibiMobile。" +
                "中端安卓建议在自定义模板中把 devicePixelRatio 限制到 1.25–1.5。");
        }
    }
}
#endif
