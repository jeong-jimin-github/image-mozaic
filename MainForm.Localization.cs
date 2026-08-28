using System.IO;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    private void ApplyLocalization()
    {
        fileMenu.Text = L10n.T("파일(&F)");
        menuOpen.Text = L10n.T("열기(&O)");
        menuSave.Text = L10n.T("저장(&S)");
        menuExit.Text = L10n.T("종료(&X)");
        editMenu.Text = L10n.T("편집(&E)");
        menuUndo.Text = L10n.T("되돌리기(&U)");
        menuRedo.Text = L10n.T("다시 실행(&R)");

        SetMenuText("autoMosaicMenu", "자동 모자이크(&A)");
        SetMenuText("autoCurrentMenu", "현재 이미지 다시 자동 처리(&A)");
        SetMenuText("autoBatchMenu", "폴더 일괄 처리(&B)");
        SetMenuText("autoSettingsMenu", "설정(&S)");
        SetMenuText("eraserSelectionMenu", "모자이크 선택 모드(&M)");
        SetMenuText("eraserModeMenu", "마스크 지우개 모드(&E)");
        SetMenuText("eraserSizeMenu", "지우개 크기");
        SetMenuText("modernViewMenu", "보기(&V)");
        SetMenuText("modernResetViewMenu", "작업 화면 맞춤");
        SetMenuText("modernToolsMenu", "도구(&T)");
        SetMenuText("modernSelectMenu", "선택 모드");
        SetMenuText("modernEraseMenu", "지우개 모드");
        SetMenuText("modernSettingsMenu", "설정(&S)");
        SetMenuText("modernHelpMenu", "도움말(&H)");

        SetControlText("ribbonOpen", "열기");
        SetControlText("ribbonFolder", "폴더 열기");
        SetControlText("ribbonSave", "저장");
        SetControlText("ribbonSaveAs", "다른 이름으로");
        SetControlText("ribbonUndo", "실행 취소");
        SetControlText("ribbonRedo", "다시 실행");
        SetControlText("ribbonAuto", "자동 모자이크");
        SetControlText("ribbonSelect", "선택 모드");
        SetControlText("ribbonEraser", "지우개 모드");
        SetControlText("ribbonSettings", "설정");
        SetControlText("ribbonHelp", "도움말");
        LocalizeRibbonButtons();
        SetControlText("folderDropTitle", "폴더를 드래그해 놓으세요.");
        SetControlText("folderDropSub", "또는 파일을 드래그해 주세요.");

        if (_folderHeader != null) _folderHeader.Text = L10n.T("이미지 목록");
        if (_folderCountBadge != null && _folderList != null) _folderCountBadge.Text = L10n.F("{0}개", _folderList.Items.Count);
        if (_folderEmptyHint != null) _folderEmptyHint.Text = L10n.T("▧\n이미지가 없습니다.");

        Text = string.IsNullOrWhiteSpace(_currentFilePath)
            ? L10n.T("이미지 모자이크 편집기")
            : L10n.F("이미지 모자이크 편집기 - {0}", Path.GetFileName(_currentFilePath));

        RefreshModernStatusDetails();
        UpdateGpuStatus(null);
        pictureBox.Invalidate();
    }

    private void LocalizeRibbonButtons()
    {
        if (_modernRibbon == null) return;
        LocalizeRibbonControls(_modernRibbon);
        _eraserSizePicker?.Invalidate();
    }

    private static void LocalizeRibbonControls(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is ModernRibbonButton button)
            {
                string source = button.IconKind switch
                {
                    ModernIcon.Open => "열기",
                    ModernIcon.Folder => "폴더 열기",
                    ModernIcon.Save => "저장",
                    ModernIcon.SaveAs => "다른 이름으로",
                    ModernIcon.Undo => "실행 취소",
                    ModernIcon.Redo => "다시 실행",
                    ModernIcon.Magic => "자동 모자이크",
                    ModernIcon.Pointer => "선택 모드",
                    ModernIcon.Eraser => "지우개 모드",
                    ModernIcon.Settings => "설정",
                    ModernIcon.Help => "도움말",
                    _ => button.Text
                };
                button.Text = L10n.T(source);
            }
            if (control.HasChildren) LocalizeRibbonControls(control);
        }
    }

    private void SetMenuText(string name, string source)
    {
        ToolStripItem[] items = menuStrip.Items.Find(name, true);
        if (items.Length > 0) items[0].Text = L10n.T(source);
    }

    private void SetControlText(string name, string source)
    {
        Control[] controls = Controls.Find(name, true);
        if (controls.Length > 0) controls[0].Text = L10n.T(source);
    }
}
