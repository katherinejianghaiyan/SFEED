(function ($) {
    var mutiDownloadMethods = {
        _download: function (options) {
            var triggerDelay = (options && options.delay) || 100;
            var removeDelay = (options && options.removeDelay) || 1000;
            if (options.source === "server") {
                this.each(function (index, item) {
                    mutiDownloadMethods._createIFrame(item, index * triggerDelay, removeDelay);
                });
            };
            if (options.source === "local") {
                this.each(function (index, item) {
                    mutiDownloadMethods._createLink(item, index * triggerDelay, removeDelay);
                });
            };
        },
        _createIFrame: function (url, triggerDelay, removeDelay) {
            //动态添加iframe，设置src，然后删除
            setTimeout(function () {
                var frame = $('<iframe style="display: none;" class="multi-download"></iframe>');
                frame.attr('src', url);
                $(document.body).after(frame);
                setTimeout(function () {
                    frame.remove();
                }, removeDelay);
            }, triggerDelay);
        },
        //download属性设置
        _createLink: function (url, triggerDelay, removeDelay) {
            var aLink = document.createElement("a"),
                evt = document.createEvent("HTMLEvents");
            evt.initEvent("click");
            //需要添加属性，不需要设置文件名
            aLink.download = "";
            aLink.href = url;
            aLink.dispatchEvent(evt);
        }
    };

    $.fn.multiDownload = function (options) {
        mutiDownloadMethods._download.apply(this, arguments);
    };
})(jQuery);