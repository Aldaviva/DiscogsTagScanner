using ManagedWinapi.Windows;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Forms;

namespace NativeMessagingHost;

public class TagScannerController {

    private const string UNINSTALL_KEY  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const uint   WM_LBUTTONUP   = 0x0202;
    private const uint   WM_LBUTTONDOWN = 0x0201;
    private const uint   MK_LBUTTON     = 0x0001;

    private readonly AutomationElement tMain;

    /// <exception cref="WindowNotFoundException"></exception>
    public TagScannerController() {
        IntPtr tMainHandle = SystemWindow.FilterToplevelWindows(window => window.ClassName == "TMain" && window.Process.ProcessName == "Tagscan").FirstOrDefault()?.HWnd ??
            throw new WindowNotFoundException("Could not find TagScanner window with classname TMain");
        tMain = AutomationElement.FromHandle(tMainHandle);
    }

    /// <exception cref="ElementNotFound"></exception>
    public void searchForOnlineRelease(OnlineMetadataService service, string releaseId) {
        // Restore the window if it was minimized
        WindowPattern window = (WindowPattern) tMain.GetCurrentPattern(WindowPattern.Pattern);
        if (window.Current.WindowVisualState == WindowVisualState.Minimized) {
            window.SetWindowVisualState(WindowVisualState.Normal);
        }

        // Bring window to foreground if it was not the topmost window
        Foregrounder.Foregrounder.BringToForeground(tMain);

        // Click Online tab
        AutomationElement onlineTab = findDescendantElementByIndex(tMain, 2, 3, 0) ?? throw new ElementNotFound("Could not find Online tab");
        if (onlineTab.Current.Name.Trim() == "ONLINE") {
            postClick(onlineTab.toHwnd());
        } else {
            throw new ElementNotFound($"Online tab has the wrong name ({onlineTab.Current.Name}), the UI hierarchy may have changed");
        }

        AutomationElement onlinePane = findDescendantElementByIndex(tMain, 3, 0, 0) ?? throw new ElementNotFound("Could not find Online panel");
        if (onlinePane.Current.Name.Trim() != "Onlie results") { //sic
            throw new ElementNotFound($"Online panel has the wrong name ({onlinePane.Current.Name}), the UI hierarchy may have changed");
        }

        // If service dropdown value does not match requested service, change it
        AutomationElement serviceDropdown = findDescendantElementByIndex(onlinePane, 1, 0) ?? throw new ElementNotFound("Could not find service dropdown");
        string            oldService      = ((SelectionPattern) serviceDropdown.GetCurrentPattern(SelectionPattern.Pattern)).Current.GetSelection()[0].Current.Name.Trim();
        if (oldService != service.getPresentationLabel()) {
            AutomationElement discogsOption = findDescendantElementByIndex(serviceDropdown, 0)?
                    .FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, "  " + service.getPresentationLabel()))
                ?? throw new ElementNotFound($"Could not find {service} option in service dropdown");
            ((SelectionItemPattern) discogsOption.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
        }

        // Set release text box value to release ID
        AutomationElement releaseTextBox = findDescendantElementByIndex(onlinePane, 3, 3) ?? throw new ElementNotFound("Could not find release search text box");
        ((ValuePattern) releaseTextBox.GetCurrentPattern(ValuePattern.Pattern)).SetValue(releaseId);

        // Submit search
        IntPtr onlineSearchButton = findDescendantElementByIndex(onlinePane, 3, 2)?.toHwnd() ?? throw new ElementNotFound("Could not find online submit button");
        postClick(onlineSearchButton);

        // Focus results list
        Thread.Sleep(1000); // don't know how to tell when the results have loaded
        AutomationElement resultsList = findDescendantElementByIndex(onlinePane, 2) ?? throw new ElementNotFound("Could not find online results list");
        resultsList.SetFocus();

        // Expand first result
        sendKeyboardPress(Keys.Right);
    }

    private static AutomationElement? findDescendantElementByIndex(AutomationElement ancestor, params int[] childIndices) {
        // ReSharper disable once RedundantLambdaParameterType - preserving nullable type prevents null return warnings in method
        return childIndices.Aggregate(ancestor, (AutomationElement? parent, int childIndex) => {
            AutomationElementCollection? children = parent?.FindAll(TreeScope.Children, Condition.TrueCondition);
            if (children is null || children.Count == 0) {
                return null;
            }

            return children.Cast<AutomationElement>().ElementAtOrDefault((childIndex + children.Count) % children.Count);
        });
    }

    /// <exception cref="MessagePostException"></exception>
    private static void postClick(IntPtr window) {
        if (!PostMessage(window, WM_LBUTTONDOWN, MK_LBUTTON, 0)) {
            throw new MessagePostException($"Failed to post WM_LBUTTONDOWN to {window.ToInt64():X}", new Win32Exception(Marshal.GetLastWin32Error()));
        }

        Thread.Sleep(30);

        if (!PostMessage(window, WM_LBUTTONUP, 0, 0)) {
            throw new MessagePostException($"Failed to post WM_LBUTTONUP to {window.ToInt64():X}", new Win32Exception(Marshal.GetLastWin32Error()));
        }
    }

    /// <summary>
    /// Send key down and key up to active window
    /// </summary>
    /// <param name="key">https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes</param>
    private static void sendKeyboardPress(Keys key) {
        sendKeyboardEvent((byte) key, 0, 0, 0);
        Thread.Sleep(10);
        sendKeyboardEvent((byte) key, 0, 2, 0);
    }

    /// <exception cref="StartException"></exception>
    public static int launch() {
        // The DisplayIcon value is the absolute path to the EXE, e.g. "C:\Programs\Multimedia\TagScanner\Tagscan.exe"
        using RegistryKey? uninstallKey               = Registry.LocalMachine.OpenSubKey(UNINSTALL_KEY);
        string?            tagScannerUninstallKeyName = uninstallKey?.GetSubKeyNames().LastOrDefault(key => key.StartsWith("TagScanner"));
        if (tagScannerUninstallKeyName is not null && uninstallKey?.OpenSubKey(tagScannerUninstallKeyName) is { } tagScannerUninstallKey) {
            using (tagScannerUninstallKey) {
                if (tagScannerUninstallKey.GetValue("DisplayIcon") is string exeAbsolutePath && File.Exists(exeAbsolutePath)) {
                    try {
                        using Process process = Process.Start(exeAbsolutePath);
                        return process.Id;
                    } catch (Win32Exception e) {
                        throw new StartException("TagScanner was not already running, and it could not be started by running " + exeAbsolutePath, e);
                    }
                }
            }
        }

        throw new StartException("TagScanner was not already running, and could not find installation directory in registry key " + (uninstallKey?.Name ?? UNINSTALL_KEY));
    }

    [DllImport("User32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hwnd, uint msg, uint wParam, uint lParam);

    [DllImport("User32.dll", EntryPoint = "keybd_event")]
    private static extern void sendKeyboardEvent(byte virtualKeyCode, byte hardwareScanCode, uint flags, int extra);

}