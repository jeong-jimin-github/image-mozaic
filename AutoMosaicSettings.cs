namespace ImageMosaicEditor;

internal sealed class AutoMosaicSettings
{
    public string Mode { get; set; } = "mosaic";
    public int Strength { get; set; } = 20;
    public double Confidence { get; set; } = 0.25;
    public int Padding { get; set; } = 10;
    public string Detector { get; set; } = "auto";
    public bool IncludeNipple { get; set; }
    public bool IncludeAnus { get; set; }
    public bool IncludeTesticles { get; set; }
    public string Ntd11ModelPath { get; set; } = string.Empty;

    public AutoMosaicSettings Clone() => (AutoMosaicSettings)MemberwiseClone();
}
