using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
public static class WebBuild {
 public static void Build(){
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   new GameObject("Quiz Royale").AddComponent<Royale>();
   EditorSceneManager.SaveScene(scene,"Assets/Arena.unity");
   PlayerSettings.companyName="Wildlight";PlayerSettings.productName="Quiz Royale";
   PlayerSettings.defaultWebScreenWidth=1280;PlayerSettings.defaultWebScreenHeight=800;
   PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
   PlayerSettings.WebGL.dataCaching=true;PlayerSettings.WebGL.memorySize=256;
   PlayerSettings.WebGL.exceptionSupport=WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
   PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Gamma;
   AssetDatabase.SaveAssets();
   PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL,ManagedStrippingLevel.Low);
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Arena.unity"},locationPathName="docs",target=BuildTarget.WebGL,options=BuildOptions.None});
   if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("WebGL build failed: "+report.summary.result);
   Debug.Log("QUIZ_ROYALE_BUILD_OK "+report.summary.totalSize);
 }
}
