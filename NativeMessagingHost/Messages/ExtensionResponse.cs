namespace NativeMessagingHost.Messages;

public record ExtensionResponse(string? error) {

    public string? error { get; set; } = error;

}