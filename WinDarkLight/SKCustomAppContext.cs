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
        private readonly Icon _darkModeIcon;
        private readonly Icon _lightModeIcon;

        public SKCustomAppContext()
        {
            _isDark = false;
            
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            
            using (var darkStream = assembly.GetManifestResourceStream("WinDarkLight.icons.dark_mode_icon.ico"))
            {
                if (darkStream == null) throw new Exception("dark mode icon not found in resources");
                _darkModeIcon = new System.Drawing.Icon(darkStream);
            }

            using (var lightStream = assembly.GetManifestResourceStream("WinDarkLight.icons.light_mode_icon.ico"))
            {
                if (lightStream == null) throw new Exception("light mode icon not found in resources");
                _lightModeIcon = new System.Drawing.Icon(lightStream);
            }

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

            //attach event handler for clicking
            _trayIcon.MouseClick += new MouseEventHandler(FlipDarkOrLightTheme);

            //attach event handler for external system theme changes
            SystemEvents.UserPreferenceChanged += UserPreferenceChanged;
        }

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                // Re-read from registry to see what the active mode is now
                var isCurrentSystemInLightMode = GetRegistryValue("SystemUsesLightTheme");
                _isDark = (isCurrentSystemInLightMode == 0);

                // Update the icon to reflect the new state
                _trayIcon.Icon = GetApplicableIcon(_isDark);
            }
        }

        private async void FlipDarkOrLightTheme(object? sender, MouseEventArgs e)
        {
            // Only toggle on Left click. Right click is reserved for Context Menu.
            if (e.Button != MouseButtons.Left)
                return;

            // Show an intermediate loading state since the broadcast may take time
            var waitIcon = System.Drawing.Icon.FromHandle(Cursors.WaitCursor.Handle);
            _trayIcon.Icon = waitIcon;
            _trayIcon.Text = "Applying theme changes...";

            if (_isDark)
            {
                //flip the flag
                _isDark = !_isDark;

                //switch under task worker so we don't block the UI thread
                await Task.Run(() => SwitchToLightMode());

                //change icon to dark
                _trayIcon.Icon = GetApplicableIcon(_isDark);

            }
            else
            {
                //flip the flag
                _isDark = !_isDark;

                //switch under task worker so we don't block the UI thread
                await Task.Run(() => SwitchToDarkMode());

                //change icon to light                
                _trayIcon.Icon = GetApplicableIcon(_isDark);
            }

            _trayIcon.Text = "WinDarkLight";

            // forcefully refresh the icon by toggling visibility
            _trayIcon.Visible = false;
            _trayIcon.Visible = true;
            
            // Clean up the temporary handle
            waitIcon.Dispose();
        }

        void ExitClickHandler(object? sender, EventArgs e)
        {
            // Detach system event to avoid memory leaks
            SystemEvents.UserPreferenceChanged -= UserPreferenceChanged;

            _trayIcon.Visible = false;
            _darkModeIcon.Dispose();
            _lightModeIcon.Dispose();
            _trayIcon.Dispose();
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
            object? registryValueObject = Registry.GetValue(keyPath, valueName, null);
            
            // Default to light mode (1) if the registry value does not exist
            if (registryValueObject == null)
            {
                return 1;
            }

            return (int)registryValueObject;

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
            if (isCurrentThemeIsDark)
            {
                //show light mode icon when system theme is set to dark mode
                return _lightModeIcon;
            }
            return _darkModeIcon;
        }
    }
}
