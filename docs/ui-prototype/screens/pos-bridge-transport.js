/**
 * pos-bridge-transport.js
 * Minimal transport-level helper for WebView2 JS-to-C# communication.
 * This file provides a base layer for message sending and connectivity verification.
 */

(function (window) {
    "use strict";

    // Establish the global transport object if it doesn't exist.
    window.posBridgeTransport = window.posBridgeTransport || {
        /**
         * Checks if the WebView2 messaging environment is available.
         * @returns {boolean} True if postMessage is supported.
         */
        isAvailable: function () {
            return !!(window.chrome && window.chrome.webview && typeof window.chrome.webview.postMessage === "function");
        },

        /**
         * Sends a transport-level ping message to the C# shell.
         * Used to verify that JS-to-C# messaging is working.
         * @param {string} source - The name of the screen or component sending the ping.
         * @returns {boolean} True if the message was attempted.
         */
        sendPing: function (source) {
            if (!this.isAvailable()) {
                if (window.console && typeof window.console.debug === "function") {
                    console.debug("[Bridge] WebView2 transport is not available; ping skipped.");
                }
                return false;
            }

            const ping = {
                type: "transport.ping",
                source: source || "unknown",
                timestamp: new Date().toISOString()
            };

            if (window.console && typeof window.console.debug === "function") {
                console.debug("[Bridge] Sending transport ping:", ping);
            }

            window.chrome.webview.postMessage(ping);
            return true;
        }
    };

    // Task 3.1.5: Listen for transport-level responses from the C# shell.
    // This allows verifying two-way bridge connectivity.
    if (window.chrome && window.chrome.webview && typeof window.chrome.webview.addEventListener === "function") {
        window.chrome.webview.addEventListener('message', function (event) {
            const data = event.data;
            if (data && data.type === "transport.pong") {
                if (window.console && typeof window.console.debug === "function") {
                    console.debug("[Bridge] Received transport pong:", data);
                }
            }
        });
    }

})(window);
