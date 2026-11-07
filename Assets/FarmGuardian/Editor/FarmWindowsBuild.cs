using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace FarmGuardian.Editor
{
    public static class FarmWindowsBuild
    {
        [MenuItem("Tools/Farm Guardian/Build Windows EXE")]
        public static void Build()
        {
            const string scene="Assets/FarmGuardian/Scenes/FarmGuardian.unity";
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Windows Build Support is not installed.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene)==null)throw new InvalidOperationException("FarmGuardian scene is missing.");
            var config=AssetDatabase.LoadAssetAtPath<FarmConfig>("Assets/FarmGuardian/Data/FarmGuardianConfig.asset");
            if(config==null)throw new InvalidOperationException("FarmGuardian configuration is missing.");
            foreach(var skin in FarmSkinCatalog.All)
                if(FarmSkinCatalog.Model(config,skin.Id)==null)throw new InvalidOperationException("Missing skin: "+skin.Id);
            if(Resources.Load<Texture2D>("CozyUI/CloverCoin")==null)throw new InvalidOperationException("Clover coin must be a Texture2D.");
            string folder="FarmGuardian-Windows";
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length;i++)if(args[i]=="-farmBuildFolder")
            {
                if(i+1>=args.Length)throw new ArgumentException("Missing -farmBuildFolder value.");
                folder=args[++i];
                if(!folder.StartsWith("FarmGuardian-Windows",StringComparison.Ordinal)||folder.IndexOfAny(Path.GetInvalidFileNameChars())>=0)
                    throw new ArgumentException("Build folder must be a FarmGuardian-Windows-prefixed directory name inside Builds.");
            }
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds",folder));
            Directory.CreateDirectory(output);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.productName="Farm Guardian";
            PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;PlayerSettings.defaultIsNativeResolution=true;
            PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;
            PlayerSettings.resizableWindow=true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{scene},locationPathName=Path.Combine(output,"FarmGuardian.exe"),
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: "+report.summary.result+", errors="+report.summary.totalErrors);
            Debug.Log("FARM_WINDOWS_BUILD_OK: "+output+", bytes="+report.summary.totalSize);
        }
    }
}
