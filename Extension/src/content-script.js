const releaseActionsSection = document.getElementById("release-actions");
const shortcodeButton = document.getElementById("shortcode-tooltip").parentElement;

const tagScannerButton = document.createElement("button");
tagScannerButton.className = "tagscanner";

const icon = document.createElement("div");
icon.className = "icon";
tagScannerButton.appendChild(icon);

const label = document.createElement("span");
label.textContent = "Send to TagScanner";
tagScannerButton.appendChild(label);

tagScannerButton.addEventListener("click", event => {
	const releaseId = document.querySelector("head link[rel=canonical]").href.match(/\/release\/(?<releaseId>\d+)-/).groups.releaseId;
	const messageToServiceWorker = {
		action: "sendReleaseToTagScanner",
		releaseId: releaseId,
		service: "discogs"
	};

	console.info(`Sending Discogs release ${releaseId} to background extension service worker`);
	chrome.runtime.sendMessage(messageToServiceWorker, response => {
		console.info("Response from service worker:", response);
		if(response.error){
			window.alert(response.error);
		}
	});
});

shortcodeButton.parentElement.insertBefore(tagScannerButton, shortcodeButton);

