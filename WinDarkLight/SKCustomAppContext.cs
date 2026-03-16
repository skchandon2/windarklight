using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Security.Permissions;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WinDarkLight
{
    public struct COPYDATASTRUCT
    {
        public int cbData;
        public IntPtr dwData;
        [MarshalAs(UnmanagedType.LPStr)] public string lpData;
    }
    internal class SKCustomAppContext : ApplicationContext
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        private NotifyIcon _trayIcon;
        private bool _isDark;

        public SKCustomAppContext()
        {
            _isDark = false;
            
            //get the current mode
            var isCurrentSystemInLightMode = GetRegistryValue("SystemUsesLightTheme");

            //use the system preference as our indicator
            if (isCurrentSystemInLightMode == 0)
            {
                _isDark = true;
            }

            //determine the correct icon to display. when dark show light-icon. when light, show dark-icon
            Icon notifyIcon = GetApplicableIcon(_isDark);

            //create tray icon
            _trayIcon = new NotifyIcon()
            {
                
                Icon = notifyIcon,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items = { 
                        new ToolStripMenuItem("Exit", null, ExitClickHandler) 
                        , new ToolStripMenuItem("About", null, AboutClickHandler)
                    }
                },
                Visible = true
            };

            //attach even handler for changing
            _trayIcon.Click += new System.EventHandler(FlipDarkOrLightTheme);

        }

        private void FlipDarkOrLightTheme(object? sender, System.EventArgs e)
        {
            //Console.WriteLine("Flipping");
            if (_isDark)
            {
                //flip the flag
                _isDark = !_isDark;

                //switch to light
                SwitchToLightMode();

                //change icon to dark
                _trayIcon.Icon = GetApplicableIcon(_isDark);

            }
            else
            {
                //flip the flag
                _isDark = !_isDark;

                //switch to dark
                SwitchToDarkMode();

                //change icon to light                
                _trayIcon.Icon = GetApplicableIcon(_isDark);
            }
        }

        void ExitClickHandler(object? sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            Application.Exit();
        }

        void AboutClickHandler(object? sender, EventArgs e)
        {
            Form1 aboutForm = new Form1();
            aboutForm.ShowDialog();
        }

        private int GetRegistryValue(string valueName)
        {
            //read registry to get the current mode
            string keyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

            //check current system preference: 1 = light mode; 0 = dark mode
            string registryValueName = valueName;// "SystemUsesLightTheme";
            object registryValueObject = Registry.GetValue(keyPath, registryValueName, null);
            int registryValueConvertedToInt = (int)registryValueObject;

            return registryValueConvertedToInt;

            ////check current apps preference: 1 = light mode; 0 = dark mode
            //string valueNameAppLightMode = "AppsUseLightTheme";
            //object isCurrentAppsInLightModeObj = Registry.GetValue(keyPath, valueNameAppLightMode, null);
            //int isCurrentAppsInLightMode = (int)isCurrentAppsInLightModeObj;
        }

        private void SwitchToDarkMode()
        {
            // 1 = light mode; 0 = dark mode
            SetCurrentVersionRegistryValue("SystemUsesLightTheme", 0);
            SetCurrentVersionRegistryValue("AppsUseLightTheme", 0);

            BroadcastThemeChange();
        }

        private void SwitchToLightMode()
        {
            // 1 = light mode; 0 = dark mode
            SetCurrentVersionRegistryValue("SystemUsesLightTheme", 1);
            SetCurrentVersionRegistryValue("AppsUseLightTheme", 1);

            BroadcastThemeChange();
        }

        private void BroadcastThemeChange()
        {
            const uint WM_SETTINGCHANGE = 0x001A;
            const uint SMTO_ABORTIFHUNG = 0x0002;
            IntPtr HWND_BROADCAST = new IntPtr(0xffff);

            // Broadcast "ImmersiveColorSet"
            IntPtr lParamThemeChanged = Marshal.StringToHGlobalUni("ImmersiveColorSet");
            try
            {
                SendMessageTimeout(
                    HWND_BROADCAST,
                    WM_SETTINGCHANGE,
                    IntPtr.Zero,
                    lParamThemeChanged,
                    SMTO_ABORTIFHUNG,
                    1000,
                    out _);
            }
            finally
            {
                Marshal.FreeHGlobal(lParamThemeChanged);
            }

            // Broadcast "WindowsThemeElement"
            IntPtr lParamTheme = Marshal.StringToHGlobalUni("WindowsThemeElement");
            try
            {
                SendMessageTimeout(
                    HWND_BROADCAST,
                    WM_SETTINGCHANGE,
                    IntPtr.Zero,
                    lParamTheme,
                    SMTO_ABORTIFHUNG,
                    1000,
                    out _);
            }
            finally
            {
                Marshal.FreeHGlobal(lParamTheme);
            }

            // Optionally, also broadcast "UserPreferences"
            IntPtr lParamUserPref = Marshal.StringToHGlobalUni("UserPreferences");
            try
            {
                SendMessageTimeout(
                    HWND_BROADCAST,
                    WM_SETTINGCHANGE,
                    IntPtr.Zero,
                    lParamUserPref,
                    SMTO_ABORTIFHUNG,
                    1000,
                    out _);
            }
            finally
            {
                Marshal.FreeHGlobal(lParamUserPref);
            }
        }

        private void SetCurrentVersionRegistryValue(string valueName, object val)
        {
            //current theme path
            string keyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

            //set current system preference: 1 = light mode; 0 = dark mode
            Registry.SetValue(keyPath, valueName, val, RegistryValueKind.DWord);

            //Registry.CurrentUser.Flush();
        }

        private void SetRegistryValueWithPath(string keyPath, string valueName, object val)
        {

            //set current system preference: 1 = light mode; 0 = dark mode
            Registry.SetValue(keyPath, valueName, val, RegistryValueKind.DWord);

        }

        private Icon GetApplicableIcon(bool isCurrentThemeIsDark)
        {
            var specialFolderPath = AppContext.BaseDirectory;
            var darkModeIcon = new System.Drawing.Icon(specialFolderPath + @"\icons\dark_mode_icon.ico");
            var lightModeIcon = new System.Drawing.Icon(specialFolderPath + @"\icons\light_mode_icon.ico");
            Icon notifyIcon = darkModeIcon;
            if (isCurrentThemeIsDark)
            {
                //show light mode icon when system theme is set to dark mode
                notifyIcon = lightModeIcon;
            }
            return notifyIcon;
        }
    }
}
