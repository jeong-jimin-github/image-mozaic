# ImageMosaicEditor

PNG 또는 JPG 이미지를 불러와 원하는 영역에 **모자이크 처리**를 한 뒤 동일한 파일로 덮어쓰기 저장하는 C# Windows Forms GUI 프로그램입니다.

## 기능

| 기능 | 설명 |
|------|------|
| 이미지 열기 | PNG / JPG(JPEG) 파일을 열어 PictureBox에 표시 |
| 모자이크 선택 | 마우스 드래그로 원하는 영역 선택 → 자동으로 모자이크 적용 |
| 덮어쓰기 저장 | 원본 파일 경로에 수정된 이미지를 그대로 저장 |
| 단축키 | <kbd>Ctrl</kbd>+<kbd>O</kbd> 열기 / <kbd>Ctrl</kbd>+<kbd>S</kbd> 저장 |

## 실행 방법

### 요구 사항
- .NET 8 SDK (Windows)

### 빌드 및 실행

```bash
cd ImageMosaicEditor
dotnet run
```

또는 Release 빌드:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## 사용 방법

1. **파일(F) > 열기(O)** 또는 `Ctrl+O` 로 PNG/JPG 파일을 엽니다.
2. PictureBox 위에서 **마우스 왼쪽 버튼을 누른 채 드래그**하여 모자이크를 적용할 영역을 선택합니다.
3. 버튼을 놓으면 해당 영역에 자동으로 모자이크가 적용됩니다.
4. 여러 영역에 반복 적용할 수 있습니다.
5. **파일(F) > 저장(S)** 또는 `Ctrl+S` 로 원본 파일에 덮어쓰기 저장합니다.

## 프로젝트 구조

```
ImageMosaicEditor/
├── ImageMosaicEditor.csproj   # 프로젝트 파일 (.NET 8 Windows Forms)
├── Program.cs                 # 진입점
├── MainForm.cs                # 메인 로직 (모자이크, 마우스 이벤트, 파일 I/O)
├── MainForm.Designer.cs       # UI 레이아웃 (디자이너)
├── app.manifest               # Windows 앱 매니페스트
└── .gitignore
```
