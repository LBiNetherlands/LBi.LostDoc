(function ($) {
    window.lostDoc = {
        //versioning
        version: 0.1,

        // Main settings...
        settings: {
            'debug': true,
            'handleClass': 'handle',
            'leftColumnSelector': 'div.main-navigation',
            'rightColumnSelector': 'main',
            'grabClass': 'grabbing',
            'autoHeightSelector': '.auto-height'
        },

        init: function () {
            this.editor = [];
            this.handle = $('.' + this.settings['handleClass']);
            this.leftColumn = $(this.settings['leftColumnSelector']);
            this.rightColumn = $(this.settings['rightColumnSelector']);
            this.autoHeightElements = $(this.settings['autoHeightSelector']);
            this.setColumnHeight();
            this.openLocalStorage();
            this.initColumnResizer();
        },

        setColumnHeight: function () {
            var height = $(window).height();
            this.autoHeightElements.css({
                'min-height': height
            });
            //this.rightColumn.css({
            //    'min-height': height
            //});
            //this.leftColumn.css({
            //    'min-height': height
            //});
        },

        pxToPercentage: function (px) {
            this.wrapperWidth = $('#wrapper').width();
            var temp = parseFloat(px) / parseFloat(this.wrapperWidth);
            var percentage = Math.round(1000 * temp) / 10;
            return percentage;
        },

        storeColumnWidth: function () {
            localStorage.setItem("columnWidth", JSON.stringify(this.colLeftWidthPerc));
        },

        openLocalStorage: function () {
            var rawValue = JSON.parse(localStorage.getItem("columnWidth"));
            if (rawValue) {
                this.colLeftWidthPerc = parseFloat();
                console.log("load this.colLeftWidthPerc", this.colLeftWidthPerc);
                this.colRightWidthPerc = 100 - this.colLeftWidthPerc;
                this.setColumnWidth();
            }
        },

        setColumnWidth: function () {
            var total = this.colLeftWidthPerc + this.colRightWidthPerc;

            if (total == 100) {
                this.leftColumn.css({
                    'width': this.colLeftWidthPerc + '%'
                });
                this.rightColumn.css({
                    'width': this.colRightWidthPerc + '%'
                });
            }
        },

        initColumnResizer: function (e) {
            var mouseDown = this.handle.bind('mousedown', function (e) {
                $('body').addClass(this.settings['grabClass']);
                //this.column = this.handle.offset().left;
                this.column = e.clientX;
                this.colRightWidth = this.rightColumn.width();
                this.colLeftWidth = this.leftColumn.width();
                var mouseMove = $(document).bind('mousemove', function (e) {
                    this.moved = this.column - e.clientX;
                    //now resize the columns
                    var colRightWidth = (this.colRightWidth - 1) + this.moved;
                    var colRightWidthPercent = this.pxToPercentage(colRightWidth);

                    var colLeftWidthPercent = 100 - colRightWidthPercent;

                    //console.log("this.colLeftWidthPerc", this.colLeftWidthPerc);
                    //console.log("this.colRightWidthPerc", this.colRightWidthPerc);

                    // TODO figure out what min/max is in js
                    if (colLeftWidthPercent < 10) {
                        colLeftWidthPercent = 10;
                        colRightWidthPercent = 100 - colLeftWidthPercent;
                    }

                    if (colRightWidthPercent < 30) {
                        colRightWidthPercent = 30;
                        colLeftWidthPercent = 100 - colRightWidthPercent;
                    }
                    
                    // commit values
                    this.colRightWidthPerc = colRightWidthPercent;
                    this.colLeftWidthPerc = colLeftWidthPercent;
                    this.setColumnWidth();
                    
                }.bind(this));
            }.bind(this));
            var mouseUp = $(document).bind('mouseup', function () {
                $(document).unbind('mousemove');
                $('body').removeClass(this.settings['grabClass']);
                this.storeColumnWidth();
            }.bind(this));
        },
    };//end window
    $(function () {
        lostDoc.init();
    });
})(Zepto);
