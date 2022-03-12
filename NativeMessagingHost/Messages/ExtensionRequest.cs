namespace NativeMessagingHost.Messages;

public record ExtensionRequest {

    public OnlineMetadataService service { get; set; }
    public string releaseId { get; set; } = null!;

}