using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class WindowHelper
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    public static IDictionary<IntPtr, string> GetOpenWindows()
    {
        IntPtr shellWindow = GetShellWindow();
        Dictionary<IntPtr, string> windows = new Dictionary<IntPtr, string>();

        EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
        {
            if (hWnd == shellWindow) return true;
            if (!IsWindowVisible(hWnd)) return true;

            int length = GetWindowTextLength(hWnd);
            if (length == 0) return true;

            StringBuilder builder = new StringBuilder(length + 1);
            GetWindowText(hWnd, builder, length + 1);

            windows[hWnd] = builder.ToString();
            return true;
        }, IntPtr.Zero);

        return windows;
    }
}