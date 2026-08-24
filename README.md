# ImageMosaicEditor

PNG/JPG 이미지를 직접 드래그해 모자이크 처리할 수 있는 .NET 8 Windows Forms 편집기에, [tnisizawa/anime-mosaic](https://github.com/tnisizawa/anime-mosaic)의 자동 검출 워크플로를 통합한 프로젝트입니다.

기존 수동 편집 기능은 그대로 유지하면서 **애니메이션/CG 이미지 자동 검출**, **현재 이미지 자동 처리**, **폴더 일괄 처리**, **mosaic / black / blur 처리 방식**, **imgutils / NTD11 검출기 선택**을 추가했습니다.

> 참고: `anime-mosaic` 저장소의 공개된 동작과 옵션 구성을 기준으로 WinForms용 브리지로 재구현했습니다. 원본 저장소의 Python 소스 파일 자체를 그대로 포함하지 않습니다.

## 기능

| 기능 | 설명 |
|------|------|
| 이미지 열기 | PNG / JPG(JPEG) 파일을 열어 편집 |
| 수동 모자이크 | 마우스 드래그로 영역 선택 후 즉시 픽셀 모자이크 적용 |
| Undo / Redo | `Ctrl+Z`, `Ctrl+Y` 지원 |
| 현재 이미지 자동 처리 | AI 검출 후 현재 편집 이미지에 자동 검열 처리 |
| 폴더 일괄 처리 | JPG/JPEG/PNG/WebP 폴더를 한 번에 처리하고 `<폴더명>_mosaic`에 저장 |
| 처리 방식 | `mosaic`, `black`, `blur` |
| 기본 검출기 | `dghs-imgutils`의 애니메이션/CG 검출 모델 |
| 선택 검출기 | NTD11 YOLO 모델 (`ultralytics` + `.pt` 모델 필요) |
| 추가 검출 | 설정에서 nipple / anus / testicles 검출 선택 |
| 덮어쓰기 저장 | 현재 편집 이미지를 원본 파일에 저장 |

## 요구 사항

- Windows
- .NET 8 Runtime 또는 SDK
- 자동 검출 기능 사용 시 Python 3.10 이상
  - 앱은 가능하면 Python 3.12 → 3.11 → 3.10 → 3.13 순으로 우선 탐색합니다.
- 기본 자동 검출: `dghs-imgutils`, `Pillow`
- NTD11 사용 시 추가로 `ultralytics` 및 NTD11 `.pt` 모델

수동 드래그 모자이크 기능만 사용할 경우 Python은 필요하지 않습니다.

## 빌드 및 실행

```bash
dotnet run
```

Release 빌드:

```bash
dotnet publish ImageMosaicEditor.csproj -c Release -r win-x64 --self-contained false -o publish
```

GitHub Actions는 push 시 Windows x64 publish를 만들고 ZIP Release를 생성합니다. `python/` 브리지 파일도 publish 결과에 함께 포함됩니다.

## 자동 모자이크 사용 방법

1. 앱에서 **자동 모자이크 > Python 의존성 설치**를 한 번 실행합니다.
2. **자동 모자이크 > 설정**에서 검출기, 처리 방식, confidence, padding, 추가 검출 대상을 정합니다.
3. 한 장을 처리하려면 이미지를 열고 **자동 모자이크 > 현재 이미지 자동 처리** 또는 `Ctrl+Shift+A`를 실행합니다.
4. 여러 장을 처리하려면 **자동 모자이크 > 폴더 일괄 처리**를 선택합니다.
5. 일괄 처리 결과는 입력 폴더의 형제 폴더인 `<입력폴더명>_mosaic`에 저장됩니다. 검출되지 않은 파일도 원본 그대로 복사됩니다.

현재 이미지 자동 처리는 저장 전의 수동 편집 상태도 임시 PNG로 전달하므로, 수동 편집과 자동 검출을 섞어 사용할 수 있습니다. 자동 처리 결과도 Undo 스택에 들어가므로 `Ctrl+Z`로 되돌릴 수 있습니다.

## 자동 검출 설정

| 설정 | 기본값 | 설명 |
|------|--------|------|
| Detector | `auto` | 기본은 imgutils, 추가 부위가 필요하고 NTD11 모델이 있으면 NTD11 보조 사용 |
| Mode | `mosaic` | `mosaic`, `black`, `blur` |
| Strength | `20` | 모자이크 블록 크기 또는 블러 반경 |
| Confidence | `0.25` | 낮을수록 검출이 늘지만 오검출 가능성도 증가 |
| Padding | `10` | 검출 박스 외곽에 추가하는 픽셀 여백 |
| Include nipple | 꺼짐 | 유두 검출 추가 |
| Include anus | 꺼짐 | NTD11 우선, 모델이 없으면 imgutils NudeNet 폴백 시도 |
| Include testicles | 꺼짐 | NTD11 모델 필요 |

### Detector 차이

- `auto`: imgutils를 기본으로 사용합니다. anus/testicles가 켜져 있고 NTD11 모델이 있으면 NTD11을 추가로 사용합니다.
- `imgutils`: `dghs-imgutils`만 사용합니다.
- `ntd11`: NTD11 YOLO 모델만 사용합니다. `ultralytics`와 모델 파일이 필수입니다.

## NTD11 선택 기능

1. `ultralytics`를 설치합니다.

```bash
py -3.12 -m pip install ultralytics
```

2. NTD11 모델 파일을 준비합니다. 원본 `anime-mosaic` 프로젝트가 안내하는 모델은 CivitAI의 **Anime NSFW Detection / ADetailer All-in-One** 계열입니다.
3. 모델을 `python/models/ntd11_anime_nsfw_segm_v5.pt`에 두거나, 앱의 자동 모자이크 설정에서 다른 `.pt` 경로를 직접 지정합니다.

모델 파일은 저장소에 포함하지 않습니다. 모델 자체의 사용/재배포 조건은 모델 제공 페이지의 라이선스를 따르세요.

## 프로젝트 구조

```text
ImageMosaicEditor/
├── ImageMosaicEditor.csproj
├── Program.cs
├── MainForm.cs                  # 기존 수동 모자이크/파일 I/O/Undo·Redo
├── MainForm.Designer.cs
├── MainForm.AutoMosaic.cs       # 자동 처리 메뉴 및 WinForms 연동
├── AutoMosaicEngine.cs          # Python 프로세스 실행/JSON 브리지
├── AutoMosaicSettings.cs
├── AutoMosaicSettingsDialog.cs
├── python/
│   ├── auto_mosaic_bridge.py    # 자동 검출 + mosaic/black/blur + 일괄 처리
│   ├── requirements.txt
│   └── models/
│       └── README.md
└── .github/workflows/release.yml
```

## 출처 및 서드파티

자동 검출 워크플로와 옵션 구성은 [tnisizawa/anime-mosaic](https://github.com/tnisizawa/anime-mosaic)를 참고했습니다.

- `dghs-imgutils`: Apache License 2.0. 애니메이션/CG 검출에 사용합니다.
- `Pillow`: 이미지 읽기/처리/저장에 사용합니다.
- `ultralytics`: NTD11 선택 기능 사용 시 필요합니다.
- NTD11 모델: 모델 제공 페이지의 별도 라이선스/이용 조건을 따릅니다.
