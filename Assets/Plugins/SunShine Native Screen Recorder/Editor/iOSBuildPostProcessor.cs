#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

/// <summary>
/// Post-build processor that adds the required iOS frameworks and
/// Info.plist usage descriptions for the ReplayKit native bridge.
/// Place this file anywhere inside an Editor folder.
/// </summary>
public static class iOSBuildPostProcessor
{
    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.iOS)
            return;

        string pbxPath = PBXProject.GetPBXProjectPath(buildPath);
        PBXProject project = new PBXProject();
        project.ReadFromFile(pbxPath);

        // Unity 2019.3+: use the UnityFramework target for native plug-in code.
        string frameworkTarget = project.GetUnityFrameworkTargetGuid();
        // Main app target (needed for some framework linkages).
        string mainTarget = project.GetUnityMainTargetGuid();

        // ── Required frameworks ───────────────────────────────────────────────
        // ReplayKit  — screen recording
        // Photos     — PHPhotoLibrary / PHAuthorizationStatus (_OBJC_CLASS_$_PHPhotoLibrary)
        // UIKit      — UISaveVideoAtPathToSavedPhotosAlbum

        AddFramework(project, frameworkTarget, "ReplayKit.framework",  false);
        AddFramework(project, frameworkTarget, "Photos.framework",     false);
        AddFramework(project, frameworkTarget, "UIKit.framework",      false);

        // Also link on the main target to be safe.
        AddFramework(project, mainTarget, "ReplayKit.framework",  false);
        AddFramework(project, mainTarget, "Photos.framework",     false);
        AddFramework(project, mainTarget, "UIKit.framework",      false);

        project.WriteToFile(pbxPath);

        // ── Info.plist usage descriptions ─────────────────────────────────────
        // NSPhotoLibraryAddUsageDescription is required when saving to Photos.
        // NSMicrophoneUsageDescription is required when mic recording is enabled.

        string plistPath = Path.Combine(buildPath, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        PlistElementDict root = plist.root;

        SetPlistKeyIfMissing(root,
            "NSPhotoLibraryAddUsageDescription",
            "This app saves recorded videos to your photo library.");

        SetPlistKeyIfMissing(root,
            "NSMicrophoneUsageDescription",
            "This app uses the microphone to record audio with your screen recording.");

        plist.WriteToFile(plistPath);

        UnityEngine.Debug.Log("[iOSBuildPostProcessor] Frameworks and plist keys added successfully.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AddFramework(PBXProject project, string targetGuid,
                                     string framework, bool weak)
    {
        // AddFrameworkToProject is a no-op if already present, so safe to call multiple times.
        project.AddFrameworkToProject(targetGuid, framework, weak);
    }

    private static void SetPlistKeyIfMissing(PlistElementDict root, string key, string value)
    {
        if (!root.values.ContainsKey(key))
            root.SetString(key, value);
    }
}
#endif
