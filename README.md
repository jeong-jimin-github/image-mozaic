# ImageMosaicEditor

.NET 8 Windows Forms 기반 이미지 모자이크 편집기에 `tnisizawa/anime-mosaic`의 자동 검출 흐름을 WinForms용으로 재구현한 프로젝트입니다.

기존 수동 드래그 모자이크와 Undo/Redo를 유지하면서 **이미지 import 즉시 자동 검열**, **드래그 앤드롭 import**, **진행률 창**, **폴더 일괄 처리**, **mosaic / black / blur** 처리를 지원합니다.

> `anime-mosaic` 저장소의 공개 동작과 옵션 구성을 참고했으며 원본 Python 소스를 그대로 복사하지 않습니다.

## 주요 기능

| 기능 | 설명 |
|---|---|
| 이미지 열기 | PNG/JPG/JPEG를 열면 즉시 자동 검열 시작 |
| 드래그 앤드롭 | 파일을 창이나 이미지 영역에 놓으면 즉시 import + 자동 검열 |
| 진행률 창 | 자동 검출/추가 검출/효과 적용/저장 단계를 0~100%로 표시 |
| 수동 모자이크 | 마우스로 영역을 드래그해 즉시 픽셀 모자이크 적용 |
| Undo / Redo | `Ctrl+Z`, `Ctrl+Y` |
| 다시 자동 처리 | `Ctrl+Shift+A` |
| 폴더 일괄 처리 | JPG/JPEG/PNG/WebP 폴더를 처리하고 `<폴더명>_mosaic`에 저장 |
| 처리 방식 | `mosaic`, `black`, `blur` |
| 덮어쓰기 저장 | 현재 편집 이미지를 원본 파일에 저장 |

## 자동 검출

NTD11 기능은 제거했습니다. 자동 검출은 `dghs-imgutils` 계열로 통일되어 있습니다.

기본 검출 대상은 `pussy`, `penis`이며 아래 추가 검출 3종은 **기본값이 모두 ON**입니다.

- 유두: `detect_censors`의 `nipple_f`
- 항문: NudeNet 보조 검출의 `ANUS_EXPOSED` / `ANUS_COVERED`
- 고환/남성 생식기: NTD11 제거 후 NudeNet의 `MALE_GENITALIA_EXPOSED` / `MALE_GENITALIA_COVERED` 영역을 넓은 안전 영역으로 사용

설정에서 각 추가 검출을 개별적으로 끌 수 있습니다.

## 진행률 표시

자동 검열 중에는 별도 진행 창이 열립니다.

한 장 처리 시 이미지 로딩 → 기본 검출 → 추가 검출 → 효과 적용 → 저장 순서로 진행률과 현재 단계를 표시합니다. 폴더 일괄 처리에서는 전체 파일 수를 기준으로 실제 처리 비율이 증가합니다.

## Release 다운로드 선택

각 GitHub Release에는 두 가지 파일을 동시에 제공합니다.

1. **`ImageMosaicEditor-Setup-win-x64.exe`**
   - 설치 마법사 방식
   - Program Files에 설치
   - 시작 메뉴 등록
   - 프로그램 제거 항목 등록
   - 바탕화면 바로가기는 설치 중 선택 가능

2. **`ImageMosaicEditor-win-x64-Portable.zip`**
   - 무설치 Portable 방식
   - 원하는 폴더에 압축 해제 후 `ImageMosaicEditor.exe` 실행

두 배포 파일 모두 같은 프로그램 파일을 사용하며 다음 런타임/의존성을 포함합니다.

- Windows x64 self-contained .NET 8 런타임
- Python 3.12 임베디드 런타임
- `dghs-imgutils==0.19.0`
- `Pillow`
- `onnxruntime`
- 자동 검열 Python 브리지

따라서 Release 사용자는 .NET Runtime이나 Python을 별도로 설치할 필요가 없습니다. `dghs-imgutils`가 사용하는 모델 데이터는 라이브러리 동작에 따라 최초 검출 시 다운로드될 수 있습니다.

## 사용 방법

### 단일 이미지

1. **파일 > 열기** 또는 `Ctrl+O`로 PNG/JPG/JPEG를 선택하거나 파일을 창에 드래그 앤드롭합니다.
2. 이미지가 표시된 직후 자동 검열과 진행률 창이 시작됩니다.
3. 결과는 Undo 스택에 저장되어 `Ctrl+Z`로 자동 처리 전 상태로 돌아갈 수 있습니다.
4. 필요하면 `Ctrl+Shift+A`로 현재 편집 상태를 다시 자동 처리합니다.

### 폴더 일괄 처리

**자동 모자이크 > 폴더 일괄 처리**에서 입력 폴더를 선택합니다. 결과는 같은 위치의 `<입력폴더명>_mosaic` 폴더에 생성되며 미검출 이미지도 원본 그대로 복사됩니다.

## 기본 설정

| 설정 | 기본값 |
|---|---:|
| Mode | `mosaic` |
| Strength | `20` |
| Confidence | `0.25` |
| Padding | `10` |
| 유두 추가 검출 | ON |
| 항문 추가 검출 | ON |
| 고환/남성 생식기 추가 검출 | ON |

## 개발 빌드

```bash
dotnet run
```

일반 self-contained publish:

```bash
dotnet publish ImageMosaicEditor.csproj -c Release -r win-x64 --self-contained true -o publish
```

Portable Python 런타임과 설치 마법사 EXE는 GitHub Actions Release workflow에서 생성합니다.

## 프로젝트 구조

```text
ImageMosaicEditor/
├── ImageMosaicEditor.csproj
├── MainForm.cs
├── MainForm.Designer.cs
├── MainForm.AutoMosaic.cs
├── AutoMosaicEngine.cs
├── AutoMosaicProgressForm.cs
├── AutoMosaicSettings.cs
├── AutoMosaicSettingsDialog.cs
├── installer/
│   └── ImageMosaicEditor.iss
├── python/
│   ├── auto_mosaic_bridge.py
│   └── requirements.txt
└── .github/workflows/release.yml
```

## 출처 및 서드파티

자동 검출 워크플로와 옵션 구성은 `tnisizawa/anime-mosaic`를 참고했습니다.

- `dghs-imgutils`: MIT License
- `Pillow`: Pillow License
- `onnxruntime`: Microsoft 배포 조건 적용
- Inno Setup: 설치형 Release 생성에 사용
