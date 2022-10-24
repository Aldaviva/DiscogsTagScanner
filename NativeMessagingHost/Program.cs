using System.Windows.Forms;
using NativeMessagingHost;
using NativeMessagingHost.Messages;

const string APPLICATION_NAME        = "com.aldaviva.tagscanner.discogs";
const string APPLICATION_DESCRIPTION = "TagScanner Discogs";
const string ALLOWED_SENDER          = "chrome-extension://gmlloidllimahcoedhmlgcfidpopfhmi/";

string? errorMessage = null;
try {
    switch (MessagingHost.getLaunchMode(ALLOWED_SENDER)) {
        case MessagingHost.LaunchMode.MANUAL:
            await MessagingHost.install(APPLICATION_NAME, APPLICATION_DESCRIPTION, ALLOWED_SENDER);
            MessageBox.Show("Installed Native Messaging Host into Chromium.", "DiscogsTagScanner", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 0;
        case MessagingHost.LaunchMode.DISALLOWED_SENDER:
            throw new BrowserMarshalException($"Wrong sender, expected {ALLOWED_SENDER}");
        case MessagingHost.LaunchMode.ALLOWED_SENDER:
            //continue program
            break;
        default:
            throw new ArgumentOutOfRangeException("Unknown LaunchMode", (Exception?) null);
    }

    ExtensionRequest request = MessagingHost.readInputFromBrowser<ExtensionRequest>();

    TagScannerController tagScannerController;
    try {
        tagScannerController = new TagScannerController();
    } catch (WindowNotFoundException) {
        TagScannerController.launch();

        //retry once after launching
        Thread.Sleep(3000);
        tagScannerController = new TagScannerController();
    }

    tagScannerController.searchForOnlineRelease(request.service, request.releaseId);
} catch (TagScannerException e) {
    errorMessage = string.Join(": ", new[] { e.GetType().Name, e.Message, e.InnerException?.ToString() }.Where(s => s != null));
}

MessagingHost.writeOutputToBrowser(new ExtensionResponse(errorMessage));

return errorMessage is null ? 0 : 1;