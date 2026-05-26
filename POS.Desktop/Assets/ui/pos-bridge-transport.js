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
         * Checks if the 'pos' host object is available for direct JS-to-C# calls.
         * @returns {boolean} True if the host object exists.
         */
        isHostObjectAvailable: function () {
            return !!(window.chrome && window.chrome.webview && window.chrome.webview.hostObjects && window.chrome.webview.hostObjects.pos);
        },

        /**
         * Logs the current bridge transport status to the console.
         * Used for reachability verification across screens.
         * @param {string} source - The name of the screen or component.
         */
        logStatus: function (source) {
            const status = {
                source: source || "unknown",
                postMessage: this.isAvailable(),
                hostObject: this.isHostObjectAvailable(),
                timestamp: new Date().toISOString()
            };
            if (window.console && typeof window.console.debug === "function") {
                console.debug("[Bridge] Transport status:", status);
            }
            return status;
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

    const PENDING_REQUESTS = new Map();
    const DEFAULT_TIMEOUT_MS = 8000;

    /**
     * window.posBridge
     * Formal Promise-based request/response client for the WebView2 bridge.
     */
    window.posBridge = window.posBridge || {
        /**
         * Checks if the WebView2 messaging environment is available.
         */
        isAvailable: function () {
            return window.posBridgeTransport.isAvailable();
        },

        /**
         * Generates a unique requestId for correlation.
         */
        createRequestId: function () {
            if (typeof window.crypto !== "undefined" && typeof window.crypto.randomUUID === "function") {
                return window.crypto.randomUUID();
            }
            return Date.now().toString(36) + Math.random().toString(36).substring(2);
        },

        /**
         * Sends a formal v1 request to the C# shell and returns a Promise.
         * @param {string} type - The action type (e.g., "catalog.search").
         * @param {object|null} payload - The action parameters.
         * @param {object} options - Optional settings (e.g., timeoutMs).
         */
        request: function (type, payload, options) {
            const self = this;
            return new Promise((resolve, reject) => {
                if (!self.isAvailable()) {
                    reject({ code: "TRANSPORT_UNAVAILABLE", message: "WebView2 transport is not available." });
                    return;
                }

                // Task 3.2.5 Cleanup: Validate type
                if (typeof type !== "string" || !type.trim()) {
                    reject({ code: "INVALID_TYPE", message: "Bridge request type must be a non-empty string." });
                    return;
                }

                // Task 3.2.5 Cleanup: Strengthen payload validation
                // Only objects and null are allowed. Primitives (string, number, boolean) are rejected.
                let finalPayload = payload;
                if (payload === undefined) {
                    finalPayload = null;
                } else if (payload !== null && (typeof payload !== "object" || Array.isArray(payload))) {
                    reject({ code: "INVALID_PAYLOAD", message: "Bridge payload must be an object or null." });
                    return;
                }

                const requestId = self.createRequestId();
                const timeoutMs = (options && options.timeoutMs) || DEFAULT_TIMEOUT_MS;

                const timer = setTimeout(() => {
                    if (PENDING_REQUESTS.has(requestId)) {
                        PENDING_REQUESTS.delete(requestId);
                        reject({ code: "TIMEOUT", message: "Bridge request timed out after " + timeoutMs + "ms", requestId: requestId });
                    }
                }, timeoutMs);

                PENDING_REQUESTS.set(requestId, {
                    resolve: resolve,
                    reject: reject,
                    timer: timer,
                    type: type
                });

                const envelope = {
                    version: "v1",
                    type: type,
                    requestId: requestId,
                    payload: finalPayload,
                    metadata: {
                        timestamp: new Date().toISOString()
                    }
                };

                if (window.console && typeof window.console.debug === "function") {
                    console.debug("[Bridge] Outbound request:", { type: type, requestId: requestId });
                }

                try {
                    window.chrome.webview.postMessage(envelope);
                } catch (err) {
                    PENDING_REQUESTS.delete(requestId);
                    clearTimeout(timer);
                    reject({ code: "SEND_FAILED", message: "Bridge request could not be sent.", requestId: requestId });
                }
            });
        },

        /**
         * Manual verification helper for DevTools.
         * window.posBridge.pingEcho().then(console.log)
         */
        pingEcho: function (options) {
            return this.request("transport.echo", { message: "manual-verification", timestamp: new Date().toISOString() }, options);
        },

        /**
         * Internal handler for inbound messages.
         * Returns true if the message was handled by the correlation logic.
         */
        _handleResponse: function (data) {
            if (!data || !data.requestId || data.version !== "v1") return false;

            const pending = PENDING_REQUESTS.get(data.requestId);
            if (!pending) return false;

            PENDING_REQUESTS.delete(data.requestId);
            clearTimeout(pending.timer);

            if (window.console && typeof window.console.debug === "function") {
                console.debug("[Bridge] Inbound response matched:", { type: data.type, requestId: data.requestId, ok: data.ok });
            }

            if (data.ok) {
                pending.resolve(data.payload);
            } else {
                pending.reject(data.error || { code: "UNKNOWN_ERROR", message: "An unknown bridge error occurred." });
            }

            return true;
        }
    };

    // Task 3.1.5 & 3.2.6: Listen for transport-level and v1 responses from the C# shell.
    if (window.chrome && window.chrome.webview && typeof window.chrome.webview.addEventListener === "function") {
        window.chrome.webview.addEventListener('message', function (event) {
            const data = event.data;

            // Handle formal v1 responses with requestId correlation
            if (window.posBridge && window.posBridge._handleResponse(data)) {
                return;
            }

            // Fallback for Milestone 3.1 transport-level probes
            if (data && data.type === "transport.pong") {
                if (window.console && typeof window.console.debug === "function") {
                    console.debug("[Bridge] Received transport pong:", data);
                }
            }
        });
    }

})(window);
