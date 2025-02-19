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


namespace WinDarkLight
{
    internal class SKCustomAppContext : ApplicationContext
    {
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
                    Items = { new ToolStripMenuItem("Exit", null, Exit) }
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
                SwitchTotDarkMode();

                //change icon to light                
                _trayIcon.Icon = GetApplicableIcon(_isDark);
            }
        }

        void Exit(object? sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            Application.Exit();
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

        private void SwitchTotDarkMode()
        {
            // 1 = light mode; 0 = dark mode
            SetRegistryValue("SystemUsesLightTheme", 0);
            SetRegistryValue("AppsUseLightTheme", 0);
        }

        private void SwitchToLightMode()
        {
            // 1 = light mode; 0 = dark mode
            SetRegistryValue("SystemUsesLightTheme", 1);
            SetRegistryValue("AppsUseLightTheme", 1);
        }

        private void SetRegistryValue(string valueName, object val)
        {
            //read registry to get the current mode
            string keyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";

            //set current system preference: 1 = light mode; 0 = dark mode
            Registry.SetValue(keyPath, valueName, val);

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
