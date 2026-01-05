
(function () {
    const cookieName = "sessionId";

    function getCookie(name) {
        const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
        return match ? match[2] : null;
    }

    function setSessionCookie(name, value) {
        document.cookie = `${name}=${value}; path=/; SameSite=Lax`;
    }

    function refreshSessionId() {
        setSessionCookie(cookieName, crypto.randomUUID());
    }

    if (window.location.pathname === "/account/login") {
        refreshSessionId();
    } else {
        if (!getCookie(cookieName)) {
            refreshSessionId();
        }
    }
})();
