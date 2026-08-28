# Image Mosaic Editor

A Windows image mosaic/censor editor with drag-and-drop batch processing, manual correction tools, and AI-assisted automatic region detection.

> Windows용 이미지 모자이크 편집기입니다. 이미지/폴더 드래그 앤 드롭, 자동 검출, 수동 모자이크, 마스크 지우개, 일괄 처리를 지원합니다.

<p align="center">
  <img src="docs/screenshot-main.png" alt="Image Mosaic Editor main window" width="900">
</p>

## Features

- **AI-assisted automatic mosaic** for supported images
- **Image and folder drag & drop**
- **Batch folder processing** with a generated `_mosaic` output folder
- **Manual mosaic selection** by dragging over the image
- **Mask eraser** to restore incorrectly processed areas from the untouched source image
- **Undo / Redo** history
- **Folder thumbnail browser** for quickly switching between images
- **Reprocess from the original image** so changing detection settings does not stack effects on top of previous results
- **CUDA-first execution** when a compatible GPU runtime is available, with automatic CPU fallback
- **Installer and portable builds** from GitHub Releases
- **Multilingual UI** with automatic locale detection and a persistent language selector

Supported import formats: **PNG, JPG/JPEG, WEBP**.

## Multilingual UI / 다국어 지원

The app detects the Windows UI locale on first launch and chooses a supported language automatically. You can change it later from **Settings → Language**; the selected language is saved for future launches and the main UI updates immediately.

처음 실행할 때 Windows 표시 언어를 감지해 기본 언어를 선택합니다. 이후 **설정 → 언어**에서 언제든 변경할 수 있으며, 선택한 언어는 저장되고 메인 UI에 즉시 반영됩니다.

| Locale | Language |
| --- | --- |
| `ko` | 한국어 |
| `en` | English |
| `ja` | 日本語 |
| `zh-Hans` | 简体中文 |

<p align="center">
  <img src="docs/screenshot-settings.png" alt="Language and automatic mosaic settings" width="620">
</p>

The automatic-processing bridge also receives the selected UI language, so progress messages from the Python detection process follow the application language.

## Quick start

1. Open the latest GitHub Release.
2. Choose either the installer or portable package.
3. Launch **ImageMosaicEditor**.
4. Drag an image into the window, use **Open**, or drag an entire folder.
5. Review the automatic result. Use **Ctrl+E** to erase incorrect mask areas or **Ctrl+M** to add a manual mosaic region.
6. Save the edited image.

### Download choices

- **`ImageMosaicEditor-Setup-win-x64.exe`** — normal Windows installer with Start Menu / uninstall registration.
- **`ImageMosaicEditor-win-x64-Portable.zip`** — extract and run without installation.

Latest releases: <https://github.com/jeong-jimin-github/image-mozaic/releases/latest>

Both release packages are designed to include the self-contained .NET runtime and the Python automatic-detection environment used by the application.

## Automatic mosaic

Opening or dropping an image can run the automatic processing flow. The detector works from a preserved source bitmap, which means running the process again after changing settings starts from the original image rather than repeatedly mosaicing an already processed result.

Available effect modes:

- `mosaic`
- `black`
- `blur`

The settings dialog also provides detection confidence, padding, strength, optional extra targets, and the application language selector.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl+O` | Open image |
| `Ctrl+S` | Save |
| `Ctrl+Shift+A` | Reprocess current image from the original |
| `Ctrl+M` | Mosaic selection mode |
| `Ctrl+E` | Mask eraser mode |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` / `Ctrl+Shift+Z` | Redo |

## GPU / CPU execution

The bundled detection environment prefers **CUDA** when a compatible runtime is available. If CUDA cannot be used, processing falls back to the CPU automatically. The current execution mode is shown in the application status bar when detected.

## Build from source

Requirements for a normal development build:

- Windows
- .NET 8 SDK
- Python 3.10+ for automatic-detection development outside the packaged Release environment

```powershell
git clone https://github.com/jeong-jimin-github/image-mozaic.git
cd image-mozaic
dotnet restore ImageMosaicEditor.sln
dotnet build ImageMosaicEditor.sln -c Release
```

The GitHub Actions release workflow builds the self-contained Windows application, bundles the Python runtime/dependencies, creates a portable ZIP, builds the installer, and publishes a GitHub Release.

## UI icons

Toolbar and empty-state icons are sourced from **Bootstrap Icons 1.11.3** through jsDelivr and stored under `assets/icons/`. They are rendered from the original SVG assets at runtime rather than being manually redrawn with WinForms primitives.

Bootstrap Icons: <https://icons.getbootstrap.com/> — MIT licensed.

## Notes

Automatic detection is not guaranteed to be perfect. Review the output and use the manual selection or mask eraser tools when necessary.
