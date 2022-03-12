namespace NativeMessagingHost;

public class TagScannerException: ApplicationException {

    protected TagScannerException(string message, Exception? cause = null): base(message, cause) { }

}

public class StartException: TagScannerException {

    public StartException(string message, Exception? cause = null): base(message, cause) { }

}

public class WindowNotFoundException: TagScannerException {

    public WindowNotFoundException(string message): base(message) { }

}

public class MessagePostException: TagScannerException {

    public MessagePostException(string message, Exception? cause = null): base(message, cause) { }

}

public class ElementNotFound: TagScannerException {

    public ElementNotFound(string message): base(message) { }

}

public class BrowserMarshalException: TagScannerException {

    public BrowserMarshalException(string message, Exception? cause = null): base(message, cause) { }

}