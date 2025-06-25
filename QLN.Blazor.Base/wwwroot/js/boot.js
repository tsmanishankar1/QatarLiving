(() => {
    const maximumRetryCount = 3;
    const retryIntervalMilliseconds = 5000;
    const reconnectModal = document.getElementById('reconnect-modal');

    const startReconnectionProcess = () => {
        reconnectModal.style.display = 'block';

        let isCanceled = false;

        console.log("Starting Custom Connection");

        (async () => {
            for (let i = 0; i < maximumRetryCount; i++) {
                reconnectModal.innerText = `Attempting to reconnect: ${i + 1} of ${maximumRetryCount}`;

                await new Promise(resolve => setTimeout(resolve, retryIntervalMilliseconds));

                if (isCanceled) {
                    return;
                }

                try {
                    const result = await Blazor.reconnect();
                    if (!result) {
                        // The server was reached, but the connection was rejected; reload the page.
                        location.reload();
                        return;
                    }

                    // Successfully reconnected to the server.
                    return;
                } catch {
                    // Didn't reach the server; try again.
                }
            }

            // Retried too many times; reload the page.
            location.reload();
        })();

        return {
            cancel: () => {
                isCanceled = true;
                reconnectModal.style.display = 'none';
            },
        };
    };

    let currentReconnectionProcess = null;

    if (!window.__blazorStarted) {
        window.__blazorStarted = true;
        Blazor.start({
            circuit: {
                reconnectionHandler: {
                    onConnectionDown: () => currentReconnectionProcess ??= startReconnectionProcess(),
                    onConnectionUp: () => {
                        currentReconnectionProcess?.cancel();
                        currentReconnectionProcess = null;
                    }
                },
                configureSignalR: function (builder) {
                    builder.withServerTimeout(30000).withKeepAliveInterval(15000);
                }
            }
        });
    }

})();