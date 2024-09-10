using Microsoft.Win32;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NativeMessagingHost;

public static class MessagingHost {

    private static readonly JsonSerializerOptions JSON_OPTIONS = new() { Converters = { new JsonStringEnumConverter() }, WriteIndented = true };

    public static LaunchMode getLaunchMode(params string[] allowedSenders) {
        /*
         * When launched by Chromium, the first non-EXE argument is the extension's origin (chrome-extension://eokhcjeakpgiplhgpdbcpgijnoofanjp/)
         * When launched by a user, there are usually no non-EXE arguments.
         */
        if (Environment.GetCommandLineArgs().ElementAtOrDefault(1) is { } sender) {
            return allowedSenders.Contains(sender) ? LaunchMode.ALLOWED_SENDER : LaunchMode.DISALLOWED_SENDER;
        } else {
            return LaunchMode.MANUAL;
        }
    }

    /// <exception cref="BrowserMarshalException"></exception>
    public static T readInputFromBrowser<T>() {
        using Stream stdin = Console.OpenStandardInput();

        byte[] lengthBuffer = new byte[4];
        int    readBytes    = stdin.Read(lengthBuffer, 0, lengthBuffer.Length);
        if (readBytes != lengthBuffer.Length) {
            throw new BrowserMarshalException($"Only read {readBytes} bytes, not {lengthBuffer.Length}, while getting JSON length");
        }

        uint       inputLength = BitConverter.ToUInt32(lengthBuffer, 0);
        Span<byte> jsonInput   = new(new byte[inputLength]);
        readBytes = stdin.Read(jsonInput);

        try {
            if (JsonSerializer.Deserialize(jsonInput[..readBytes], typeof(T), JSON_OPTIONS) is T request) {
                return request;
            } else {
                throw new BrowserMarshalException("Failed to parse stdin JSON");
            }
        } catch (Exception e) when (e is JsonException or NotSupportedException) {
            throw new BrowserMarshalException("Failed to parse stdin JSON", e);
        }
    }

    public static void writeOutputToBrowser(object output) {
        using Stream stdout = Console.OpenStandardOutput();

        byte[] responseJsonBytes = JsonSerializer.SerializeToUtf8Bytes(output, JSON_OPTIONS);
        stdout.Write(BitConverter.GetBytes((uint) responseJsonBytes.Length));
        stdout.Write(responseJsonBytes);
        stdout.Flush();
    }

    public static async Task install(string applicationName, string applicationDescription, params string[] allowedOrigins) {
        NativeMessagingHostManifest manifest = new(applicationName, applicationDescription, allowedOrigins);

        Environment.CurrentDirectory = Path.GetDirectoryName(getCurrentExeAbsolutePath())!;
        await using FileStream jsonStream = File.Create("chromiumNativeMessagingHostManifest.json");
        await JsonSerializer.SerializeAsync(jsonStream, manifest, JSON_OPTIONS);

        Registry.SetValue(Path.Combine(@"HKEY_CURRENT_USER\SOFTWARE\Google\Chrome\NativeMessagingHosts", applicationName), null, jsonStream.Name);
    }

    public enum LaunchMode {

        MANUAL,
        ALLOWED_SENDER,
        DISALLOWED_SENDER

    }

    private record NativeMessagingHostManifest(string name, string description, params string[] allowedOrigins) {

        public string path { get; set; } = getCurrentExeAbsolutePath();
        public string type => "stdio";

        [JsonPropertyName("allowed_origins")]
        public string[] allowedOrigins { get; set; } = allowedOrigins;

    }

    private static string getCurrentExeAbsolutePath() {
        return Environment.ProcessPath!;
    }

}