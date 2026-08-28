param(
    [string]$ExePath = (Join-Path $PSScriptRoot '..\bin\Release\net8.0-windows\ImageMosaicEditor.exe'),
    [string]$OutputDir = (Join-Path $PSScriptRoot '..\docs'),
    [string]$Language = 'en'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class ReadmeCaptureNative {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
"@

function Capture-Window([IntPtr]$Hwnd, [string]$Path) {
    $rect = New-Object ReadmeCaptureNative+RECT
    if (-not [ReadmeCaptureNative]::GetWindowRect($Hwnd, [ref]$rect)) { throw 'GetWindowRect failed.' }
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    if ($width -le 0 -or $height -le 0) { throw "Invalid window bounds: ${width}x${height}" }

    $bitmap = New-Object System.Drawing.Bitmap $width, $height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $hdc = $graphics.GetHdc()
    try {
        # PW_RENDERFULLCONTENT (2) renders the target HWND itself instead of copying desktop pixels.
        if (-not [ReadmeCaptureNative]::PrintWindow($Hwnd, $hdc, 2)) { throw 'PrintWindow failed.' }
    } finally {
        $graphics.ReleaseHdc($hdc)
        $graphics.Dispose()
    }
    try { $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png) }
    finally { $bitmap.Dispose() }
    Write-Host "Captured $Path (${width}x${height})"
}

function Find-WindowForProcess([int]$ProcessId, [string]$TitleContains) {
    $script:foundHwnd = [IntPtr]::Zero
    $callback = [ReadmeCaptureNative+EnumWindowsProc]{
        param([IntPtr]$hwnd, [IntPtr]$lparam)
        $pidValue = [uint32]0
        [void][ReadmeCaptureNative]::GetWindowThreadProcessId($hwnd, [ref]$pidValue)
        if ($pidValue -ne $ProcessId -or -not [ReadmeCaptureNative]::IsWindowVisible($hwnd)) { return $true }
        $sb = New-Object System.Text.StringBuilder 512
        [void][ReadmeCaptureNative]::GetWindowText($hwnd, $sb, $sb.Capacity)
        if ($sb.ToString() -like "*$TitleContains*") { $script:foundHwnd = $hwnd; return $false }
        return $true
    }
    [void][ReadmeCaptureNative]::EnumWindows($callback, [IntPtr]::Zero)
    return $script:foundHwnd
}

$settingsDir = Join-Path $env:LOCALAPPDATA 'ImageMosaicEditor'
$settingsPath = Join-Path $settingsDir 'settings.json'
$hadSettings = Test-Path $settingsPath
$oldSettings = if ($hadSettings) { [IO.File]::ReadAllBytes($settingsPath) } else { $null }
$p = $null

try {
    New-Item -ItemType Directory -Force $settingsDir, $OutputDir | Out-Null
    @{ Language = $Language } | ConvertTo-Json | Set-Content -Encoding UTF8 $settingsPath

    Get-Process ImageMosaicEditor -ErrorAction SilentlyContinue | Stop-Process -Force
    $p = Start-Process (Resolve-Path $ExePath) -PassThru
    [void]$p.WaitForInputIdle(10000)
    for ($i=0; $i -lt 100 -and $p.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 100; $p.Refresh() }
    if ($p.MainWindowHandle -eq 0) { throw 'Main window was not created.' }
    Start-Sleep -Milliseconds 800

    Capture-Window $p.MainWindowHandle (Join-Path $OutputDir 'screenshot-main.png')

    # Invoke modal Settings from a helper process so this script can capture the dialog while ShowDialog is active.
    $helper = @"
Add-Type -AssemblyName UIAutomationClient
`$p = Get-Process -Id $($p.Id)
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$p.MainWindowHandle)
`$cond = New-Object System.Windows.Automation.PropertyCondition ([System.Windows.Automation.AutomationElement]::NameProperty), 'Settings(S)'
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (-not `$el) { throw 'Settings(S) menu item not found.' }
`$pattern = `$el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
`$pattern.Invoke()
"@
    Start-Process powershell.exe -ArgumentList '-NoProfile','-STA','-Command',$helper -WindowStyle Hidden | Out-Null

    $dialog = [IntPtr]::Zero
    for ($i=0; $i -lt 100 -and $dialog -eq [IntPtr]::Zero; $i++) {
        Start-Sleep -Milliseconds 100
        $dialog = Find-WindowForProcess $p.Id 'Auto Mosaic Settings'
    }
    if ($dialog -eq [IntPtr]::Zero) { throw 'Settings dialog was not found.' }
    Start-Sleep -Milliseconds 400
    Capture-Window $dialog (Join-Path $OutputDir 'screenshot-settings.png')
    [void][ReadmeCaptureNative]::PostMessage($dialog, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero)
} finally {
    if ($p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
    if ($hadSettings) { [IO.File]::WriteAllBytes($settingsPath, $oldSettings) }
    elseif (Test-Path $settingsPath) { Remove-Item $settingsPath -Force }
}
