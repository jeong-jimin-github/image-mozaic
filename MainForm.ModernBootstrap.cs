using System;

namespace ImageMosaicEditor;

public partial class MainForm
{
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        InitializeModernUi();
        ApplyRibbonIconFixes();
        StartModernStatusSync();
    }
}
