var ApiRequest = function () {
    function doRequest(options) {
        $.ajax({
            "type": options.type,
            "url": options.parameters === undefined || options.parameters === '' ? options.url : options.url + '/?' + options.parameters,
            "cache": false,
            "async": options.async === undefined ? true : options.async,
            "timeout": 600000,
            "dataType": options.dataType === undefined ? "json" : options.dataType,
            "data": options.data,
            "contentType": options.contentType,
            "success": function (data, status) {
                if (options.success)
                    options.success.call(this, data);
            },
            "error": function (xhr, status, errorThrown) {
                if (options.error)
                    options.error.call(this, xhr.responseText);
            },
            "complete": function (xhr) {
                if (options.complete)
                    options.complete.call(this, xhr);
            }
        });
    }

    //允许创建多个访问请求
    var RequestCreate = function () {
        var request = {
            contentTypeJson: false,
            post: function (options) {
                doRequest({
                    "url": options.url,
                    "type": "post",
                    "parameters": options.parameters,
                    "async": options.async,
                    "dataType": options.dataType,
                    "data": this.contentTypeJson ? JSON.stringify(options.postData) : options.postData,
                    "contentType": this.contentTypeJson ? "application/json" : undefined,
                    "success": options.success,
                    "error": options.error,
                    "complete": options.complete
                });
            },
            get: function (options) {
                doRequest({
                    "url": options.url,
                    "type": "get",
                    "parameters": options.parameters,
                    "async": options.async,
                    "dataType": options.dataType,
                    "success": options.success,
                    "error": options.error,
                    "complete": options.complete
                });
            }
        };
        return request;
    };
    return RequestCreate;
}