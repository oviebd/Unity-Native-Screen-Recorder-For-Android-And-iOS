using UnityEngine;

/// <summary>
/// Drop this component into your scene to configure <see cref="SmileSoftScreenRecordController"/>
/// at startup without writing any code.
/// </summary>
public class EasyScreenRecordInitializer : MonoBehaviour
{
    public const string PremiumFeatureInfo =
        "Customization features are not available in the free version. Upgrade to Pro to unlock all customization options.";

    [SerializeField] private bool useThisSetting = true;

    // ── Android settings ──────────────────────────────────────────────────────

    [Header("Android Settings")]
    [SerializeField] private string folderName = "SmileCapture";
    [Tooltip("Android + iOS. iOS: NoAudio = silent file; SystemAudio = in-app sound only; MicAudio = microphone + in-app sound.")]
    [SerializeField] private SmileSoftScreenRecordController.AudioRecordingMode audioRecordingMode
        = SmileSoftScreenRecordController.AudioRecordingMode.MicAudio;

    [Header("Android — Bitrate  (a × 512 × 512, change 'a')")]
    [SerializeField] private int bitrate = 5242880;

    [SerializeField] private int fps = 24;

    [Header("Android — Rotation (0 / 90 / 180 / 270)")]
    [SerializeField] private int videoRotation = 0;

    [SerializeField] private SmileSoftScreenRecordController.VideoEncoder videoEncoder
        = SmileSoftScreenRecordController.VideoEncoder.H264;

    [SerializeField] private bool addInGallery = false;

    // ── iOS settings ──────────────────────────────────────────────────────────

    [Header("iOS Settings")]
    [Tooltip("Pro only on iOS. The free framework always saves to Documents and never to Photos.")]
    [SerializeField] private bool iosSaveToPhotos = true;

    [Tooltip("Copy the finished recording to the app's Documents folder (accessible via Files / iTunes). Free iOS: always enabled.")]
    [SerializeField] private bool iosSaveToDocuments = true;

    [Header("Free Version (iOS)")]
    [Tooltip("Auto-stop recording after this many seconds in the free iOS framework.")]
    [SerializeField] private int iosFreeVersionMaxSeconds = 5;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (useThisSetting)
            SetUp();
    }

    private void SetUp()
    {
        SmileSoftScreenRecordController ctrl = SmileSoftScreenRecordController.instance;
        if (ctrl == null)
        {
            Debug.LogError("[EasyScreenRecordInitializer] SmileSoftScreenRecordController instance not found.");
            return;
        }

        // ── Android-only ──────────────────────────────────────────────────────
        // Uncomment the next line to store video in the persistent data path instead of public storage.
        // ctrl.SetVideoStoringDestination(Application.persistentDataPath);

        ctrl.SetStoredFolderName(folderName);
        ctrl.SetBitRate(bitrate);
        ctrl.SetFPS(fps);
        ctrl.SetVideoRotation(videoRotation);
        ctrl.SetVideoEncoder((int)videoEncoder);
        // ctrl.SetVideoSize(Screen.width, Screen.height); // uncomment to override resolution

        ctrl.SetGalleryAddingCapabilities(addInGallery);

        // ── Cross-platform ────────────────────────────────────────────────────
        ctrl.SetAudioRecordingMode(audioRecordingMode);

        // ── iOS-only ──────────────────────────────────────────────────────────
        ctrl.SetIosSaveToPhotos(iosSaveToPhotos);
        ctrl.SetIosSaveToDocuments(iosSaveToDocuments);
        ctrl.SetIosFreeVersionMaxRecordingSeconds(iosFreeVersionMaxSeconds);
    }
}
