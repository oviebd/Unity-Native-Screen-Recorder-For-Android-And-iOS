// using System;
// using System.Runtime.InteropServices;
// using UnityEngine;

// /// <summary>
// /// Static adapter that wraps the native iOS ReplayKit C API (ReplayKitBridge.mm).
// /// Not a MonoBehaviour — no scene GameObject required.
// ///
// /// All UnitySendMessage callbacks target SmileSoftScreenRecordController
// /// (same GameObject as the Android library).
// ///
// /// Callback flow:
// ///   StartRecording  →  OnRecordStartStatus("True" / "False")
// ///   StopRecording   →  OnRecordingProcessing("")       show spinner immediately
// ///                   →  OnRecordingSaved("path")        file is ready — open player
// ///                   →  OnRecordingSaved("")            failure
// /// </summary>
// public static class ReplayKitBridge
// {
//     /// <summary>
//     /// One-shot callback registered by <see cref="StopRecording(System.Action{string})"/>.
//     /// Invoked from <see cref="SmileSoftScreenRecordController.OnRecordingSaved"/> when native code finishes writing the file.
//     /// </summary>
//     private static Action<string> s_pendingOnRecordingSaved;

// #if UNITY_IOS && !UNITY_EDITOR

//     // Native symbols live in SmileSoftRecorderFree.xcframework / SmileSoftRecorderPro.xcframework (Plugins/iOS).
//     // Use the framework name that matches the xcframework bundled in this package.
// #if SMILESOFT_RECORDER_PRO
//     private const string NativeLib = "SmileSoftRecorderPro";
// #else
//     private const string NativeLib = "SmileSoftRecorderFree";
// #endif

//     /// <summary>
//     /// <paramref name="audioMode"/>: 0 = NoAudio, 1 = SystemAudio, 2 = MicAudio (same as <see cref="SmileSoftScreenRecordController.AudioRecordingMode"/>).
//     /// </summary>
//     [DllImport(NativeLib, EntryPoint = "RK_StartRecording")]
//     private static extern void Native_StartRecording(
//         int audioMode,
//         [MarshalAs(UnmanagedType.I1)] bool saveToPhotos,
//         [MarshalAs(UnmanagedType.I1)] bool saveToDocuments);

//     [DllImport(NativeLib, EntryPoint = "RK_StopRecording")]
//     private static extern void Native_StopRecording();

//     [DllImport(NativeLib, EntryPoint = "RK_IsRecordingAvailable")]
//     [return: MarshalAs(UnmanagedType.I1)]
//     private static extern bool Native_IsRecordingAvailable();

//     [DllImport(NativeLib, EntryPoint = "RK_IsRecording")]
//     [return: MarshalAs(UnmanagedType.I1)]
//     private static extern bool Native_IsRecording();

//     [DllImport(NativeLib, EntryPoint = "RK_PlayVideo")]
//     private static extern void Native_PlayVideo([MarshalAs(UnmanagedType.LPStr)] string path);

// #endif

//     // ── Public API ────────────────────────────────────────────────────────────

//     /// <summary>
//     /// Begin screen recording.
//     /// Result fires on SmileSoftScreenRecordController.OnRecordStartStatus("True"/"False").
//     /// </summary>
//     /// <param name="audioMode">0 = NoAudio, 1 = SystemAudio, 2 = MicAudio.</param>
//     public static void StartRecording(int audioMode, bool saveToPhotos, bool saveToDocuments)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         try
//         {
//             Debug.Log($"[ReplayKitBridge] StartRecording audioMode={audioMode} photos={saveToPhotos} docs={saveToDocuments}");
//             Native_StartRecording(audioMode, saveToPhotos, saveToDocuments);
//         }
//         catch (Exception e) { Debug.LogError("[ReplayKitBridge] StartRecording: " + e.Message); }
// #else
//         Debug.Log("[ReplayKitBridge] StartRecording — editor stub.");
// #endif
//     }

