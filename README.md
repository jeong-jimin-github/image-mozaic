# Image Mosaic Editor

Windows용 이미지 모자이크 편집기입니다.

## 주요 기능

- 이미지 파일 열기 및 드래그 앤 드롭
- 폴더 드래그 앤 드롭 및 폴더 전체 자동 처리
- 우측 폴더 이미지 썸네일 목록
- 수동 모자이크 / Undo / Redo
- AI 기반 자동 검열
- 원본 기준 재처리
- 마스크 지우개 모드
- CUDA 사용 가능 시 GPU 우선, CPU fallback
- Portable ZIP / 설치형 EXE Release

## 자동 모자이크

현재 이미지 자동 처리: `Ctrl+Shift+A`

지우개 모드: `Ctrl+E`

선택 모드: `Ctrl+M`

자동 처리 시 이전 처리본 위에 다시 덧씌우지 않고 보존된 원본 이미지에서 새로 처리합니다.

## 배포

GitHub Release에서 두 가지 파일을 제공합니다.

1. `ImageMosaicEditor-Setup-win-x64.exe` — 설치 마법사
2. `ImageMosaicEditor-win-x64-Portable.zip` — 무설치 Portable

두 배포 파일 모두 self-contained .NET 런타임과 Python 자동 검열 환경을 포함합니다.
