using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Unified screen-recording controller for Android and iOS.
///
/// Android: delegates to the native ScreenRecordFragment Java library via JNI.
/// iOS:     delegates to ReplayKitBridge (native RPScreenRecorder via .mm).
///
/// This GameObject MUST be named "Screen Recorder" in the scene.
/// iOS native .mm sends UnitySendMessage to this name for ReplayKit callbacks.
///
/// iOS recording lifecycle:
///   StartRecording()         → OnRecordStartStatus("True"/"False")
///   StopRecording()          → OnRecordingProcessing()   ← show spinner
///                            → OnRecordingSaved(path)    ← file ready, open player
///
/// Android free-version lifecycle:
///   Recording auto-stops at the free-tier duration limit
///   → OnFreeVersionLimitReached(json)  ← native UnitySendMessage
///   → <see cref="OnFreeVersionLimitReachedEvent"/>       ← subscribe in your UI (path string)
///
/// iOS free-version lifecycle (same callback as Android):
///   Recording auto-stops at the configured free-tier duration limit
///   → native UIAlert shown by ReplayKitBridge (not Unity C#)
///   → OnFreeVersionLimitReached(json)  ← native UnitySendMessage
///   → <see cref="OnFreeVersionLimitReachedEvent"/>           ← unified event, both platforms
///   Free iOS: saves to Documents only — never to Photos.
/// </summary>
public class SmileSoftScreenRecordController : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static SmileSoftScreenRecordController instance;

    // ── iOS events ────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired on iOS immediately after StopRecording() is called,
    /// while the video is still being encoded and written.
    /// Use this to show a processing / spinner UI.
    /// </summary>
    public static event Action OnIosRecordingProcessing;

    /// <summary>
    /// Fired on iOS when the recording file is fully written and ready to use.
    /// The string parameter is the absolute local file path.
    /// Empty string means the recording failed.
    /// This is the ONLY reliable source of the final file path on iOS.
    /// </summary>
    public static event Action<string> OnIosRecordingSaved;

    /// <summary>
    /// Fired when the native plugin auto-stops recording because the free-version
    /// duration limit was reached (Android and iOS). The string parameter is the absolute local file path of the
    /// saved partial recording. Subscribe from your UI layer (e.g. <see cref="ExampleScreenRecorder"/>)
    /// to show an upsell message, alert, or completion UI.
    /// </summary>
    public static event Action<string> OnFreeVersionLimitReachedEvent;

    // ── iOS state ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The absolute path of the most recent iOS recording.
    /// Set when OnRecordingSaved fires. Empty string on failure.
    /// </summary>
    public string LastIosRecordingPath { get; private set; } = string.Empty;

    // ── Android ───────────────────────────────────────────────────────────────

    private readonly string _fileProvider = "com.SmileSoft.unityplugin.ScreenRecordProvider";
    private AndroidJavaObject _screenRecorder;

    // ── iOS internal ──────────────────────────────────────────────────────────

    /// <summary>Matches <see cref="AudioRecordingMode"/> (native ReplayKit + optional audio strip).</summary>
    private int _iosAudioRecordingMode = (int)AudioRecordingMode.SystemAudio;
    private bool _isIosTryingToStartRecord = false;
    private bool _iosSaveToPhotos = true;
    private bool _iosSaveToDocuments = true;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (instance == null)
            instance = this;

        if (gameObject.name != "Screen Recorder")
            Debug.LogWarning(
                "[SmileSoftScreenRecordController] GameObject must be named " +
                "\"Screen Recorder\" to receive iOS native callbacks.");

        Setup();
    }

    private void Setup()
    {
        if (IsAndroidPlatform())
        {
            _screenRecorder = new AndroidJavaObject(
                "com.SmileSoft.unityplugin.ScreenCapture.ScreenRecordFragment");
            _screenRecorder?.Call("SetUp");
        }
        // iOS: ReplayKitBridge is a static class — nothing to instantiate.
    }

    // ── Focus polling (iOS) ───────────────────────────────────────────────────

    private void OnApplicationFocus(bool hasFocus)
    {
        // iOS start result is delivered via OnRecordStartStatus from native code.
        // Avoid polling IsRecording here — it can block the main thread via native dispatch_sync.
    }

    private void CheckIsRecording()
    {
        OnIosRecordStatus(ReplayKitBridge.IsRecording());
    }

    // ── Public recording API ──────────────────────────────────────────────────

    /// <summary>Start screen recording.</summary>
    public void StartRecording()
    {
        if (IsAndroidPlatform())
        {
            _screenRecorder?.Call("StartRecording");
            return;
        }

        if (IsIosPlatform())
        {
            if (!ReplayKitBridge.IsRecordingAvailable())
            {
                OnIosRecordStatus(false);
                return;
            }
            _isIosTryingToStartRecord = true;
            ReplayKitBridge.StartRecording(_iosAudioRecordingMode, _iosSaveToPhotos, _iosSaveToDocuments);
        }
    }

    /// <summary>
    /// Stop the current recording.
    /// <para>
    /// Android: returns the saved file path synchronously (file is immediately ready).<br/>
    /// iOS: returns empty string immediately. The file is not ready yet.
    ///      Subscribe to <see cref="OnIosRecordingProcessing"/> to show a spinner,
    ///      and <see cref="OnIosRecordingSaved"/> to receive the path when the file is ready,
    ///      or pass <paramref name="onIosRecordingSaved"/> for a one-shot path callback.
    /// </para>
    /// </summary>
    /// <param name="onIosRecordingSaved">
    /// iOS only: invoked once with the absolute file path when the recording is written (Documents path);
    /// empty string on failure. Ignored on Android (use the return value instead).
    /// </param>
    /// <returns>Android: file path. iOS: always empty — use <paramref name="onIosRecordingSaved"/> or <see cref="OnIosRecordingSaved"/>.</returns>
    public void StopRecording(Action<string> onIosRecordingSaved = null)
    {
        if (IsAndroidPlatform())
        {
            string path = _screenRecorder?.Call<string>("StopRecording");
            Debug.Log("[SmileSoftScreenRecordController] Android record path: " + path);
            onIosRecordingSaved?.Invoke(path);
            return;
        }

        if (IsIosPlatform())
        {
            // OnRecordingProcessing fires immediately from native side.
            // OnRecordingSaved fires when the file is fully written (and Photos copy requested, if enabled).
            ReplayKitBridge.StopRecording(path =>
            {
                string recordPath = string.IsNullOrEmpty(path) ? string.Empty : "file://" + path;
                Debug.Log("[SmileSoftScreenRecordController] iOS record path: " +
                          (string.IsNullOrEmpty(recordPath) ? "(failed)" : recordPath));
                onIosRecordingSaved?.Invoke(recordPath);
                return;
            });
        }

    }

    /// <summary>
    /// Called by native code (Android and iOS) when the free-version recording duration limit is reached
    /// and the plugin auto-stops the session. Parses the JSON payload, finalizes the recording,
    /// and raises <see cref="OnFreeVersionLimitReachedEvent"/> with the saved video path.
    /// </summary>
    /// <param name="jsonPayload">
    /// JSON string with <c>videoPath</c> and <c>isAutoStoppedForFreeUser</c>
    /// (see <see cref="FreeVersionLimitPayload"/>).
    /// </param>
    public void OnFreeVersionLimitReached(string jsonPayload)
    {
        var data = JsonUtility.FromJson<FreeVersionLimitPayload>(jsonPayload);
        if (data == null || string.IsNullOrEmpty(data.videoPath))
            return;

        string path = data.videoPath;

        Debug.Log("[SmileSoftScreenRecordController] Free-version limit reached. Video path: " + path);

        LastIosRecordingPath = path;
        OnFreeVersionLimitReachedEvent?.Invoke(path);
    }

    /// <summary>
    /// Open a video for playback.
    /// Android: opens the native video preview activity.
    /// iOS: presents the system full-screen player (<see cref="ReplayKitBridge.PlayVideo"/>) for a local file path.
    /// </summary>
    public void PreviewVideo(string videoPath)
    {
        if (IsAndroidPlatform() && videoPath != null && File.Exists(videoPath))
        {
            _screenRecorder?.Call("PreviewVideo", videoPath);
            return;
        }

        if (IsIosPlatform() && !string.IsNullOrEmpty(videoPath))
        {
            string localPath = NormalizeIosVideoPath(videoPath);

            if (!File.Exists(localPath) && !string.IsNullOrEmpty(LastIosRecordingPath))
            {
                string fallback = NormalizeIosVideoPath(LastIosRecordingPath);
                if (File.Exists(fallback))
                {
                    Debug.Log("[SmileSoftScreenRecordController] PreviewVideo: using LastIosRecordingPath fallback.");
                    localPath = fallback;
                }
            }

            if (!File.Exists(localPath))
            {
                Debug.LogWarning("[SmileSoftScreenRecordController] PreviewVideo: file not found: " + localPath);
                return;
            }

            ReplayKitBridge.PlayVideo(localPath);
        }
    }

    private static string NormalizeIosVideoPath(string videoPath)
    {
        if (string.IsNullOrEmpty(videoPath))
            return string.Empty;

        string localPath = videoPath;
        if (localPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            try { localPath = new Uri(videoPath).LocalPath; }
            catch (UriFormatException) { /* keep localPath */ }
        }
        return localPath;
    }

    // ── Android-only settings ─────────────────────────────────────────────────

    // public void SetVideoStoringDestination(string destination)
    // {
    //     if (IsAndroidPlatform())
    //         _screenRecorder?.Call("SetVideoStoringDestination", destination);
    // }

    public void SetStoredFolderName(string folderName)
    {
        if (IsAndroidPlatform())
            _screenRecorder?.Call("SetVideoStoredFolderName", folderName);
    }

    public void SetVideoName(string videoName)
    {
        if (IsAndroidPlatform())
            _screenRecorder?.Call("SetVideoName", videoName);
    }

    public void SetGalleryAddingCapabilities(bool canAddToGallery)
    {
        if (IsAndroidPlatform())
            _screenRecorder?.Call("SetGalleryAddingCapabilities", canAddToGallery);
    }

    public void SetFPS(int fps)
    {
        if (IsAndroidPlatform())
            _screenRecorder?.Call("SetVideoFps", fps);
    }

    public void SetVideoRotation(int rotation)
    {
        if (IsAndroidPlatform())
            _screenRecorder?.Call("SetVideoRotation", rotation);
    }

    public void SetBitRate(int bitRate)
    {
        if (IsAndroidPlatform())
            _screenRecorder?.Call("SetBitrate", bitRate);
    }

    public void SetVideoSize(int width, int height)
    {
        if (IsAndroidPlatform())
            _screenRecorder?.Call("SetVideoSize", width, height);
    }

    public void SetVideoEncoder(int encoder)
    {
        if (IsAndroidPlatform())
            _screenRecorder?.Call("SetVideoEncoder", encoder);
    }

    // ── Audio (cross-platform) ────────────────────────────────────────────────

    public enum AudioRecordingMode
    {
        NoAudio = 0,
        /// <summary>Android 10+: internal/app audio. iOS ReplayKit: in-app audio only (microphone off).</summary>
        SystemAudio = 1,
        /// <summary>Microphone commentary; iOS also keeps in-app audio in the capture.</summary>
        MicAudio = 2
    }

    public void SetAudioRecordingMode(AudioRecordingMode mode)
    {
        SetAudioRecordingMode((int)mode);
    }

    private void SetAudioRecordingMode(int mode)
    {
        if (IsAndroidPlatform())
        {
            _screenRecorder?.Call("SetAudioRecordingMode", mode);
            return;
        }

        if (IsIosPlatform())
            _iosAudioRecordingMode = mode;
    }

    public bool IsSystemAudioSupported()
    {
        if (IsAndroidPlatform())
            return _screenRecorder?.Call<bool>("IsSystemAudioSupported") ?? false;
        return false;
    }

    // ── iOS-only settings (configure from <see cref="EasyScreenRecordInitializer"/> or your own code) ──

    /// <summary>Save finished iOS recording to the Photos library. Default: true.</summary>
    public void SetIosSaveToPhotos(bool save) => _iosSaveToPhotos = save;

    /// <summary>Copy finished iOS recording to the app's Documents folder. Default: true.</summary>
    public void SetIosSaveToDocuments(bool save) => _iosSaveToDocuments = save;

    /// <summary>
    /// iOS free version only: max recording duration in seconds before auto-stop (default 5).
    /// No-op on Android and in the Pro iOS framework.
    /// </summary>
    public void SetIosFreeVersionMaxRecordingSeconds(int seconds)
    {
        if (IsIosPlatform())
            ReplayKitBridge.SetFreeVersionMaxRecordingSeconds(seconds);
    }

    // ── Share ─────────────────────────────────────────────────────────────────

   public void ShareVideo(string filePath, string message, string title)
{
    if (IsAndroidPlatform() && filePath != null && File.Exists(filePath))
    {
        _screenRecorder?.Call("ShareVideo", filePath, message, title, _fileProvider);
        return;
    }

    if (IsIosPlatform() && !string.IsNullOrEmpty(filePath))
    {
        ReplayKitBridge.ShareVideo(filePath); // message/title ignored — native share sheet handles UI
    }
}

    // ── Availability ──────────────────────────────────────────────────────────

    public bool IsRecordingAvailable()
    {
        if (IsIosPlatform())
            return ReplayKitBridge.IsRecordingAvailable();
        return true;
    }

    // ── Platform helpers ──────────────────────────────────────────────────────

    public bool IsAndroidPlatform()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }

    public bool IsIosPlatform()
    {
#if UNITY_IPHONE && !UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }

    // ── Native callbacks (UnitySendMessage targets) ───────────────────────────

    /// <summary>Android: called by the native library when permissions are resolved.</summary>
    public void OnRecordPermissionGranted(string status)
    {
        Debug.Log(status == "True"
            ? "[SmileSoftScreenRecordController] Permissions granted."
            : "[SmileSoftScreenRecordController] Permissions not fully granted.");
    }

    /// <summary>
    /// Called by native code (Android and iOS) when recording start succeeds or fails.
    /// </summary>
    public void OnRecordStartStatus(string status)
    {
        bool success = (status == "True");

        if (IsIosPlatform())
        {
            _isIosTryingToStartRecord = false;
            OnIosRecordStatus(success);
            return;
        }

        Debug.Log(success
            ? "[SmileSoftScreenRecordController] Record started."
            : "[SmileSoftScreenRecordController] Record failed to start.");
    }

    /// <summary>
    /// Called by native iOS code immediately after StopRecording() is called,
    /// while the video file is still being encoded and written.
    /// Use this to show a processing / spinner UI.
    /// </summary>
    public void OnRecordingProcessing(string unused)
    {
        Debug.Log("[SmileSoftScreenRecordController] iOS recording processing — show spinner.");
        OnIosRecordingProcessing?.Invoke();
    }

    /// <summary>
    /// Called by native iOS code when the recording file is fully written and ready.
    /// This is the authoritative callback — always use this path to open or share the file.
    /// Path is empty on failure.
    /// </summary>
    public void OnRecordingSaved(string path)
    {
        LastIosRecordingPath = string.IsNullOrEmpty(path) ? string.Empty : path;

        Debug.Log("[SmileSoftScreenRecordController] OnRecordingSaved: " +
                  (string.IsNullOrEmpty(LastIosRecordingPath) ? "(failed)" : LastIosRecordingPath));

        OnIosRecordingSaved?.Invoke(LastIosRecordingPath);

        if (IsIosPlatform())
            ReplayKitBridge.NotifyRecordingSaved(LastIosRecordingPath);
    }

    /// <summary>Called by native iOS code with diagnostic messages (visible in Unity console).</summary>
    public void OnNativePluginLog(string message)
    {
        Debug.Log("[SmileSoftRecorder Native] " + message);
    }

    // ── Encoder enum ──────────────────────────────────────────────────────────

    public enum VideoEncoder
    {
        DEFAULT = 0, H263 = 1, H264 = 2, MPEG_4_SP = 3, VP8 = 4, HEVC = 5
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void OnIosRecordStatus(bool success)
    {
        Debug.Log(success
            ? "[SmileSoftScreenRecordController] iOS recording started."
            : "[SmileSoftScreenRecordController] iOS recording failed to start.");
    }
}




/// <summary>
/// JSON payload sent from native code to <see cref="SmileSoftScreenRecordController.OnFreeVersionLimitReached"/>.
/// </summary>
[Serializable]
public class FreeVersionLimitPayload
{
    /// <summary>Absolute path to the auto-stopped partial MP4 file.</summary>
    public string videoPath = string.Empty;

    /// <summary>Always <c>true</c> for this callback.</summary>
    public bool isAutoStoppedForFreeUser = false;
}
