using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

/// <summary>
/// Example UI controller demonstrating how to use SmileSoftScreenRecordController.
///
/// iOS recording flow:
///   1. StartRecording()         — recording begins
///   2. StopRecording()          — triggers processing
///   3. OnRecordingProcessing    — show spinner (file being encoded)
///   4. OnRecordingSaved(path)   — file is ready; hide spinner, open video player
///
/// iOS / Android free-version flow:
///   1. StartRecording()         — recording begins
///   2. Native plugin auto-stops at free-tier limit
///   3. OnRecordingProcessing    — show spinner while the partial clip is saved
///   4. OnFreeVersionLimitReachedEvent(path) — native iOS alert + completion UI with partial recording
/// </summary>
public class ExampleScreenRecorder : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject afterVideoCompletePanel;        // spinner shown while iOS encodes
    [SerializeField] private Text savedPathText;
    [SerializeField] private Button previewButton;
    [SerializeField] private Button shareButton;
    [SerializeField] private AlertPanel alertPanel;

    [Header("Free Version")]
    [Tooltip("Android only — iOS free version shows a native UIAlert instead.")]
    [SerializeField] private string freeVersionLimitMessage =
        "Free version records only 5 seconds. Upgrade to Pro for unlimited recording time.";

    private string _recordedFilePath;

    private void OnEnable()
    {
        SmileSoftScreenRecordController.OnFreeVersionLimitReachedEvent += HandleFreeVersionLimitReached;
        SmileSoftScreenRecordController.OnIosRecordingProcessing += HandleIosRecordingProcessing;
    }

    private void OnDisable()
    {
        SmileSoftScreenRecordController.OnFreeVersionLimitReachedEvent -= HandleFreeVersionLimitReached;
        SmileSoftScreenRecordController.OnIosRecordingProcessing -= HandleIosRecordingProcessing;
    }

    private void Start()
    {
        alertPanel.Hide();
        HideAfterVideoCompletePanel();
    }

    // ── Recording controls ────────────────────────────────────────────────────

    public void StartRecording()
    {
        SetFileName();

        if (SmileSoftScreenRecordController.instance.IsIosPlatform() &&
            !SmileSoftScreenRecordController.instance.IsRecordingAvailable())
        {
            alertPanel.ShowAlert("Screen recorder unavailable on this device.");
            return;
        }

        SmileSoftScreenRecordController.instance.StartRecording();
    }

    public void StopRecording()
    {
        _recordedFilePath = null;
        HideAfterVideoCompletePanel();

        SmileSoftScreenRecordController.instance.StopRecording(path =>
            {
                _recordedFilePath = string.IsNullOrEmpty(path) ? string.Empty : path;
                Debug.Log("[SmileSoftScreenRecordController] iOS record path: " +
                          (string.IsNullOrEmpty(_recordedFilePath) ? "(failed)" : _recordedFilePath));
                ShowVideoCompleteDialog(_recordedFilePath);

                //  SmileSoftScreenRecordController.instance.PreviewVideo(_recordedFilePath);
            });
    }

    public void PreviewVideo()
    {
        string path = _recordedFilePath;
        if (string.IsNullOrEmpty(path) && SmileSoftScreenRecordController.instance != null)
            path = SmileSoftScreenRecordController.instance.LastIosRecordingPath;

        if (string.IsNullOrEmpty(path))
        {
            alertPanel.ShowAlert("No recording available to preview.");
            return;
        }

        SmileSoftScreenRecordController.instance.PreviewVideo(path);
    }

    public void ShareVideo()
    {
        SmileSoftScreenRecordController.instance.ShareVideo(
            _recordedFilePath, "Greetings From SmileSoft", "Sunshine Native Share");
    }

    public void HideAfterVideoCompletePanel()
    {
        afterVideoCompletePanel.SetActive(false);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private void SetFileName()
    {
        System.DateTime now = System.DateTime.Now;
        string date = now.ToShortDateString().Replace('/', '_')
                    + now.ToLongTimeString().Replace(':', '_');
        SmileSoftScreenRecordController.instance.SetVideoName("Record_" + date);
    }

    private void ShowVideoCompleteDialog(string filePath, bool isFreeLimitReached = false)
    {
        afterVideoCompletePanel.SetActive(true);
        Debug.Log("U>> in video complete dialogue file path - " + filePath);
        bool fileReady = !string.IsNullOrEmpty(filePath);
        previewButton.interactable = fileReady;
        // Share is disabled in the free iOS native framework; keep the button off after a limit stop.
        shareButton.interactable = fileReady && !isFreeLimitReached;
        savedPathText.text = fileReady
            ? "Video saved: " + filePath
            : "Recording failed or path unavailable.";
    }

    private void HandleIosRecordingProcessing()
    {
        afterVideoCompletePanel.SetActive(true);
        previewButton.interactable = false;
        shareButton.interactable = false;
        savedPathText.text = "Processing recording...";
    }

    /// <summary>
    /// Handles <see cref="SmileSoftScreenRecordController.OnFreeVersionLimitReachedEvent"/>.
    /// Called when the free-version duration limit is hit and recording is auto-stopped.
    /// </summary>
    /// <param name="videoPath">Absolute path to the saved partial recording.</param>
    private void HandleFreeVersionLimitReached(string videoPath)
    {
        _recordedFilePath = videoPath;
        ShowVideoCompleteDialog(videoPath, isFreeLimitReached: true);
    }
}
