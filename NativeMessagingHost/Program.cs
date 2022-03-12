using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;
using NativeMessagingHost;
using NativeMessagingHost.Messages;

/*using TagScannerController controller = new();
controller.searchForOnlineRelease(OnlineMetadataService.DISCOGS, "456");
return 0;*/

const string ALLOWED_SENDER   = "chrome-extension://eokhcjeakpgiplhgpdbcpgijnoofanjp/";
const string INSTALLATION_KEY = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\TagScanner_is1";

JsonSerializerOptions jsonOptions = new() { Converters = { new JsonStringEnumConverter() } };

using Stream stdin  = Console.OpenStandardInput();
using Stream stdout = Console.OpenStandardOutput();

string? errorMessage = null;
try {
    string? sender = Environment.GetCommandLineArgs().ElementAtOrDefault(1);
    if (sender != ALLOWED_SENDER) {
        throw new BrowserMarshalException($"Wrong sender, expected {ALLOWED_SENDER}, but got {sender ?? "null"}");
    }

    byte[] lengthBuffer = new byte[4];
    int    readBytes    = stdin.Read(lengthBuffer, 0, lengthBuffer.Length);
    if (readBytes != lengthBuffer.Length) {
        throw new BrowserMarshalException($"Only read {readBytes} bytes, not {lengthBuffer.Length}, while getting JSON length, exiting.");
    }

    uint       inputLength = BitConverter.ToUInt32(lengthBuffer, 0);
    Span<byte> jsonInput   = new(new byte[inputLength]);
    readBytes = stdin.Read(jsonInput);

    try {
        ExtensionRequest request = (ExtensionRequest) JsonSerializer.Deserialize(jsonInput[..readBytes], typeof(ExtensionRequest), jsonOptions)!;

        TagScannerController tagScannerController;
        try {
            tagScannerController = new TagScannerController();
        } catch (WindowNotFoundException) {
            if (Registry.GetValue(INSTALLATION_KEY, "DisplayIcon", null) is string tagScannerAbsolutePath && File.Exists(tagScannerAbsolutePath)) {
                try {
                    Process.Start(tagScannerAbsolutePath);
                } catch (Win32Exception e) {
                    throw new StartException("TagScanner was not already running, and it could not be started by running " + tagScannerAbsolutePath, e);
                }

                Thread.Sleep(5000);
                //retry once after launchingstring? tagScannerAbsolutePath =
                tagScannerController = new TagScannerController();
            } else {
                throw new StartException("TagScanner was not already running, and could not find installation directory in registry key " + INSTALLATION_KEY);
            }
        }

        tagScannerController.searchForOnlineRelease(request.service, request.releaseId);
    } catch (Exception e) when (e is JsonException or NotSupportedException) {
        throw new BrowserMarshalException("Failed to parse stdin JSON", e);
    }
} catch (TagScannerException e) {
    errorMessage = string.Join(": ", new[] { e.GetType().Name, e.Message, e.InnerException?.ToString() }.Where(s => s != null));
}

byte[] responseJsonBytes = JsonSerializer.SerializeToUtf8Bytes(new ExtensionResponse(errorMessage), jsonOptions);
stdout.Write(BitConverter.GetBytes((uint) responseJsonBytes.Length));
stdout.Write(responseJsonBytes);
stdout.Flush();

return errorMessage is null ? 0 : 1;