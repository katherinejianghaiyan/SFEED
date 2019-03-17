String.prototype.replaceAll = function (s1, s2) {
    var r = new RegExp(s1.replace(/([\(\)\[\]\{\}\^\$\+\-\*\?\.\"\'\|\/\\])/g, "\\$1"), "ig");
    return this.replace(r, s2);
}

function StringBuilder() {
    this._strings = new Array();
}

//append方法
StringBuilder.prototype.append = function (str) {
    this._strings.push(str);
    return this;
};

StringBuilder.prototype.appendArray = function (arr) {
    this._strings.concat(arr);
    return this;
};

StringBuilder.prototype.insert = function (index, str) {
    this._strings.splice(index, 0, str);
    return this;
}

//toString方法
StringBuilder.prototype.toString = function () {
    return this._strings.join('');
};

StringBuilder.prototype.toArray = function () {
    return this._strings;
}

//获取长度
StringBuilder.prototype.length = function () {
    return this._strings.length;
};

//首位清除空值
String.prototype.trim = function () {
    return this.replace(/(^\s*)|(\s*$)/g, "");
};

$.extend({
    mathRound: function (number, digital) {
        var n = parseFloat(number);
        if (isNaN(n)) return 0;
        var y = 1;
        if (!isNaN(parseInt(digital))) y = digital;
        var e = Math.pow(10, y);
        return Math.round(n * e) / e;
    },
    jsonCheck: function (str) {
        var _strings = str;
        if (str.indexOf('"') > -1) _strings = _strings.replaceAll('"', '\\\"');
        if (str.indexOf('\r') > -1) _strings = _strings.replaceAll('\r', '\\r');
        if (str.indexOf('\n') > -1) _strings = _strings.replaceAll('\n', '\\n');
        if (str.indexOf('\t') > -1) _strings = _strings.replaceAll('\t', '\\t');
        if (str.indexOf('\\') > -1) _strings = _strings.replaceAll('\\', '\\\\');
        return _strings;
    }
});







