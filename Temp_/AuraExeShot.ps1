param(
    [string]$Action = "shot",
    [int]$InX = 0,
    [int]$InY = 0,
    [string]$OutFile = "Temp_/exe_1.png"
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class AuraWin {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, int e);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
"@ -ErrorAction Stop

$root = "C:\Users\leona\OneDrive\Desktop\VIDEOJUEGOS\RPG-Unity"
$exe  = Join-Path $root "Builds\Windows\RPG.exe"
$full = Join-Path $root $OutFile

function Get-GameProc {
    return Get-Process -Name "RPG" -ErrorAction SilentlyContinue | Select-Object -First 1
}

if ($Action -eq "launch") {
    Get-Process -Name "RPG" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
    Start-Process -FilePath $exe -WorkingDirectory (Join-Path $root "Builds\Windows") | Out-Null
    Start-Sleep -Seconds 10
}

$p = Get-GameProc
if ($p -eq $null) { Write-Output "NO_PROCESS"; exit 1 }

$h = $p.MainWindowHandle
if ($h -eq 0) { Start-Sleep -Seconds 3; $p.Refresh(); $h = $p.MainWindowHandle }
if ($h -eq 0) { Write-Output "NO_WINDOW"; exit 1 }

[void][AuraWin]::SetForegroundWindow($h)
Start-Sleep -Milliseconds 500

$r = New-Object AuraWin+RECT
[void][AuraWin]::GetWindowRect($h, [ref]$r)
$w = $r.Right - $r.Left
$hgt = $r.Bottom - $r.Top

if ($Action -eq "click") {
    if ($InX -le 0) { $InX = [int]($r.Left + $w / 2) }
    if ($InY -le 0) { $InY = [int]($r.Top + $hgt / 2) }
    [void][AuraWin]::SetCursorPos($InX, $InY)
    Start-Sleep -Milliseconds 250
    [AuraWin]::mouse_event(0x0002, 0, 0, 0, 0)
    Start-Sleep -Milliseconds 80
    [AuraWin]::mouse_event(0x0004, 0, 0, 0, 0)
    Start-Sleep -Seconds 4
}

$screen = [System.Windows.Forms.SystemInformation]::VirtualScreen
$bmp = New-Object System.Drawing.Bitmap $screen.Width, $screen.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($screen.Left, $screen.Top, 0, 0, $bmp.Size)
$g.Dispose()
$bmp.Save($full, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Write-Output ("WINDOW left=" + $r.Left + " top=" + $r.Top + " w=" + $w + " h=" + $hgt + " click=" + $InX + "," + $InY + " file=" + $full)
