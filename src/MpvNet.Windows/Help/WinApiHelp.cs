﻿
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

using static MpvNet.Windows.Native.WinApi;

namespace MpvNet.Windows.Help;

public class WinApiHelp
{
    public static Version WindowsTen1607 { get; } = new Version(10, 0, 14393); // Windows 10 1607

    public static int GetResizeBorder(int v)
    {
        switch (v)
        {
            case 1 /* WMSZ_LEFT        */ : return 3;
            case 3 /* WMSZ_TOP         */ : return 2;
            case 2 /* WMSZ_RIGHT       */ : return 3;
            case 6 /* WMSZ_BOTTOM      */ : return 2;
            case 4 /* WMSZ_TOPLEFT     */ : return 1;
            case 5 /* WMSZ_TOPRIGHT    */ : return 1;
            case 7 /* WMSZ_BOTTOMLEFT  */ : return 3;
            case 8 /* WMSZ_BOTTOMRIGHT */ : return 3;
            default: return -1;
        }
    }

    public static void SubtractWindowBorders(IntPtr hwnd, ref Rect rc, int dpi)
    {
        Rect r = new Rect(0, 0, 0, 0);
        AddWindowBorders(hwnd, ref r, dpi);
        rc.Left -= r.Left;
        rc.Top -= r.Top;
        rc.Right -= r.Right;
        rc.Bottom -= r.Bottom;
    }

    public static void AddWindowBorders(IntPtr hwnd, ref Rect rc, int dpi)
    {
        uint windowStyle = (uint)GetWindowLong(hwnd, -16); // GWL_STYLE
        uint windowStyleEx = (uint)GetWindowLong(hwnd, -20); // GWL_EXSTYLE

        if (Environment.OSVersion.Version >= WindowsTen1607)
            AdjustWindowRectExForDpi(ref rc, windowStyle, false, windowStyleEx, (uint)dpi);
        else
            AdjustWindowRect(ref rc, windowStyle, false);
    }

    public static Rectangle GetWorkingArea(IntPtr handle, Rectangle workingArea)
    {
        if (handle != IntPtr.Zero && GetDwmWindowRect(handle, out Rect dwmRect) &&
            GetWindowRect(handle, out Rect rect))
        {
            int left = workingArea.Left;
            int top = workingArea.Top;
            int right = workingArea.Right;
            int bottom = workingArea.Bottom;

            left += rect.Left - dwmRect.Left;
            top -= rect.Top - dwmRect.Top;
            right -= dwmRect.Right - rect.Right;
            bottom -= dwmRect.Bottom - rect.Bottom;

            return new Rectangle(left, top, right - left, bottom - top);
        }

        return workingArea;
    }

    public static bool GetDwmWindowRect(IntPtr handle, out Rect rect)
    {
        const uint DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        return 0 == DwmGetWindowAttribute(handle, DWMWA_EXTENDED_FRAME_BOUNDS,
            out rect, (uint)Marshal.SizeOf<Rect>());
    }

    public static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
            return GetWindowLongPtr(hWnd, nIndex);
        else
            return GetWindowLong32(hWnd, nIndex);
    }

    public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr(hWnd, nIndex, dwNewLong);
        else
            return SetWindowLong32(hWnd, nIndex, dwNewLong);
    }

    public static string GetAppPathForExtension(params string[] extensions)
    {
        foreach (string it in extensions)
        {
            string extension = it;

            if (!extension.StartsWith("."))
                extension = "." + extension;

            uint c = 0U;

            if (AssocQueryString(0x40, 2, extension, null, null, ref c) == 1)
            {
                if (c > 0L)
                {
                    var sb = new StringBuilder((int)c);

                    if (0 == AssocQueryString(0x40, 2, extension, default, sb, ref c))
                    {
                        string ret = sb.ToString();

                        if (File.Exists(ret))
                            return ret;
                    }
                }
            }
        }

        return "";
    }
}
