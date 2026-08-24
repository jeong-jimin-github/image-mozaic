namespace ImageMosaicEditor;

internal sealed class AutoMosaicSettings
{
    public string Mode { get; set; } = "mosaic";
    public int Strength { get; set; } = 20;
    public double Confidence { get; set; } = 0.25;
    public int Padding { get; set; } = 10;

    // Additional detection is enabled by default for all supported targets.
    public bool IncludeNipple { get; set; } = true;
    public bool IncludeAnus { get; set; } = true;
    public bool IncludeTesticles { get; set; } = true;

    public AutoMosaicSettings Clone() => (AutoMosaicSettings)MemberwiseClone();
}
