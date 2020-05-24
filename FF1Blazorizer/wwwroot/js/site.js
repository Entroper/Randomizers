﻿function handleFileSelect(inputId) {
	const input = document.getElementById(inputId);
	const file = input.files[0];
	const reader = new FileReader();

	return new Promise((resolve, reject) => {
		reader.onload = e => {
			const encoded = e.target.result.split(',')[1];
			resolve(encoded);
		};
		reader.onerror = () => {
			reader.abort();
			reject(new DOMException("Error reading file"));
		};
		reader.readAsDataURL(file);
	});
}

async function computePreset(preset) {
	const result = await fetch('presets/' + preset + '.json');
	const overrides = await result.json();

	if (preset !== 'default') {
		const defaultResult = await fetch('presets/default.json');
		const basic = await defaultResult.json();
		overrides.Flags = Object.assign(basic.Flags, overrides.Flags);
	}

	return JSON.stringify(overrides);
}

async function downloadROM(filename, encoded) {
	const url = "data:application/octet-stream;base64," + encoded;
	const result = await fetch(url);
	const blob = await result.blob();

	const anchor = document.createElement('a');
	anchor.download = filename;
	anchor.href = window.URL.createObjectURL(blob);
	anchor.dispatchEvent(new MouseEvent('click'));
}

function updateHistory(seedString, flagString) {
	let href = document.location.href;
	if (href.indexOf('?') > 0) {
		href = href.substr(0, href.indexOf('?'));
	}

	history.replaceState({}, '', href + '?' + 's=' + seedString + '&' + 'f=' + flagString);
}

function copyLocation() {
	const textarea = document.createElement('textarea');
	textarea.value = location.href;
	document.body.appendChild(textarea);
	textarea.select();
	document.execCommand('copy');
	document.body.removeChild(textarea);
}

function getScreenRightEdge() {
	return window.innerWidth;
}

Blazor.start({}).then(() => {

	let newWorker;

	DotNet.invokeMethod('FF1Blazorizer', 'ShowUpdateNotification');
	if ('serviceWorker' in navigator) {
		navigator.serviceWorker.register('/service-worker.js').then(reg => {
			reg.addEventListener('updatefound', () => {
				newWorker = reg.installing;

				newWorker.addEventListener('statechange', () => {
					if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
						DotNet.invokeMethod('FF1Blazorizer', 'ShowUpdateNotification');
					}
				});
			});
		});

		let refreshing;
		navigator.serviceWorker.addEventListener('controllerchange', function () {
			if (refreshing) return;
			window.location.reload();
			refreshing = true;
		});
	}
})


/**
 * Call from Blazor to register the new service worker 
 */
function updateServiceWorkerNow() {
	newWorker.postMessage({ action: 'skipWaiting' });
}
