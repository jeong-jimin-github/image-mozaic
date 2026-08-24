using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageMosaicEditor;

public partial class MainForm
{
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        try
        {
            Icon? appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (appIcon != null)
                Icon = appIcon;
        }
        catch
        {
            // The executable icon is cosmetic. Never block startup if Windows
            // cannot extract it while running from a development host.
        }
    }
}
