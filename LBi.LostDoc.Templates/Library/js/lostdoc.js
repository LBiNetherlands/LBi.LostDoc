var LostDoc = LostDoc || {};

new function (LostDoc) {
    LostDoc.App = function () {
        //versioning
        this.version = 0.1;

     

        this._settings = new LostDoc.Settings({});

        this._layout = new LostDoc.Layout({
                'handleSelector': '.handle',
                'containerSelector': '#wrapper',
                'leftColumnSelector': 'div.left-col-outer',
                'rightColumnSelector': 'div.right-col-outer',
                'resizingClass': 'resizing'
            },
            this._settings);

        this._nav = new LostDoc.Nav({});

    };

    //this.App.prototype.initialize = function (arg1, arg2) {
    //    // this should be called explicitly (can also be called implicitly
    //    // from the constructor but in that case you have to check whether
    //    // the args are available, as it will also be called on subclassing
    //    // then (!!!))

    //    // if this would be defined inside the constructor, it would be shared
    //    // among 'subclasses'
    //    this.foo = [];

    //    // static method (this you can use without having to instantiate the
    //    // 'class')
    //    this.bar = function () {
    //        alert(arg1 + ' - ' + arg2);
    //    }
    //};

    //this.Dummy.prototype.foo = function () {
    //    // some function
    //};

}(LostDoc); // note that the namespace is instantiated immediately

app = new LostDoc.App();


//(function ($) {
//    window.LostDoc = {
//        //versioning
//        version: 0.1,

//        // Main settings...
//        settings: {
//            'debug': true,
//            'handleSelector': '.handle',
//            'containerSelector': '#wrapper',
//            'leftColumnSelector': 'div.left-col-outer',
//            'rightColumnSelector': 'div.right-col-outer',
//            'autoHeightSelector': '.auto-height',
//            'resizingClass': 'resizing'
//        },

//        _userSettings: {
//            leftColWidth: 20
//        },
//        _storeSettings: function () {
//            localStorage.setItem('settings', JSON.stringify(this._userSettings));
//        },
//        _loadSettings: function () {
//            var rawSettings = localStorage.getItem('settings');
//            if (rawSettings) {
//                this._userSettings = JSON.parse(rawSettings);
//            }
//        },
//        _setLeftColWidth: function (value) {
//            this._userSettings.leftColWidth = value;
//            this._storeSettings();
//        },
//        _getLeftColWidth: function () {
//            return this._userSettings.leftColWidth;
//        },

//        init: function () {
//            this._handle = $(this.settings['handleSelector']);
//            //this.leftColumn = $(this.settings['leftColumnSelector']);
//            //this.rightColumn = $(this.settings['rightColumnSelector']);
//            this._autoHeightElements = $(this.settings['autoHeightSelector']);

//            this._loadSettings();
//            this._updateWindowHeight();
//            $(window).resize(this._updateWindowHeight.bind(this));

//            this.resizer.init(this._getLeftColWidth(),
//                              this.settings['handleSelector'],
//                              this.settings['containerSelector'],
//                              this.settings['leftColumnSelector'],
//                              this.settings['rightColumnSelector']);

//            this.resizer.onResized = function (newLeftColWidth) {
//                this._setLeftColWidth(newLeftColWidth);
//                $('body').removeClass(this.settings['resizingClass']);
//            }.bind(this);

//            this.resizer.onResize = function () {
//                $('body').addClass(this.settings['resizingClass']);
//            }.bind(this);
//        },

//        _updateWindowHeight: function () {
//            var windowHeight = $(window).height();

//            this._autoHeightElements.css({
//                'height': windowHeight
//            });
//        },

//        resizer: {
//            init: function (leftColWidthPercent, handleSelector, containerSelector, leftColumnSelector, rightColumnSelector) {
//                this._leftColWidthPercent = leftColWidthPercent;

//                this._handleSelector = handleSelector;
//                this._containerSelector = containerSelector;
//                this._leftcolumnSelector = leftColumnSelector;
//                this._rightColumnSelector = rightColumnSelector;

//                this._handle = $(this._handleSelector);
//                this._container = $(this._containerSelector);
//                this._leftColumn = $(this._leftcolumnSelector);
//                this._rightColumn = $(this._rightColumnSelector);

//                $(window).resize(this._resize.bind(this));

//                var mouseDown = this._handle.bind('mousedown', function (e) {
//                    if (this.onResize) {
//                        this.onResize();
//                    }
//                    this._oldClientX = e.clientX;

//                    this._leftColtWidth = this._leftColumn.width();

//                    var mouseMove = $(document).bind('mousemove', function (e) {
//                        var offset = this._oldClientX - e.clientX;

//                        // TODO move constants into settings
//                        this._leftColWidthPercent = Math.max(10, Math.min(70, this._pxToPercentage(this._leftColtWidth - offset)));
//                        // console.log(offset, this._leftColWidthPercent);
//                        this._update();
//                    }.bind(this));
//                }.bind(this));

//                var mouseUp = $(document).bind('mouseup', function () {
//                    $(document).unbind('mousemove');

//                    if (this.onResized) {
//                        this.onResized(this._leftColWidthPercent);
//                    }
//                }.bind(this));

//                this._update();
//            },

//            _resize: function (ev) {
//                var leftColWidth = this._container.width() * (this._leftColWidthPercent / 100);
//                var newLeftColWidthPercent = this._pxToPercentage(leftColWidth);

//                if (newLeftColWidthPercent != this._leftColWidthPercent) {
//                    this._leftColWidthPercent = newLeftColWidthPercent;
//                    this._update();
//                }

//                var windowHeight = $(window).height();

//                var handleTop = Math.round(windowHeight * .4);

//                this._handle.css({
//                    'top': handleTop + 'px'
//                });

//            },

//            _pxToPercentage: function (px) {
//                var containerWidth = this._container.width();
//                var temp = parseFloat(px) / parseFloat(containerWidth);
//                var percentage = Math.round(10000 * temp) / 100;
//                return percentage;
//            },

//            _update: function () {
//                this._leftColumn.css('width', this._leftColWidthPercent + '%');
//                this._rightColumn.css('width', (100 - this._leftColWidthPercent) + '%');
//                this._handle.css('left', this._leftColWidthPercent + '%');
//            },

//            onResized: function (leftColWidthPercent) {
//            },
//            onResize: function () {
//            }
//        }
//    };//end window
//    $(function () {
//        LostDoc.init();
//    });
//})(Zepto);
