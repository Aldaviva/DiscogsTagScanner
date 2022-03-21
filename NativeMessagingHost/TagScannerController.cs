using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using ManagedWinapi.Windows;

namespace NativeMessagingHost;

public class TagScannerController {

    private const uint WM_LBUTTONUP   = 0x0202;
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint MK_LBUTTON     = 0x0001;

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
        IntPtr onlineTab = findDescendantElementByIndex(tMain, 4, 0, 0)?.toHwnd() ?? throw new ElementNotFound("Could not find Online tab");
        postClick(onlineTab);

        AutomationElement onlinePane = findDescendantElementByIndex(tMain, 5, 0, 0) ?? throw new ElementNotFound("Could not find Online panel");

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

        // Submit search somehow (keyboard enter? some automation thing?)
        IntPtr onlineSearchButton = findDescendantElementByIndex(onlinePane, 3, 2)?.toHwnd() ?? throw new ElementNotFound("Could not find online submit button");
        postClick(onlineSearchButton);
    }

    private static AutomationElement? findDescendantElementByIndex(AutomationElement ancestor, params int[] childIndices) {
        return childIndices.Aggregate(ancestor, (AutomationElement? parent, int childIndex) => {
            AutomationElementCollection? children = parent?.FindAll(TreeScope.Children, Condition.TrueCondition);
            return children?.Cast<AutomationElement>().ElementAtOrDefault((childIndex + children.Count) % children.Count);
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

    [DllImport("User32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hwnd, uint msg, uint wParam, uint lParam);

}