//     /// <summary>
//     /// Stop the current recording. Returns immediately (void).
//     /// <para>
//     /// Two callbacks will fire on SmileSoftScreenRecordController:
//     /// <list type="number">
//     ///   <item><c>OnRecordingProcessing</c> — fires immediately. Show a spinner/progress UI.</item>
//     ///   <item><c>OnRecordingSaved(path)</c> — fires when the file is fully written and ready.
//     ///         Open your VideoPlayer here. Empty string on failure.</item>
//     /// </list>
//     /// </para>
//     /// </summary>
//     // public static void StopRecording()
//     // {
//     //     StopRecording(null);
//     // }

//     /// <summary>
//     /// Stop the current recording and receive the final file path when native save (Documents and/or Photos copy) completes.
//     /// <paramref name="onRecordingSaved"/> is invoked once with the absolute path, or an empty string on failure.
//     /// Also raises <see cref="SmileSoftScreenRecordController.OnIosRecordingSaved"/> as usual.
//     /// </summary>
//     public static void StopRecording(Action<string> onRecordingSaved)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         try
//         {
//             s_pendingOnRecordingSaved = onRecordingSaved;
//             Debug.Log("[ReplayKitBridge] StopRecording.");
//             Native_StopRecording();
//         }
//         catch (Exception e)
//         {
//             s_pendingOnRecordingSaved = null;
//             Debug.LogError("[ReplayKitBridge] StopRecording: " + e.Message);
//         }
// #else
//         Debug.Log("[ReplayKitBridge] StopRecording — editor stub.");
//         onRecordingSaved?.Invoke(string.Empty);
// #endif
//     }

//     /// <summary>
//     /// Called from <see cref="SmileSoftScreenRecordController"/> when iOS native code sends <c>OnRecordingSaved</c>.
//     /// </summary>
//     internal static void NotifyRecordingSaved(string path)
//     {
//         var cb = s_pendingOnRecordingSaved;
//         s_pendingOnRecordingSaved = null;
//         cb?.Invoke(string.IsNullOrEmpty(path) ? string.Empty : path);
//     }

//     /// <summary>Whether RPScreenRecorder is available on this device.</summary>
//     public static bool IsRecordingAvailable()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         try   { return Native_IsRecordingAvailable(); }
//         catch (Exception e) { Debug.LogError("[ReplayKitBridge] IsRecordingAvailable: " + e.Message); return false; }
// #else
//         return false;
// #endif
//     }

//     /// <summary>Whether a recording session is currently active.</summary>
//     public static bool IsRecording()
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         try   { return Native_IsRecording(); }
//         catch (Exception e) { Debug.LogError("[ReplayKitBridge] IsRecording: " + e.Message); return false; }
// #else
//         return false;
// #endif
//     }

//     /// <summary>
//     /// Presents the native full-screen video player for a local file (iOS: AVPlayerViewController).
//     /// Pass a filesystem path; <c>file://</c> URLs are accepted on the native side.
//     /// </summary>
//     public static void PlayVideo(string path)
//     {
// #if UNITY_IOS && !UNITY_EDITOR
//         if (string.IsNullOrEmpty(path))
//         {
//             Debug.LogWarning("[ReplayKitBridge] PlayVideo: path is empty.");
//             return;
//         }
//         try
//         {
//             Debug.Log("[ReplayKitBridge] PlayVideo: " + path);
//             Native_PlayVideo(path);
//         }
//         catch (Exception e) { Debug.LogError("[ReplayKitBridge] PlayVideo: " + e.Message); }
// #else
//         Debug.Log("[ReplayKitBridge] PlayVideo — editor stub: " + path);
// #endif
//     }

//     #if UNITY_IOS && !UNITY_EDITOR
//     [DllImport(NativeLib, EntryPoint = "RK_ShareVideo")]
//     private static extern void Native_ShareVideo([MarshalAs(UnmanagedType.LPStr)] string path);
// #endif

// public static void ShareVideo(string path)
// {
// #if UNITY_IOS && !UNITY_EDITOR
//     if (string.IsNullOrEmpty(path))
//     {
//         Debug.LogWarning("[ReplayKitBridge] ShareVideo: path is empty.");
//         return;
//     }
//     // Strip file:// prefix if present — native side expects a plain filesystem path
//     if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
//     {
//         try { path = new Uri(path).LocalPath; } catch { }
//     }
//     try
//     {
//         Debug.Log("[ReplayKitBridge] ShareVideo: " + path);
//         Native_ShareVideo(path);
//     }
//     catch (Exception e) { Debug.LogError("[ReplayKitBridge] ShareVideo: " + e.Message); }
// #else
//     Debug.Log("[ReplayKitBridge] ShareVideo — editor stub: " + path);
// #endif
// }
// }


