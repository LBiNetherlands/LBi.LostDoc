var LostDoc = LostDoc || {};

new function (LostDoc) {
    LostDoc.Settings = function (defaultSettings) {
        if (defaultSettings) {
            $.extend(this, defaultSettings);
        }
        var storedSettings = localStorage.getItem('settings');
        if (storedSettings) {
            var parsedSettings = JSON.parse(storedSettings);
            $.extend(this, parsedSettings);
        }
    };

    LostDoc.Settings.prototype.save = function () {
        localStorage.setItem('settings', JSON.stringify(this));
    };
}(LostDoc);