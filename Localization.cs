using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace ImageMosaicEditor;

internal sealed record AppLanguage(string Code, string DisplayName);

internal static class L10n
{
    private sealed class UserPreferences
    {
        public string? Language { get; set; }
    }

    public static readonly AppLanguage[] SupportedLanguages =
    [
        new("ko", "한국어"),
        new("en", "English"),
        new("ja", "日本語"),
        new("zh-Hans", "简体中文")
    ];

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new(StringComparer.Ordinal)
        {
            ["이미지 모자이크 편집기"] = "Image Mosaic Editor",
            ["파일(&F)"] = "File(&F)", ["열기(&O)"] = "Open(&O)", ["저장(&S)"] = "Save(&S)", ["종료(&X)"] = "Exit(&X)",
            ["편집(&E)"] = "Edit(&E)", ["되돌리기(&U)"] = "Undo(&U)", ["다시 실행(&R)"] = "Redo(&R)",
            ["자동 모자이크(&A)"] = "Auto Mosaic(&A)", ["현재 이미지 다시 자동 처리(&A)"] = "Reprocess Current Image(&A)", ["폴더 일괄 처리(&B)"] = "Batch Process Folder(&B)",
            ["보기(&V)"] = "View(&V)", ["작업 화면 맞춤"] = "Fit Workspace", ["도구(&T)"] = "Tools(&T)", ["선택 모드"] = "Selection Mode", ["지우개 모드"] = "Eraser Mode",
            ["설정(&S)"] = "Settings(&S)", ["도움말(&H)"] = "Help(&H)",
            ["열기"] = "Open", ["폴더 열기"] = "Open Folder", ["저장"] = "Save", ["다른 이름으로"] = "Save As", ["실행 취소"] = "Undo", ["다시 실행"] = "Redo",
            ["자동 모자이크"] = "Auto Mosaic", ["설정"] = "Settings", ["도움말"] = "Help",
            ["모자이크 선택 모드(&M)"] = "Mosaic Selection Mode(&M)", ["마스크 지우개 모드(&E)"] = "Mask Eraser Mode(&E)", ["지우개 크기"] = "Eraser Size",
            ["이미지 목록"] = "Images", ["폴더를 드래그해 놓으세요."] = "Drop a folder here.", ["또는 파일을 드래그해 주세요."] = "Or drag image files here.", ["▧\n이미지가 없습니다."] = "▧\nNo images",
            ["이미지 또는 폴더를 드래그 해주세요"] = "Drag an image or folder here", ["지원 형식: JPG, PNG, WEBP"] = "Supported: JPG, PNG, WEBP", ["(폴더 드래그시 전체 자동처리됩니다)"] = "(Dropping a folder processes all images automatically)",
            ["이미지를 열어 드래그로 모자이크 영역을 선택하세요."] = "Open an image and drag to select a mosaic area.",
            ["이미지 파일 열기"] = "Open Image", ["이미지 파일 (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|모든 파일 (*.*)|*.*"] = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*",
            ["이미지 파일 (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|모든 파일 (*.*)|*.*"] = "Image files (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|All files (*.*)|*.*",
            ["저장할 이미지가 없습니다."] = "There is no image to save.", ["알림"] = "Notice", ["저장되었습니다."] = "Saved.", ["저장 완료"] = "Save Complete", ["오류"] = "Error",
            ["저장 중 오류가 발생했습니다:\n{0}"] = "An error occurred while saving:\n{0}", ["저장 오류"] = "Save Error", ["다른 이름으로 저장"] = "Save As",
            ["PNG 이미지 (*.png)|*.png|JPEG 이미지 (*.jpg;*.jpeg)|*.jpg;*.jpeg|모든 파일 (*.*)|*.*"] = "PNG image (*.png)|*.png|JPEG image (*.jpg;*.jpeg)|*.jpg;*.jpeg|All files (*.*)|*.*",
            ["자동 모자이크 설정"] = "Auto Mosaic Settings", ["검출 민감도와 처리 방식, 기본 검출 대상을 조정합니다."] = "Adjust detection sensitivity, processing mode, and optional detection targets.",
            ["처리 방식"] = "Effect", ["강도"] = "Strength", ["신뢰도"] = "Confidence", ["검출 여백(px)"] = "Detection padding (px)", ["추가 검출"] = "Extra detection",
            ["유두"] = "Nipples", ["항문"] = "Anus", ["고환/남성 생식기"] = "Testicles / male genitalia",
            ["설정을 변경한 뒤 다시 자동 처리하면 이전 처리본 위에 덧씌우지 않고 원본 이미지에서 새로 처리합니다."] = "Reprocessing after changing settings starts from the original image instead of stacking effects.",
            ["언어"] = "Language", ["Windows 표시 언어를 처음 실행 시 자동 감지합니다."] = "The Windows display language is detected automatically on first launch.", ["확인"] = "OK", ["취소"] = "Cancel",
            ["자동 처리할 이미지 폴더를 선택하세요."] = "Choose a folder of images to process automatically.",
            ["먼저 이미지를 열어주세요."] = "Open an image first.", ["검출된 영역이 없습니다. 이전 자동 처리는 제거하고 원본으로 되돌렸습니다."] = "No target region was detected. The previous automatic processing was removed and the original was restored.",
            ["자동 모자이크 경고"] = "Auto Mosaic Warning", ["자동 모자이크 오류"] = "Auto Mosaic Error", ["자동 모자이크 처리 실패"] = "Auto mosaic failed",
            ["자동 검열 진행"] = "Automatic Detection", ["폴더 전체 자동 검열 진행"] = "Batch Automatic Detection",
            ["AI 검출 단계에서는 모델 실행 동안 진행률이 잠시 같은 위치에 머물 수 있습니다."] = "During AI detection, progress may stay at the same position while the model is running.", ["처리 중..."] = "Processing...", ["처리 완료"] = "Complete",
            ["모드: 선택"] = "Mode: Selection", ["모드: 지우개"] = "Mode: Eraser", ["지우개 크기: {0}px"] = "Eraser: {0}px", ["GPU: 자동 감지"] = "GPU: Auto detect", ["GPU: 사용 가능 (CUDA)"] = "GPU: Available (CUDA)", ["GPU: CPU fallback"] = "GPU: CPU fallback",
            ["마스크 지우개 크기: {0}px"] = "Mask eraser size: {0}px", ["마스크 지우개 모드 - 잘못 처리된 부분을 드래그하면 원본으로 복원됩니다."] = "Mask eraser mode - drag over incorrect areas to restore original pixels.", ["모자이크 선택 모드 - 드래그로 수동 모자이크 영역을 선택하세요."] = "Mosaic selection mode - drag to select a manual mosaic area.",
            ["지울 수 있는 원본 이미지가 없습니다."] = "No original image is available to restore.", ["원본과 작업 이미지 크기가 달라 지우개를 사용할 수 없습니다."] = "The eraser cannot be used because the original and working image sizes differ.", ["마스크 지우개: 원본 픽셀 복원 중..."] = "Mask eraser: restoring original pixels...",
            ["일괄 처리 결과 이미지 - Ctrl+E로 잘못 처리된 마스크를 지울 수 있습니다."] = "Batch result image - use Ctrl+E to erase incorrect mask areas.",
            ["드래그 앤 드롭: 이미지/폴더 가져오기\nCtrl+Shift+A: 현재 이미지를 원본에서 다시 자동 처리\nCtrl+E: 마스크 지우개\nCtrl+M: 수동 모자이크 선택\nCtrl+Z / Ctrl+Y: 실행 취소 / 다시 실행"] = "Drag & drop: import an image/folder\nCtrl+Shift+A: reprocess current image from the original\nCtrl+E: mask eraser\nCtrl+M: manual mosaic selection\nCtrl+Z / Ctrl+Y: undo / redo",
            ["Image Mosaic Editor 도움말"] = "Image Mosaic Editor Help",
            ["{0}개"] = "{0}", ["이미지 목록   0개"] = "Images   0", ["이미지 모자이크 편집기 - {0}"] = "Image Mosaic Editor - {0}", ["저장 완료: {0}"] = "Saved: {0}",
            ["자동 모자이크 설정: {0} / 추가 검출 {1}/3 - 다시 처리하면 원본에서 새로 적용됩니다."] = "Auto mosaic settings: {0} / extra detection {1}/3 - reprocessing starts from the original.",
            ["자동 검출 완료: 검열 대상 영역 없음{0}"] = "Detection complete: no target regions{0}", ["자동 검열 완료: {0}개 영역{1}"] = "Automatic processing complete: {0} region(s){1}",
            ["전체 처리 완료: 처리 {0}, 미검출 {1}, 오류 {2}{3}"] = "Batch complete: processed {0}, undetected {1}, errors {2}{3}",
            ["출력 폴더:\n{0}\n\n처리: {1}개\n미검출: {2}개\n오류: {3}개\n검출 영역: {4}개"] = "Output folder:\n{0}\n\nProcessed: {1}\nUndetected: {2}\nErrors: {3}\nDetected regions: {4}", ["실행 장치: {0}"] = "Runtime device: {0}", ["경고: {0}"] = "Warning: {0}", ["폴더 전체 자동 모자이크 완료"] = "Folder Auto Mosaic Complete",
            ["이미지 가져오기 완료 - 자동 검열을 시작합니다..."] = "Image imported - starting automatic detection...", ["자동 검열 준비 중..."] = "Preparing automatic detection...", ["원본 이미지에서 새로 처리 준비 중..."] = "Preparing to process from the original image...", ["폴더 전체 자동 검열 준비 중..."] = "Preparing folder batch processing...", ["처리할 파일 확인 중..."] = "Checking files to process...",
            ["오류 단계: {0}\n\n"] = "Error stage: {0}\n\n", ["{0}{1}\n\n상세 로그: %LOCALAPPDATA%\\ImageMosaicEditor\\auto-error.log"] = "{0}{1}\n\nDetailed log: %LOCALAPPDATA%\\ImageMosaicEditor\\auto-error.log"
        },
        ["ja"] = new(StringComparer.Ordinal)
        {
            ["이미지 모자이크 편집기"] = "画像モザイクエディター",
            ["파일(&F)"] = "ファイル(&F)", ["열기(&O)"] = "開く(&O)", ["저장(&S)"] = "保存(&S)", ["종료(&X)"] = "終了(&X)", ["편집(&E)"] = "編集(&E)", ["되돌리기(&U)"] = "元に戻す(&U)", ["다시 실행(&R)"] = "やり直す(&R)",
            ["자동 모자이크(&A)"] = "自動モザイク(&A)", ["현재 이미지 다시 자동 처리(&A)"] = "現在の画像を再処理(&A)", ["폴더 일괄 처리(&B)"] = "フォルダー一括処理(&B)", ["보기(&V)"] = "表示(&V)", ["작업 화면 맞춤"] = "作業画面に合わせる", ["도구(&T)"] = "ツール(&T)", ["선택 모드"] = "選択モード", ["지우개 모드"] = "消しゴムモード", ["설정(&S)"] = "設定(&S)", ["도움말(&H)"] = "ヘルプ(&H)",
            ["열기"] = "開く", ["폴더 열기"] = "フォルダーを開く", ["저장"] = "保存", ["다른 이름으로"] = "名前を付けて保存", ["실행 취소"] = "元に戻す", ["다시 실행"] = "やり直す", ["자동 모자이크"] = "自動モザイク", ["설정"] = "設定", ["도움말"] = "ヘルプ",
            ["모자이크 선택 모드(&M)"] = "モザイク選択モード(&M)", ["마스크 지우개 모드(&E)"] = "マスク消しゴムモード(&E)", ["지우개 크기"] = "消しゴムサイズ",
            ["이미지 목록"] = "画像一覧", ["폴더를 드래그해 놓으세요."] = "ここにフォルダーをドロップしてください。", ["또는 파일을 드래그해 주세요."] = "または画像ファイルをドラッグしてください。", ["▧\n이미지가 없습니다."] = "▧\n画像がありません",
            ["이미지 또는 폴더를 드래그 해주세요"] = "画像またはフォルダーをドラッグしてください", ["지원 형식: JPG, PNG, WEBP"] = "対応形式: JPG, PNG, WEBP", ["(폴더 드래그시 전체 자동처리됩니다)"] = "(フォルダーをドロップすると全画像を自動処理します)", ["이미지를 열어 드래그로 모자이크 영역을 선택하세요."] = "画像を開き、ドラッグしてモザイク範囲を選択してください。",
            ["이미지 파일 열기"] = "画像ファイルを開く", ["이미지 파일 (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|모든 파일 (*.*)|*.*"] = "画像ファイル (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|すべてのファイル (*.*)|*.*", ["이미지 파일 (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|모든 파일 (*.*)|*.*"] = "画像ファイル (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|すべてのファイル (*.*)|*.*",
            ["저장할 이미지가 없습니다."] = "保存する画像がありません。", ["알림"] = "お知らせ", ["저장되었습니다."] = "保存しました。", ["저장 완료"] = "保存完了", ["오류"] = "エラー", ["저장 중 오류가 발생했습니다:\n{0}"] = "保存中にエラーが発生しました:\n{0}", ["저장 오류"] = "保存エラー", ["다른 이름으로 저장"] = "名前を付けて保存", ["PNG 이미지 (*.png)|*.png|JPEG 이미지 (*.jpg;*.jpeg)|*.jpg;*.jpeg|모든 파일 (*.*)|*.*"] = "PNG画像 (*.png)|*.png|JPEG画像 (*.jpg;*.jpeg)|*.jpg;*.jpeg|すべてのファイル (*.*)|*.*",
            ["자동 모자이크 설정"] = "自動モザイク設定", ["검출 민감도와 처리 방식, 기본 검출 대상을 조정합니다."] = "検出感度、処理方式、追加の検出対象を調整します。", ["처리 방식"] = "処理方式", ["강도"] = "強度", ["신뢰도"] = "信頼度", ["검출 여백(px)"] = "検出余白 (px)", ["추가 검출"] = "追加検出", ["유두"] = "乳首", ["항문"] = "肛門", ["고환/남성 생식기"] = "睾丸 / 男性器", ["설정을 변경한 뒤 다시 자동 처리하면 이전 처리본 위에 덧씌우지 않고 원본 이미지에서 새로 처리합니다."] = "設定変更後の再処理は、前回の処理に重ねず元画像からやり直します。", ["언어"] = "言語", ["Windows 표시 언어를 처음 실행 시 자동 감지합니다."] = "初回起動時にWindowsの表示言語を自動検出します。", ["확인"] = "OK", ["취소"] = "キャンセル",
            ["자동 처리할 이미지 폴더를 선택하세요."] = "自動処理する画像フォルダーを選択してください。", ["먼저 이미지를 열어주세요."] = "先に画像を開いてください。", ["검출된 영역이 없습니다. 이전 자동 처리는 제거하고 원본으로 되돌렸습니다."] = "対象領域は検出されませんでした。以前の自動処理を削除し、元画像に戻しました。", ["자동 모자이크 경고"] = "自動モザイク警告", ["자동 모자이크 오류"] = "自動モザイクエラー", ["자동 모자이크 처리 실패"] = "自動モザイク処理に失敗しました", ["자동 검열 진행"] = "自動検出中", ["폴더 전체 자동 검열 진행"] = "フォルダー一括自動検出", ["AI 검출 단계에서는 모델 실행 동안 진행률이 잠시 같은 위치에 머물 수 있습니다."] = "AI検出中はモデル実行のため進捗表示がしばらく止まる場合があります。", ["처리 중..."] = "処理中...", ["처리 완료"] = "処理完了",
            ["모드: 선택"] = "モード: 選択", ["모드: 지우개"] = "モード: 消しゴム", ["지우개 크기: {0}px"] = "消しゴム: {0}px", ["GPU: 자동 감지"] = "GPU: 自動検出", ["GPU: 사용 가능 (CUDA)"] = "GPU: 利用可能 (CUDA)", ["GPU: CPU fallback"] = "GPU: CPUフォールバック", ["마스크 지우개 크기: {0}px"] = "マスク消しゴムサイズ: {0}px", ["마스크 지우개 모드 - 잘못 처리된 부분을 드래그하면 원본으로 복원됩니다."] = "マスク消しゴムモード - 誤処理部分をドラッグすると元画像に復元します。", ["모자이크 선택 모드 - 드래그로 수동 모자이크 영역을 선택하세요."] = "モザイク選択モード - ドラッグして手動モザイク範囲を選択してください。", ["지울 수 있는 원본 이미지가 없습니다."] = "復元できる元画像がありません。", ["원본과 작업 이미지 크기가 달라 지우개를 사용할 수 없습니다."] = "元画像と作業画像のサイズが異なるため消しゴムを使用できません。", ["마스크 지우개: 원본 픽셀 복원 중..."] = "マスク消しゴム: 元のピクセルを復元中...", ["일괄 처리 결과 이미지 - Ctrl+E로 잘못 처리된 마스크를 지울 수 있습니다."] = "一括処理結果 - Ctrl+Eで誤ったマスク部分を消せます。",
            ["드래그 앤 드롭: 이미지/폴더 가져오기\nCtrl+Shift+A: 현재 이미지를 원본에서 다시 자동 처리\nCtrl+E: 마스크 지우개\nCtrl+M: 수동 모자이크 선택\nCtrl+Z / Ctrl+Y: 실행 취소 / 다시 실행"] = "ドラッグ＆ドロップ: 画像/フォルダーを読み込み\nCtrl+Shift+A: 元画像から現在の画像を再処理\nCtrl+E: マスク消しゴム\nCtrl+M: 手動モザイク選択\nCtrl+Z / Ctrl+Y: 元に戻す / やり直す", ["Image Mosaic Editor 도움말"] = "Image Mosaic Editor ヘルプ",
            ["{0}개"] = "{0}件", ["이미지 목록   0개"] = "画像一覧   0件", ["이미지 모자이크 편집기 - {0}"] = "画像モザイクエディター - {0}", ["저장 완료: {0}"] = "保存完了: {0}", ["자동 모자이크 설정: {0} / 추가 검출 {1}/3 - 다시 처리하면 원본에서 새로 적용됩니다."] = "自動モザイク設定: {0} / 追加検出 {1}/3 - 再処理は元画像から適用します。", ["자동 검출 완료: 검열 대상 영역 없음{0}"] = "検出完了: 対象領域なし{0}", ["자동 검열 완료: {0}개 영역{1}"] = "自動処理完了: {0}領域{1}", ["전체 처리 완료: 처리 {0}, 미검출 {1}, 오류 {2}{3}"] = "一括処理完了: 処理 {0}, 未検出 {1}, エラー {2}{3}", ["출력 폴더:\n{0}\n\n처리: {1}개\n미검출: {2}개\n오류: {3}개\n검출 영역: {4}개"] = "出力フォルダー:\n{0}\n\n処理: {1}\n未検出: {2}\nエラー: {3}\n検出領域: {4}", ["실행 장치: {0}"] = "実行デバイス: {0}", ["경고: {0}"] = "警告: {0}", ["폴더 전체 자동 모자이크 완료"] = "フォルダー自動モザイク完了", ["이미지 가져오기 완료 - 자동 검열을 시작합니다..."] = "画像を読み込みました - 自動検出を開始します...", ["자동 검열 준비 중..."] = "自動検出を準備中...", ["원본 이미지에서 새로 처리 준비 중..."] = "元画像からの再処理を準備中...", ["폴더 전체 자동 검열 준비 중..."] = "フォルダー一括処理を準備中...", ["처리할 파일 확인 중..."] = "処理対象ファイルを確認中...", ["오류 단계: {0}\n\n"] = "エラー段階: {0}\n\n", ["{0}{1}\n\n상세 로그: %LOCALAPPDATA%\\ImageMosaicEditor\\auto-error.log"] = "{0}{1}\n\n詳細ログ: %LOCALAPPDATA%\\ImageMosaicEditor\\auto-error.log"
        },
        ["zh-Hans"] = new(StringComparer.Ordinal)
        {
            ["이미지 모자이크 편집기"] = "图像马赛克编辑器", ["파일(&F)"] = "文件(&F)", ["열기(&O)"] = "打开(&O)", ["저장(&S)"] = "保存(&S)", ["종료(&X)"] = "退出(&X)", ["편집(&E)"] = "编辑(&E)", ["되돌리기(&U)"] = "撤销(&U)", ["다시 실행(&R)"] = "重做(&R)", ["자동 모자이크(&A)"] = "自动马赛克(&A)", ["현재 이미지 다시 자동 처리(&A)"] = "重新处理当前图像(&A)", ["폴더 일괄 처리(&B)"] = "批量处理文件夹(&B)", ["보기(&V)"] = "视图(&V)", ["작업 화면 맞춤"] = "适应工作区", ["도구(&T)"] = "工具(&T)", ["선택 모드"] = "选择模式", ["지우개 모드"] = "橡皮擦模式", ["설정(&S)"] = "设置(&S)", ["도움말(&H)"] = "帮助(&H)",
            ["열기"] = "打开", ["폴더 열기"] = "打开文件夹", ["저장"] = "保存", ["다른 이름으로"] = "另存为", ["실행 취소"] = "撤销", ["다시 실행"] = "重做", ["자동 모자이크"] = "自动马赛克", ["설정"] = "设置", ["도움말"] = "帮助", ["모자이크 선택 모드(&M)"] = "马赛克选择模式(&M)", ["마스크 지우개 모드(&E)"] = "蒙版橡皮擦模式(&E)", ["지우개 크기"] = "橡皮擦大小",
            ["이미지 목록"] = "图像列表", ["폴더를 드래그해 놓으세요."] = "将文件夹拖放到这里。", ["또는 파일을 드래그해 주세요."] = "或将图像文件拖到这里。", ["▧\n이미지가 없습니다."] = "▧\n没有图像", ["이미지 또는 폴더를 드래그 해주세요"] = "请拖入图像或文件夹", ["지원 형식: JPG, PNG, WEBP"] = "支持格式: JPG, PNG, WEBP", ["(폴더 드래그시 전체 자동처리됩니다)"] = "(拖入文件夹后会自动处理全部图像)", ["이미지를 열어 드래그로 모자이크 영역을 선택하세요."] = "打开图像并拖动选择马赛克区域。",
            ["이미지 파일 열기"] = "打开图像", ["이미지 파일 (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|모든 파일 (*.*)|*.*"] = "图像文件 (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|所有文件 (*.*)|*.*", ["이미지 파일 (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|모든 파일 (*.*)|*.*"] = "图像文件 (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|所有文件 (*.*)|*.*", ["저장할 이미지가 없습니다."] = "没有可保存的图像。", ["알림"] = "提示", ["저장되었습니다."] = "已保存。", ["저장 완료"] = "保存完成", ["오류"] = "错误", ["저장 중 오류가 발생했습니다:\n{0}"] = "保存时发生错误:\n{0}", ["저장 오류"] = "保存错误", ["다른 이름으로 저장"] = "另存为", ["PNG 이미지 (*.png)|*.png|JPEG 이미지 (*.jpg;*.jpeg)|*.jpg;*.jpeg|모든 파일 (*.*)|*.*"] = "PNG 图像 (*.png)|*.png|JPEG 图像 (*.jpg;*.jpeg)|*.jpg;*.jpeg|所有文件 (*.*)|*.*",
            ["자동 모자이크 설정"] = "自动马赛克设置", ["검출 민감도와 처리 방식, 기본 검출 대상을 조정합니다."] = "调整检测灵敏度、处理方式和附加检测目标。", ["처리 방식"] = "处理方式", ["강도"] = "强度", ["신뢰도"] = "置信度", ["검출 여백(px)"] = "检测边距 (px)", ["추가 검출"] = "附加检测", ["유두"] = "乳头", ["항문"] = "肛门", ["고환/남성 생식기"] = "睾丸 / 男性生殖器", ["설정을 변경한 뒤 다시 자동 처리하면 이전 처리본 위에 덧씌우지 않고 원본 이미지에서 새로 처리합니다."] = "更改设置后重新处理时，会从原图重新开始，不会叠加之前的效果。", ["언어"] = "语言", ["Windows 표시 언어를 처음 실행 시 자동 감지합니다."] = "首次启动时会自动检测 Windows 显示语言。", ["확인"] = "确定", ["취소"] = "取消",
            ["자동 처리할 이미지 폴더를 선택하세요."] = "请选择要自动处理的图像文件夹。", ["먼저 이미지를 열어주세요."] = "请先打开图像。", ["검출된 영역이 없습니다. 이전 자동 처리는 제거하고 원본으로 되돌렸습니다."] = "未检测到目标区域。已移除之前的自动处理并恢复原图。", ["자동 모자이크 경고"] = "自动马赛克警告", ["자동 모자이크 오류"] = "自动马赛克错误", ["자동 모자이크 처리 실패"] = "自动马赛克处理失败", ["자동 검열 진행"] = "自动检测进行中", ["폴더 전체 자동 검열 진행"] = "文件夹批量自动检测", ["AI 검출 단계에서는 모델 실행 동안 진행률이 잠시 같은 위치에 머물 수 있습니다."] = "AI 检测阶段运行模型时，进度可能会暂时停留在同一位置。", ["처리 중..."] = "处理中...", ["처리 완료"] = "处理完成",
            ["모드: 선택"] = "模式: 选择", ["모드: 지우개"] = "模式: 橡皮擦", ["지우개 크기: {0}px"] = "橡皮擦: {0}px", ["GPU: 자동 감지"] = "GPU: 自动检测", ["GPU: 사용 가능 (CUDA)"] = "GPU: 可用 (CUDA)", ["GPU: CPU fallback"] = "GPU: CPU 回退", ["마스크 지우개 크기: {0}px"] = "蒙版橡皮擦大小: {0}px", ["마스크 지우개 모드 - 잘못 처리된 부분을 드래그하면 원본으로 복원됩니다."] = "蒙版橡皮擦模式 - 拖动错误处理区域可恢复原始像素。", ["모자이크 선택 모드 - 드래그로 수동 모자이크 영역을 선택하세요."] = "马赛克选择模式 - 拖动选择手动马赛克区域。", ["지울 수 있는 원본 이미지가 없습니다."] = "没有可用于恢复的原始图像。", ["원본과 작업 이미지 크기가 달라 지우개를 사용할 수 없습니다."] = "原图与工作图像尺寸不同，无法使用橡皮擦。", ["마스크 지우개: 원본 픽셀 복원 중..."] = "蒙版橡皮擦: 正在恢复原始像素...", ["일괄 처리 결과 이미지 - Ctrl+E로 잘못 처리된 마스크를 지울 수 있습니다."] = "批处理结果图像 - 可用 Ctrl+E 擦除错误蒙版区域。",
            ["드래그 앤 드롭: 이미지/폴더 가져오기\nCtrl+Shift+A: 현재 이미지를 원본에서 다시 자동 처리\nCtrl+E: 마스크 지우개\nCtrl+M: 수동 모자이크 선택\nCtrl+Z / Ctrl+Y: 실행 취소 / 다시 실행"] = "拖放: 导入图像/文件夹\nCtrl+Shift+A: 从原图重新处理当前图像\nCtrl+E: 蒙版橡皮擦\nCtrl+M: 手动马赛克选择\nCtrl+Z / Ctrl+Y: 撤销 / 重做", ["Image Mosaic Editor 도움말"] = "Image Mosaic Editor 帮助",
            ["{0}개"] = "{0}", ["이미지 목록   0개"] = "图像列表   0", ["이미지 모자이크 편집기 - {0}"] = "图像马赛克编辑器 - {0}", ["저장 완료: {0}"] = "已保存: {0}", ["자동 모자이크 설정: {0} / 추가 검출 {1}/3 - 다시 처리하면 원본에서 새로 적용됩니다."] = "自动马赛克设置: {0} / 附加检测 {1}/3 - 重新处理会从原图开始。", ["자동 검출 완료: 검열 대상 영역 없음{0}"] = "检测完成: 无目标区域{0}", ["자동 검열 완료: {0}개 영역{1}"] = "自动处理完成: {0} 个区域{1}", ["전체 처리 완료: 처리 {0}, 미검출 {1}, 오류 {2}{3}"] = "批处理完成: 已处理 {0}, 未检测 {1}, 错误 {2}{3}", ["출력 폴더:\n{0}\n\n처리: {1}개\n미검출: {2}개\n오류: {3}개\n검출 영역: {4}개"] = "输出文件夹:\n{0}\n\n已处理: {1}\n未检测: {2}\n错误: {3}\n检测区域: {4}", ["실행 장치: {0}"] = "运行设备: {0}", ["경고: {0}"] = "警告: {0}", ["폴더 전체 자동 모자이크 완료"] = "文件夹自动马赛克完成", ["이미지 가져오기 완료 - 자동 검열을 시작합니다..."] = "图像已导入 - 开始自动检测...", ["자동 검열 준비 중..."] = "正在准备自动检测...", ["원본 이미지에서 새로 처리 준비 중..."] = "正在准备从原图重新处理...", ["폴더 전체 자동 검열 준비 중..."] = "正在准备文件夹批处理...", ["처리할 파일 확인 중..."] = "正在检查待处理文件...", ["오류 단계: {0}\n\n"] = "错误阶段: {0}\n\n", ["{0}{1}\n\n상세 로그: %LOCALAPPDATA%\\ImageMosaicEditor\\auto-error.log"] = "{0}{1}\n\n详细日志: %LOCALAPPDATA%\\ImageMosaicEditor\\auto-error.log"
        }
    };

    private static readonly string SettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImageMosaicEditor");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");
    private static string? _currentLanguage;

    public static string CurrentLanguage => _currentLanguage ??= LoadLanguage();

    public static string T(string koreanSource)
    {
        string language = CurrentLanguage;
        if (language == "ko") return koreanSource;
        return Translations.TryGetValue(language, out var table) && table.TryGetValue(koreanSource, out string? translated)
            ? translated
            : koreanSource;
    }

    public static string F(string koreanFormat, params object?[] args) => string.Format(CultureInfo.CurrentCulture, T(koreanFormat), args);

    public static void SetLanguage(string language)
    {
        string normalized = NormalizeLanguage(language);
        _currentLanguage = normalized;
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            string json = JsonSerializer.Serialize(new UserPreferences { Language = normalized }, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // A read-only profile must not prevent the application from running.
        }
    }

    private static string LoadLanguage()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var prefs = JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(SettingsPath));
                if (!string.IsNullOrWhiteSpace(prefs?.Language))
                    return NormalizeLanguage(prefs.Language);
            }
        }
        catch
        {
            // Fall through to locale detection if settings are unavailable/corrupt.
        }

        return DetectSystemLanguage();
    }

    private static string DetectSystemLanguage()
    {
        string name = CultureInfo.CurrentUICulture.Name;
        string twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (twoLetter.Equals("ko", StringComparison.OrdinalIgnoreCase)) return "ko";
        if (twoLetter.Equals("ja", StringComparison.OrdinalIgnoreCase)) return "ja";
        if (twoLetter.Equals("zh", StringComparison.OrdinalIgnoreCase)) return "zh-Hans";
        return "en";
    }

    private static string NormalizeLanguage(string language)
    {
        if (language.StartsWith("ko", StringComparison.OrdinalIgnoreCase)) return "ko";
        if (language.StartsWith("ja", StringComparison.OrdinalIgnoreCase)) return "ja";
        if (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh-Hans";
        return "en";
    }
}
