# 📱 Native Screen Recorder for Android & iOS — Unity Plugin

> **Record gameplay. Save to gallery. Share anywhere. All with a single line of code.**

[![Unity Version](https://img.shields.io/badge/Unity-6000.3%2B-black?logo=unity)](https://unity.com)
[![Android](https://img.shields.io/badge/Android-5.1%2B-green?logo=android)](https://assetstore.unity.com/packages/tools/integration/native-screen-recorder-for-android-ios-180403)
[![iOS](https://img.shields.io/badge/iOS-9.0%2B-blue?logo=apple)](https://assetstore.unity.com/packages/tools/integration/native-screen-recorder-for-android-ios-180403)
[![License](https://img.shields.io/badge/License-Unity%20Asset%20Store%20EULA-red)](https://unity.com/legal/as-terms)
[![Asset Store](https://img.shields.io/badge/Asset%20Store-$30-orange)](https://assetstore.unity.com/packages/tools/integration/native-screen-recorder-for-android-ios-180403)

A **cross-platform Unity screen recorder** that uses native OS APIs — Android's `MediaRecorder` with Foreground Service and iOS's `ReplayKit` — to capture gameplay with zero third-party dependencies. No SDK tokens. No subscriptions. No boilerplate.

---

## 🚀 Why Developers Love This Plugin

Most Unity screen recording solutions require heavy third-party SDKs, complex setup, or only work on one platform. This plugin solves all of that:

- ✅ **One API, both platforms** — Android and iOS with a unified C# interface
- ✅ **Zero dependencies** — uses OS-native APIs; no external libraries to maintain
- ✅ **Minimal performance impact** — the OS handles encoding, not your game loop
- ✅ **Tiny footprint** — only 1.2 MB; won't bloat your project
- ✅ **Built-in, URP, and HDRP compatible** — works with every Unity render pipeline
- ✅ **Active maintenance** — updated to Unity 6 (6000.3), latest Android 13+, iOS 9+

---

## ⚡ Quick Start

```csharp
// Start recording
SmileSoftScreenRecordController.instance.StartRecording();

// Stop and save to gallery
SmileSoftScreenRecordController.instance.StopRecording();
```

That's it for most use cases. Drop the prefab, call two methods, ship.

---

## 🆓 Free vs 💎 Pro

| Feature | Free (This Repo) | Pro ([Asset Store](https://assetstore.unity.com/packages/tools/integration/native-screen-recorder-for-android-ios-180403)) |
|---|:---:|:---:|
| Android recording (MediaRecorder API) | ✅ | ✅ |
| iOS recording (ReplayKit) | ✅ | ✅ |
| Save to device gallery | ✅ | ✅ |
| Microphone audio recording | ✅ | ✅ |
| System/in-game audio (Android) | ✅ | ✅ |
| Configure bitrate, FPS, resolution, encoder | ✅ | ✅ |
| Easy Initializer (Inspector setup) | ✅ | ✅ |
| Callbacks (OnStarted, OnStopped, OnFailed) | ✅ | ✅ |
| **Recording duration** | ⏱ **5 seconds max** | ♾ **Unlimited** |
| **Native share dialog** (Android & iOS) | ❌ | ✅ |

**The free version is fully functional for testing and prototyping.** When you're ready to ship, upgrade to Pro for unlimited recording and native sharing.

---

## 💎 Upgrade to Pro — $30 One-Time

**[→ Get the Pro Version on Unity Asset Store](https://assetstore.unity.com/packages/tools/integration/native-screen-recorder-for-android-ios-180403)**

### How to Install Pro Over Free

No migration headaches. No code rewrites.

1. Purchase the plugin on the Unity Asset Store
2. Import the `.unitypackage` into your existing project
3. All Pro functionality activates automatically — it overrides the free version seamlessly

> 💡 **Zero extra code required.** Your existing `StartRecording()` / `StopRecording()` calls work identically. Pro just removes the 5-second cap and unlocks sharing.

---

## 📦 Download Free Version

Install the free version directly from GitHub as a `.unitypackage`:

| Version | Unity | Release Date | Download |
|---|---|---|---|
| **v5.0.3** ⭐ *(Latest)* | 6000.3+ | Mar 9, 2026 | [Download](../../releases/tag/v5.0.3) |
| v4.1.0 | 2022.3+ | Oct 2024 | [Download](../../releases/tag/v4.1.0) |
| v3.0.0 | 2021.3+ | Jan 2024 | [Download](../../releases/tag/v3.0.0) |

**Installation:**
1. Download the `.unitypackage` for your Unity version
2. In Unity: `Assets → Import Package → Custom Package`
3. Import all files
4. Drag `Assets → SunShine Android Native Screen Recorder → Prefab → Screen Recorder` into your scene

---

## 🔧 Core Features

### 🎬 Recording
- Start and stop recording with one method call each
- Records everything visible — game view, UI overlays, HUD — exactly as the player sees it
- Callbacks for permission granted, recording started, and recording stopped

### 🎙 Audio Options
| Mode | Android | iOS |
|---|:---:|:---:|
| No audio | ✅ | ✅ |
| Microphone | ✅ | ✅ |
| System/in-game audio | ✅ | ❌ |

### 🤖 Android-Specific
- `SetBitRate()` — control video quality
- `SetVideoSize(width, height)` — custom resolution
- `SetVideoEncoder()` — choose encoder per Android API level
- `SetVideoRotation()` — 0, 90, 180, 270 degrees
- `SetStoredFolderName()` — custom save folder
- `SetGalleryAddingCapabilities()` — auto-add to gallery

### 🍎 iOS-Specific
- `SetIosSaveToPhotos()` — save to Camera Roll
- `SetIosSaveToDocuments()` — save to app private directory
- Native ReplayKit preview window with trim-before-save

### 📤 Share (Pro Only)
- `ShareVideo()` — native Android share dialog
- `PreviewVideo()` — native video preview on both platforms
- iOS: `filePath` + `message` only (everything else auto-configured)

---

## 🛠 API Reference

All methods are called on `SmileSoftScreenRecordController.instance`.

```csharp
// Recording
.StartRecording()
.StopRecording()                       // Returns file path on Android

// Audio
.SetAudioRecordingMode(AudioRecordingMode.MicAudio)
// AudioRecordingMode: NoAudio | SystemAudio (Android) | MicAudio

// Android — Files & Quality
.SetVideoDestination(destination)
.SetVideoName(fileName)
.SetStoredFolderName(folderName)
.SetGalleryAddingCapabilities(true)
.SetBitRate(5242880)                   // e.g. a × 512 × 512
.SetVideoSize(1080, 1920)
.SetVideoEncoder(encoderInt)
.SetVideoRotation(0)                   // 0 | 90 | 180 | 270

// iOS — Save Options
.SetIosSaveToPhotos(true)
.SetIosSaveToDocuments(true)

// Preview & Share (Pro)
.PreviewVideo(filePath)
.ShareVideo(filePath, message, title)
```

> ⚠️ Call all configuration methods **before** `StartRecording()`.

---

## 📲 Common Use Cases

- **"Share your score"** flows in mobile games
- Post-match replay clips for battle, sports, or racing games
- Social sharing to TikTok, Instagram Reels, YouTube Shorts
- In-app QA recording for internal playtesting builds
- Tutorial capture tools for onboarding flows

---

## ⭐ What Pro Users Are Saying

> *"Works perfectly on both platforms. The setup was surprisingly painless — had it running in minutes."*

> *"Exactly what I needed. Native APIs mean no performance hit during recording. My players love the share feature."*

> *"Clean API, well documented, responsive support. Highly recommended for any mobile game that wants social sharing."*

**23 reviews · 58 favorites on the Asset Store**

---

## 🗺 Platform Compatibility

| Platform | Min Version | Render Pipelines |
|---|---|---|
| Android | 5.1 (Lollipop) — Android 13+ | Built-in, URP, HDRP |
| iOS | iPhone / iPad / iPod Touch · iOS 9.0+ | Built-in, URP, HDRP |

> ⚠️ **Not supported:** Desktop, WebGL, macOS, AR face-tracking compositing, or recording specific cameras/textures (screen-only capture).

---

## 📖 Documentation & Support

- 📄 **[Full Online Documentation](https://smilesoft.store/docs/native-screen-recorder)**
- 🛒 **[Pro Version — Unity Asset Store](https://assetstore.unity.com/packages/tools/integration/native-screen-recorder-for-android-ios-180403)**
- 💬 **[Discord Community](https://discord.gg/8HXVCdVr)**
- 🌐 **[Developer Website](https://habiburrahmanovie.com/)**
- 📧 **Email:** smilesoft4849@gmail.com

Issues and questions? Open a GitHub issue or post in the Discord — responses within 48 hours.

---

## 🔍 Keywords

`unity screen recorder` · `unity gameplay recorder` · `android screen recording unity` · `ios replaykit unity` · `unity record and save video` · `unity mobile game recorder` · `cross platform screen recorder` · `unity share gameplay` · `unity record screen with audio` · `unity video capture plugin` · `native screen capture unity` · `unity 6 screen recording`

---

*Made with ❤️ by [Smile Soft](https://assetstore.unity.com/publishers/35227)*
