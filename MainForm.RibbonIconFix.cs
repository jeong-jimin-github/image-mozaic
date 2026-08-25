namespace ImageMosaicEditor;

public partial class MainForm
{
    private void ApplyRibbonIconFixes()
    {
        // The ribbon now renders its final icons directly. Keeping this bootstrap
        // hook as a no-op avoids the old overlay controls from stacking on top of
        // the redesigned buttons.
    }
}
