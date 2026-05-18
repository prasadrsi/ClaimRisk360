"use strict";

// SignalR Notification Client
(function () {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/notifications")
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    // --- Toast Notifications ---
    connection.on("ReceiveNotification", function (data) {
        showToast(data.title, data.message, data.type, data.claimId);
    });

    // --- Badge Update ---
    connection.on("BadgeUpdate", function (count) {
        updateBadge(count);
    });

    // --- Data Refresh ---
    connection.on("DataRefresh", function (data) {
        handleDataRefresh(data.area, data.entityId);
    });

    // --- Connection Management ---
    connection.onreconnecting(function () {
        updateConnectionStatus("reconnecting");
    });

    connection.onreconnected(function () {
        updateConnectionStatus("connected");
        showToast("Reconnected", "Real-time connection restored", "success");
    });

    connection.onclose(function () {
        updateConnectionStatus("disconnected");
        setTimeout(startConnection, 5000);
    });

    function startConnection() {
        connection.start()
            .then(function () {
                updateConnectionStatus("connected");
            })
            .catch(function (err) {
                updateConnectionStatus("disconnected");
                setTimeout(startConnection, 5000);
            });
    }

    // --- Toast Display ---
    function showToast(title, message, type, claimId) {
        var container = document.getElementById("cr-toast-container");
        if (!container) return;

        var iconMap = {
            success: "bi-check-circle-fill text-success",
            danger: "bi-exclamation-triangle-fill text-danger",
            warning: "bi-exclamation-circle-fill text-warning",
            info: "bi-info-circle-fill text-primary"
        };

        var bgMap = {
            success: "border-success",
            danger: "border-danger",
            warning: "border-warning",
            info: "border-primary"
        };

        var icon = iconMap[type] || iconMap.info;
        var border = bgMap[type] || bgMap.info;
        var now = new Date().toLocaleTimeString();
        var id = "toast-" + Date.now();

        var linkHtml = "";
        if (claimId) {
            linkHtml = ' <a href="/Explainability?ClaimId=' + claimId + '" class="text-decoration-underline small">View</a>';
        }

        var html =
            '<div id="' + id + '" class="toast border-start border-4 ' + border + ' show" role="alert">' +
            '  <div class="toast-header">' +
            '    <i class="bi ' + icon + ' me-2"></i>' +
            '    <strong class="me-auto">' + title + '</strong>' +
            '    <small class="text-muted">' + now + '</small>' +
            '    <button type="button" class="btn-close" data-bs-dismiss="toast"></button>' +
            '  </div>' +
            '  <div class="toast-body">' +
            '    ' + message + linkHtml +
            '  </div>' +
            '</div>';

        container.insertAdjacentHTML("beforeend", html);

        // Auto-dismiss after 8 seconds
        setTimeout(function () {
            var toastEl = document.getElementById(id);
            if (toastEl) {
                toastEl.classList.remove("show");
                setTimeout(function () { toastEl.remove(); }, 300);
            }
        }, 8000);

        // Play notification sound (subtle)
        playNotificationSound();
    }

    // --- Badge Update ---
    function updateBadge(count) {
        var badges = document.querySelectorAll(".cr-notif-badge");
        badges.forEach(function (badge) {
            if (count > 0) {
                badge.textContent = count;
                badge.style.display = "";
            } else {
                badge.style.display = "none";
            }
        });

        // Update the bell icon parent title
        var bells = document.querySelectorAll("[title*='claim(s) pending review']");
        bells.forEach(function (bell) {
            bell.title = count + " claim(s) pending review";
        });
    }

    // --- Data Refresh ---
    function handleDataRefresh(area, entityId) {
        var currentPath = window.location.pathname.toLowerCase();

        var refreshMap = {
            "claims": ["/claims"],
            "cases": ["/casemanagement"],
            "role": ["/", "/index", "/usermanagement"],
            "users": ["/usermanagement"]
        };

        // Always refresh dashboard and reports
        var alwaysRefresh = ["/dashboard", "/reports"];

        var shouldRefresh = false;
        var targets = refreshMap[area] || [];

        for (var i = 0; i < targets.length; i++) {
            if (currentPath === targets[i] || currentPath === targets[i] + "/") {
                shouldRefresh = true;
                break;
            }
        }

        if (!shouldRefresh) {
            for (var j = 0; j < alwaysRefresh.length; j++) {
                if (currentPath === alwaysRefresh[j] || currentPath === alwaysRefresh[j] + "/") {
                    shouldRefresh = true;
                    break;
                }
            }
        }

        if (shouldRefresh) {
            // Show a subtle refresh bar
            showRefreshBar();
        }
    }

    // --- Refresh Bar ---
    function showRefreshBar() {
        // Don't show if one is already visible
        if (document.getElementById("cr-refresh-bar")) return;

        var bar = document.createElement("div");
        bar.id = "cr-refresh-bar";
        bar.className = "cr-refresh-bar";
        bar.innerHTML =
            '<div class="d-flex align-items-center justify-content-center gap-2">' +
            '  <i class="bi bi-arrow-repeat cr-spin"></i>' +
            '  <span>Data has been updated.</span>' +
            '  <button class="btn btn-sm btn-light" onclick="location.reload()">Refresh Now</button>' +
            '  <button type="button" class="btn-close btn-close-white btn-sm ms-2" onclick="this.closest(\'.cr-refresh-bar\').remove()"></button>' +
            '</div>';

        var main = document.querySelector(".cr-main");
        if (main) {
            main.insertBefore(bar, main.querySelector(".cr-content"));
        }

        // Auto-refresh after 15 seconds if user doesn't act
        setTimeout(function () {
            if (document.getElementById("cr-refresh-bar")) {
                location.reload();
            }
        }, 15000);
    }

    // --- Connection Status Indicator ---
    function updateConnectionStatus(status) {
        var indicator = document.getElementById("cr-connection-status");
        if (!indicator) return;

        if (status === "connected") {
            indicator.className = "cr-connection-dot bg-success";
            indicator.title = "Real-time: Connected";
        } else if (status === "reconnecting") {
            indicator.className = "cr-connection-dot bg-warning cr-pulse";
            indicator.title = "Reconnecting...";
        } else {
            indicator.className = "cr-connection-dot bg-danger";
            indicator.title = "Disconnected";
        }
    }

    // --- Notification Sound ---
    function playNotificationSound() {
        try {
            var ctx = new (window.AudioContext || window.webkitAudioContext)();
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.frequency.value = 880;
            osc.type = "sine";
            gain.gain.value = 0.05;
            osc.start();
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.3);
            osc.stop(ctx.currentTime + 0.3);
        } catch (e) {
            // Audio not supported
        }
    }

    // Start connection on page load
    startConnection();
})();