using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Static adapter that wraps the native iOS ReplayKit C API (ReplayKitBridge.mm).
/// Not a MonoBehaviour — no scene GameObject required.
///
/// All UnitySendMessage callbacks target the "Screen Recorder" GameObject
/// (must have <see cref="SmileSoftScreenRecordController"/> attached).
///
/// Callback flow:
///   StartRecording  →  OnRecordStartStatus("True" / "False")
///   StopRecording   →  OnRecordingProcessing("")       show spinner immediately
///                   →  OnRecordingSaved("path")        file is ready — open player
///                   →  OnRecordingSaved("")            failure
/// </summary>
public static class ReplayKitBridge
{
    /// <summary>
    /// One-shot callback registered by <see cref="StopRecording(System.Action{string})"/>.
    /// Invoked from <see cref="SmileSoftScreenRecordController.OnRecordingSaved"/> when native code finishes writing the file.
    /// </summary>
    private static Action<string> s_pendingOnRecordingSaved;

#if UNITY_IOS && !UNITY_EDITOR

    // iOS: native code is linked into UnityFramework (SmileSoftRecorderFree.framework / SmileSoftRecorderPro.framework
    // in Assets/Plugins/iOS). Use "__Internal" so IL2CPP resolves RK_* symbols at link time instead of dlopen().
    // EntryPoint names must match ReplayKitBridge.mm (extern "C" RK_* functions).
    private const string NativeLib = "__Internal";

    /// <summary>
    /// <paramref name="audioMode"/>: 0 = NoAudio, 1 = SystemAudio, 2 = MicAudio (same as <see cref="SmileSoftScreenRecordController.AudioRecordingMode"/>).
    /// </summary>
    [DllImport(NativeLib, EntryPoint = "RK_StartRecording")]
    private static extern void Native_StartRecording(
        int audioMode,
        [MarshalAs(UnmanagedType.I1)] bool saveToPhotos,
        [MarshalAs(UnmanagedType.I1)] bool saveToDocuments);

    [DllImport(NativeLib, EntryPoint = "RK_StopRecording")]
    private static extern void Native_StopRecording();

