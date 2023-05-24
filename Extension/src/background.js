const NATIVE_MSG_HOST_FQDN = "com.aldaviva.tagscanner.discogs";

chrome.runtime.onMessage.addListener((messageFromPage, sender, sendResponseToPage) => {
	if(messageFromPage.action === "sendReleaseToTagScanner"){
		const releaseId = messageFromPage.releaseId;
		const messageToNative = {
			service: "discogs",
			releaseId: releaseId
		};

		console.info(`Sending Discogs release ${releaseId} to TagScanner`, messageToNative);
		chrome.runtime.sendNativeMessage(NATIVE_MSG_HOST_FQDN, messageToNative, nativeResponse => {
			let pageResponse = { error: null };
			if(nativeResponse){
				console.info("Received response from native messaging host", nativeResponse);
				pageResponse.error = nativeResponse.error;
			} else {
				pageResponse.error = chrome.runtime.lastError.message;
			}
			
			console.debug("Sending response to page:", pageResponse);
			sendResponseToPage(pageResponse);
		});

		return true; //allow sendResponseToPage to be called asynchronously instead of synchronously closing the pipe
	}
});