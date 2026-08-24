# ImageMosaicEditor

PNG/JPG 이미지를 직접 드래그해 모자이크 처리할 수 있는 .NET 8 Windows Forms 편집기에, [tnisizawa/anime-mosaic](https://github.com/tnisizawa/anime-mosaic)의 자동 검출 워크플로를 통합한 프로젝트입니다.

기존 수동 편집 기능을 유지하면서 **이미지 import 즉시 자동 검열**, **드래그 앤드롭 import**, **폴더 일괄 처리**, **mosaic / black / blur**, **imgutils / NTD11 검출기 선택**을 지원합니다.

> `anime-mosaic` 저장소의 공개된 동작과 옵션 구성을 기준으로 WinForms용 브리지로 재구현했습니다. 원본 저장소의 Python 소스 파일 자체를 그대로 포함하지 않습니다.

## 기능

| 기능 | 설명 |
|------|------|
| 이미지 열기 | PNG / JPG(JPEG)를 열면 즉시 자동 검열 시작 |
| 드래그 앤드롭 | PNG/JPG/JPEG 파일을 창이나 이미지 영역에 놓으면 즉시 import + 자동 검열 |
| 수동 모자이크 | 마우스 드래그로 영역 선택 후 픽셀 모자이크 적용 |
| Undo / Redo | `Ctrl+Z`, `Ctrl+Y` 지원 |
| 현재 이미지 다시 자동 처리 | `Ctrl+Shift+A`로 현재 편집 상태에 자동 검열 재적용 |
| 폴더 일괄 처리 | JPG/JPEG/PNG/WebP 폴더를 한 번에 처리하고 `<폴더명>_mosaic`에 저장 |
| 처리 방식 | `mosaic`, `black`, `blur` |
| 기본 검출기 | `dghs-imgutils` 애니메이션/CG 검출 모델 |
| 선택 검출기 | NTD11 YOLO 모델 |
| 추가 검출 | 설정에서 nipple / anus / testicles 선택 |
| 덮어쓰기 저장 | 현재 편집 이미지를 원본 파일에 저장 |

## Release ZIP

GitHub Release의 `ImageMosaicEditor-win-x64.zip`은 가능한 한 별도 설치 없이 실행할 수 있도록 다음을 함께 포함합니다.

- Windows x64용 self-contained .NET 8 런타임
- Python 3.12 임베디드 런타임
- `dghs-imgutils==0.19.0`
- `Pillow`
- `ultralytics` 및 그 Python 의존성
- 자동 검출 Python 브리지

따라서 Release ZIP 사용자는 .NET SDK/Runtime이나 Python, pip 패키지를 따로 설치할 필요가 없습니다. 개발 환경에서 `dotnet run`으로 실행할 때만 시스템 Python을 폴백으로 사용할 수 있습니다.

`dghs-imgutils`의 검출 모델 파일은 라이브러리 동작에 따라 최초 사용 시 다운로드될 수 있습니다. NTD11의 `.pt` 모델은 모델 제공자의 재배포 조건 때문에 ZIP에 넣지 않으며, NTD11을 사용할 경우 사용자가 모델 파일만 별도로 지정해야 합니다.

## 사용 방법

### 한 장 import

1. **파일 > 열기** 또는 `Ctrl+O`로 PNG/JPG/JPEG를 선택합니다.
2. 이미지가 표시되면 자동 검열이 즉시 시작됩니다.
3. 검출된 영역은 현재 설정의 `mosaic`, `black`, `blur` 방식으로 처리됩니다.
4. 결과는 Undo 스택에 들어가므로 `Ctrl+Z`로 자동 처리 전 상태로 돌아갈 수 있습니다.

### 드래그 앤드롭

PNG/JPG/JPEG 파일을 프로그램 창이나 이미지 영역으로 끌어 놓으면 바로 import되고 자동 검열이 시작됩니다. 여러 파일을 동시에 놓는 경우 현재 단일 이미지 편집 구조에 맞춰 첫 번째 지원 이미지가 열립니다. 폴더 전체 처리는 **자동 모자이크 > 폴더 일괄 처리**를 사용하세요.

### 폴더 일괄 처리

**자동 모자이크 > 폴더 일괄 처리**에서 입력 폴더를 선택합니다. 결과는 입력 폴더와 같은 위치의 `<입력폴더명>_mosaic` 폴더에 생성되며, 미검출 이미지도 원본 그대로 복사됩니다.

## 자동 검출 설정

| 설정 | 기본값 | 설명 |
|------|--------|------|
| Detector | `auto` | imgutils 기본, 추가 부위가 필요하고 NTD11 모델이 있으면 NTD11 보조 사용 |
| Mode | `mosaic` | `mosaic`, `black`, `blur` |
| Strength | `20` | 모자이크 블록 크기 또는 블러 반경 |
| Confidence | `0.25` | 낮을수록 검출이 늘지만 오검출 가능성도 증가 |
| Padding | `10` | 검출 박스 외곽 여백 픽셀 |
| Include nipple | 꺼짐 | 유두 검출 추가 |
| Include anus | 꺼짐 | NTD11 우선, 없으면 NudeNet 폴백 시도 |
| Include testicles | 꺼짐 | NTD11 모델 필요 |

### Detector

- `auto`: imgutils를 기본 사용하고 필요 시 NTD11을 보조로 사용합니다.
- `imgutils`: `dghs-imgutils`만 사용합니다.
- `ntd11`: NTD11 YOLO 모델을 사용합니다. `ultralytics`는 Release ZIP에 이미 포함되어 있으므로 `.pt` 모델만 지정하면 됩니다.

## NTD11 모델

NTD11을 사용할 경우 원본 `anime-mosaic` 프로젝트가 안내하는 **Anime NSFW Detection / ADetailer All-in-One** 계열 `.pt` 모델을 준비한 뒤:

- `python/models/ntd11_anime_nsfw_segm_v5.pt`에 두거나
- **자동 모자이크 > 설정**에서 임의의 `.pt` 경로를 지정합니다.

모델 파일의 사용/재배포 조건은 모델 제공 페이지의 라이선스를 따르세요.

## 개발 빌드

```bash
dotnet run
```

일반 로컬 publish:

```bash
dotnet publish ImageMosaicEditor.csproj -c Release -r win-x64 --self-contained true -o publish
```

완전한 portable Python 의존성 번들은 GitHub Actions의 Release workflow에서 생성합니다.

## 프로젝트 구조

```text
ImageMosaicEditor/
├── ImageMosaicEditor.csproj
├── Program.cs
├── MainForm.cs
├── MainForm.Designer.cs
├── MainForm.AutoMosaic.cs       # 자동 처리/import/drag&drop 연동
├── AutoMosaicEngine.cs          # 번들 Python 우선 실행 + JSON 브리지
├── AutoMosaicSettings.cs
├── AutoMosaicSettingsDialog.cs
├── python/
│   ├── auto_mosaic_bridge.py
│   ├── requirements.txt
│   └── models/
│       └── README.md
└── .github/workflows/release.yml # self-contained .NET + Python runtime/deps 패키징
```

## 출처 및 서드파티

자동 검출 워크플로와 옵션 구성은 [tnisizawa/anime-mosaic](https://github.com/tnisizawa/anime-mosaic)를 참고했습니다.

- `dghs-imgutils`: MIT License
- `Pillow`: Pillow License
- `ultralytics`: 배포되는 버전의 라이선스 조건을 확인하세요.
- NTD11 모델: 모델 제공 페이지의 별도 라이선스/이용 조건을 따릅니다.
