using System.Windows.Automation;

namespace NativeMessagingHost;

public static class Extensions {

    public static IntPtr toHwnd(this AutomationElement element) {
        return new IntPtr(element.Current.NativeWindowHandle);
    }

}