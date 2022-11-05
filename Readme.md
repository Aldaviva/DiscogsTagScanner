<img src="Extension/src/images/24.png" /> DiscogsTagScanner
===

[![Build status](https://img.shields.io/github/workflow/status/Aldaviva/DiscogsTagScanner/.NET?logo=github)](https://github.com/Aldaviva/DiscogsTagScanner/actions/workflows/dotnet.yml)

[<img src="https://www.discogs.com/favicon.ico" height="16"/> Discogs](https://www.discogs.com/) is an online database of music release metadata. [<img src="https://www.xdlab.ru/favicon.ico" height="16"/> TagScanner](https://www.xdlab.ru/en/) is a program that edits music file metadata.

This browser extension and native program help you quickly load Discogs releases in TagScanner with one click, without having to manually copy and paste the release ID or name from your browser, or use TagScanner's limited search feature.

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="true" levels="1,2,3" -->

- [Screenshots](#screenshots)
- [Requirements](#requirements)
- [Installation](#installation)
    - [Browser extension](#browser-extension)
    - [Native program](#native-program)
- [Usage](#usage)
- [Developing](#developing)

<!-- /MarkdownTOC -->

<a id="screenshots"></a>
## Screenshots

|Discogs||TagScanner|
|:---:|:---:|:---:|
|<img src="https://i.imgur.com/ClZodn1.png" />|<h3>&#8680;</h3>|<img src="https://i.imgur.com/x13AyRC.png" />|
|Click the new **<img src="Extension/src/images/48.png" height="16" /> Send to TagScanner** button||Release loads in Online pane|


<a id="requirements"></a>
## Requirements

- Windows
- [.NET Desktop Runtime 6 x64 for Windows](https://dotnet.microsoft.com/en-us/download/dotnet/6.0/runtime?initial-os=windows) or later
- Chromium-based browser, such as [Vivaldi](https://vivaldi.com/download/) or [Chrome](https://www.google.com/chrome/)
- [TagScanner](https://www.xdlab.ru/en/download.htm)

<a id="installation"></a>
## Installation

<a id="browser-extension"></a>
### Browser extension

1. Download `DiscogsTagScanner.crx` from the [latest release](https://github.com/Aldaviva/DiscogsTagScanner/releases/latest).
1. In your browser, open the Extensions page by going to `chrome://extensions/`.
1. Temporarily enable **Developer mode**.
1. Drag and drop `DiscogsTagScanner.crx` into the Extensions page.
    - If drag and drop does not work, try entering `chrome://extensions/` into the URL bar once again.
1. Accept the confirmation prompt to install the extension.
1. You can now delete `DiscogsTagScanner.crx` and turn off Developer mode if you wish.

<a id="native-program"></a>
### Native program

1. Download `NativeMessagingHost.exe` from the [latest release](https://github.com/Aldaviva/DiscogsTagScanner/releases/latest) and save it somewhere, such as `C:\Program Files\DiscogsTagScanner\NativeMessagingHost.exe`.
1. Run `NativeMessagingHost.exe` once, which registers the native messaging host with Chromium-based browsers on your computer.

<a id="usage"></a>
## Usage
1. Open TagScanner and highlight the files you want to edit.
1. Go to the Discogs release page of the files' release.
1. At the top of the right column, next to the **Release** heading, click **<img src="Extension/src/images/48.png" height="16" /> Send to TagScanner**.
    - TagScanner will load the Discogs release in the Online pane. You can click the **Preview** button to see the pending changes.
1. Click the **Save** button to write the changes to the files.

<a id="developing"></a>
## Developing

The following steps are for developers only. You don't need to follow these steps to use this extension.

1. Build the browser extension CRX by running the PowerShell script
    ```powershell
    .\Extension\build.ps1
    ```
    The build script depends on Vivaldi for packing the CRX (you can replace this with Chrome) and a private key saved in `Extension\PackExtensionPrivateKey.pem`.
1. Build the native messaging host by publishing `NativeMessagingHost.csproj` in Visual Studio Community 2022 or later, which produces `NativeMessagingHost\bin\Release\net6.0-windows\publish\win-x64\NativeMessagingHost.exe`. You can alternatively run
    ```powershell
    dotnet publish -c Release -p:PublishSingleFile=true -r win-x64 --self-contained false .\NativeMessagingHost\NativeMessagingHost.csproj
    ```
