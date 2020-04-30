(function () {
    window.DisqusFunctions = {
        createDisqus: function (disqusThreadElement, disqusSrc) {
            var dsq = document.createElement('script');
            dsq.type = 'text/javascript';
            dsq.async = true;
            dsq.src = disqusSrc;
            dsq.disqus_container_id = 'disqus_thread';
            disqusThreadElement.appendChild(dsq);
        },
        resetDisqus: function (newIdentifier, newUrl, newTitle) {
            try {
                DISQUS.reset({
                    reload: true,
                    config: function () {
                        this.page.identifier = newIdentifier;
                        this.page.url = newUrl;
                        this.page.title = newTitle;
                    }
                });
            }
            catch (err) {
                // do nothing
            }
        }
    };
})();