    [DllImport(NativeLib, EntryPoint = "RK_IsRecordingAvailable")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Native_IsRecordingAvailable();

    [DllImport(NativeLib, EntryPoint = "RK_IsRecording")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Native_IsRecording();

    [DllImport(NativeLib, EntryPoint = "RK_PlayVideo")]
    private static extern void Native_PlayVideo([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport(NativeLib, EntryPoint = "RK_ShareVideo")]
    private static extern void Native_ShareVideo([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport(NativeLib, EntryPoint = "RK_SetFreeVersionMaxRecordingSeconds")]
    private static extern void Native_SetFreeVersionMaxRecordingSeconds(int seconds);

#endif

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Begin screen recording.
    /// Result fires on SmileSoftScreenRecordController.OnRecordStartStatus("True"/"False").
    /// </summary>
    /// <param name="audioMode">0 = NoAudio, 1 = SystemAudio, 2 = MicAudio.</param>
    public static void StartRecording(int audioMode, bool saveToPhotos, bool saveToDocuments)
    {
#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            Debug.Log($"[ReplayKitBridge] StartRecording audioMode={audioMode} photos={saveToPhotos} docs={saveToDocuments}");
            Native_StartRecording(audioMode, saveToPhotos, saveToDocuments);
        }
        catch (Exception e) { Debug.LogError("[ReplayKitBridge] StartRecording: " + e.Message); }
#else
        Debug.Log("[ReplayKitBridge] StartRecording — editor stub.");
#endif
    }

    /// <summary>
    /// Stop the current recording. Returns immediately (void).
    /// <para>
    /// Two callbacks will fire on SmileSoftScreenRecordController:
    /// <list type="number">
    ///   <item><c>OnRecordingProcessing</c> — fires immediately. Show a spinner/progress UI.</item>
    ///   <item><c>OnRecordingSaved(path)</c> — fires when the file is fully written and ready.
    ///         Open your VideoPlayer here. Empty string on failure.</item>
    /// </list>
    /// </para>
    /// </summary>
    public static void StopRecording(Action<string> onRecordingSaved)
    {
#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            s_pendingOnRecordingSaved = onRecordingSaved;
            Debug.Log("[ReplayKitBridge] StopRecording.");
            Native_StopRecording();
        }
        catch (Exception e)
        {
            s_pendingOnRecordingSaved = null;
            Debug.LogError("[ReplayKitBridge] StopRecording: " + e.Message);
        }
#else
        Debug.Log("[ReplayKitBridge] StopRecording — editor stub.");
        onRecordingSaved?.Invoke(string.Empty);
#endif
    }

    /// <summary>
    /// Called from <see cref="SmileSoftScreenRecordController"/> when iOS native code sends <c>OnRecordingSaved</c>.
    /// </summary>
    internal static void NotifyRecordingSaved(string path)
    {
        var cb = s_pendingOnRecordingSaved;
        s_pendingOnRecordingSaved = null;
        cb?.Invoke(string.IsNullOrEmpty(path) ? string.Empty : path);
    }

    /// <summary>Whether RPScreenRecorder is available on this device.</summary>
    public static bool IsRecordingAvailable()
    {
#if UNITY_IOS && !UNITY_EDITOR
        try { return Native_IsRecordingAvailable(); }
        catch (Exception e) { Debug.LogError("[ReplayKitBridge] IsRecordingAvailable: " + e.Message); return false; }
#else
        return false;
#endif
    }

    /// <summary>Whether a recording session is currently active.</summary>
    public static bool IsRecording()
    {
#if UNITY_IOS && !UNITY_EDITOR
        try { return Native_IsRecording(); }
        catch (Exception e) { Debug.LogError("[ReplayKitBridge] IsRecording: " + e.Message); return false; }
#else
        return false;
#endif
    }

    /// <summary>
    /// Presents the native full-screen video player for a local file (iOS: AVPlayerViewController).
    /// Pass a filesystem path; <c>file://</c> URLs are accepted on the native side.
    /// </summary>
    public static void PlayVideo(string path)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[ReplayKitBridge] PlayVideo: path is empty.");
            return;
        }
        try
        {
            Debug.Log("[ReplayKitBridge] PlayVideo: " + path);
            Native_PlayVideo(path);
        }
        catch (Exception e) { Debug.LogError("[ReplayKitBridge] PlayVideo: " + e.Message); }
#else
        Debug.Log("[ReplayKitBridge] PlayVideo — editor stub: " + path);
#endif
    }

    public static void ShareVideo(string path)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[ReplayKitBridge] ShareVideo: path is empty.");
            return;
        }
        // Strip file:// prefix if present — native side expects a plain filesystem path
        if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            try { path = new Uri(path).LocalPath; } catch { }
        }
        try
        {
            Debug.Log("[ReplayKitBridge] ShareVideo: " + path);
            Native_ShareVideo(path);
        }
        catch (Exception e) { Debug.LogError("[ReplayKitBridge] ShareVideo: " + e.Message); }
#else
        Debug.Log("[ReplayKitBridge] ShareVideo — editor stub: " + path);
#endif
    }

    /// <summary>
    /// iOS free framework only: configure auto-stop duration (seconds). Default is 5.
    /// </summary>
    public static void SetFreeVersionMaxRecordingSeconds(int seconds)
    {
#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            int clamped = Mathf.Max(1, seconds);
            Debug.Log($"[ReplayKitBridge] SetFreeVersionMaxRecordingSeconds: {clamped}");
            Native_SetFreeVersionMaxRecordingSeconds(clamped);
        }
        catch (Exception e) { Debug.LogError("[ReplayKitBridge] SetFreeVersionMaxRecordingSeconds: " + e.Message); }
#else
        Debug.Log("[ReplayKitBridge] SetFreeVersionMaxRecordingSeconds — editor stub.");
#endif
    }
}