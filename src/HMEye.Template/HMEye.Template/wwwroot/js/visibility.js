window.visibilityHandler = {
    initialize: function (dotNetRef) {
        function notifyVisibilityChange() {
            if (document.visibilityState === "visible") {
                dotNetRef.invokeMethodAsync('OnVisibilityChanged');
            }
        }

        document.addEventListener('visibilitychange', notifyVisibilityChange);
    }
};
