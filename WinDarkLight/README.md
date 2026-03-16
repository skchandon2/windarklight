# WinDarkLight

WinDarkLight is a lightweight, responsive Windows system tray utility built in **C# .NET 10 (WinForms)** that allows you to instantly toggle your system-wide Windows theme between Light Mode and Dark Mode with a single click. 

Unlike native Windows personalization settings that require navigating menus, WinDarkLight sits unobtrusively in your taskbar tray. It listens to Windows OS theme changes, meaning it stays perfectly in sync whether you toggle the theme via the tray icon or automatically via Windows settings.

## Features

- **One-Click Toggle:** Left-click the system tray icon to instantly flip between Windows Light and Dark themes.
- **Smart System Sync:** Listens for `SystemEvents.UserPreferenceChanged`. If you change your theme in the Windows Settings app, WinDarkLight instantly updates its internal state and icon to match the OS!
- **System-Wide Broadcast:** Reliable theme repainting. Refreshes all open applications immediately by broadcasting `HWND_BROADCAST` along with `WM_SETTINGCHANGE` for `ImmersiveColorSet`, `WindowsThemeElement`, and `UserPreferences`.
- **Asynchronous Broadcasting:** Ensures the UI thread never hangs, providing an immediate "loading" cursor while long broadcasts complete in the background.
- **Fully Standalone:** Built and deployed as a single-file executable, containing the entire .NET 10 runtime. No installation or prerequisites required!

## Distribution & Usage

### Running Locally (Source)
If you have the .NET SDK installed:
```bash
git clone <your-repo-link>
cd WinDarkLight
dotnet run
```

### Distributable (.exe) Download
For normal users, **this app does not require installation or .NET dependencies**. 


## Tech Stack
- **Language**: C# 12
- **Framework**: .NET 10.0 Windows Forms
- **Win32 APIs**: `SendMessageTimeout`, `HWND_BROADCAST`

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.   