var LostDoc = LostDoc || {};

new function (LostDoc) {

    LostDoc.Layout = function (settings, userSettings) {

        this._settings = {
            'handleSelector': '.handle',
            'containerSelector': '#wrapper',
            'leftColumnSelector': 'div.left-col-outer',
            'rightColumnSelector': 'div.right-col-outer',
            'resizingClass': 'resizing'
        };

        if (settings != null) {
            $.extend(this._settings, settings);
        }

        if (!userSettings.layout) {
            userSettings.layout = {
                leftColWidth: 20
            };
        }

        this._userSettings = userSettings;

        this._leftColWidthPercent = this._userSettings.layout.leftColWidth;

        this._handle = $(this._settings.handleSelector);
        this._container = $(this._settings.containerSelector);
        this._leftColumn = $(this._settings.leftColumnSelector);
        this._rightColumn = $(this._settings.rightColumnSelector);

        $(window).resize(this._resize.bind(this));

        var mouseDown = this._handle.bind('mousedown', function (e) {
            $('body').addClass(this._settings.resizingClass);

            this._oldClientX = e.clientX;

            this._leftColtWidth = this._leftColumn.width();

            var mouseMove = $(document).bind('mousemove', function (e) {
                var offset = this._oldClientX - e.clientX;

                // TODO move constants into settings
                this._leftColWidthPercent = Math.max(10, Math.min(70, this._pxToPercentage(this._leftColtWidth - offset)));
                // console.log(offset, this._leftColWidthPercent);
                this._update();
            }.bind(this));
        }.bind(this));

        var mouseUp = $(document).bind('mouseup', function () {
            $(document).unbind('mousemove');

            this._userSettings.layout.leftColWidth = this._leftColWidthPercent;
            this._userSettings.save();
            $('body').removeClass(this._settings.resizingClass);
        }.bind(this));

        this._update();
    };

    LostDoc.Layout.prototype._resize = function (ev) {
        var leftColWidth = this._container.width() * (this._leftColWidthPercent / 100);
        var newLeftColWidthPercent = this._pxToPercentage(leftColWidth);

        if (newLeftColWidthPercent != this._leftColWidthPercent) {
            this._leftColWidthPercent = newLeftColWidthPercent;
            this._update();
        }

        var windowHeight = $(window).height();

        var handleTop = Math.round(windowHeight * .4);

        this._handle.css({
            'top': handleTop + 'px'
        });
    };

    LostDoc.Layout.prototype._pxToPercentage = function (px) {
        var containerWidth = this._container.width();
        var temp = parseFloat(px) / parseFloat(containerWidth);
        var percentage = Math.round(10000 * temp) / 100;
        return percentage;
    };

    LostDoc.Layout.prototype._update = function () {
        this._leftColumn.css('width', this._leftColWidthPercent + '%');
        this._rightColumn.css('width', (100 - this._leftColWidthPercent) + '%');
        this._handle.css('left', this._leftColWidthPercent + '%');
    };
}(LostDoc); // note that the namespace is instantiated immediately
