namespace NativeMessagingHost;

public enum OnlineMetadataService {

    FREEDB,
    DISCOGS,
    MUSICBRAINZ

}

public static class OnlineMetadataServiceExtensions {

    public static string getPresentationLabel(this OnlineMetadataService service) => service switch {
        OnlineMetadataService.FREEDB      => "FreeDB",
        OnlineMetadataService.DISCOGS     => "Discogs",
        OnlineMetadataService.MUSICBRAINZ => "MusicBrainz",
        _                                 => throw new ArgumentOutOfRangeException(nameof(service), service, null)
    };

}