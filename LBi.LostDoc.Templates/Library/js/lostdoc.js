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
            localStorage.setItem("columnWidth", this.colLeftWidthPerc);
        },

        openLocalStorage: function () {
            this.colLeftWidthPerc = localStorage.getItem("columnWidth");
            this.colRightWidthPerc = 100 - this.colLeftWidthPerc;
            if (this.colLeftWidthPerc > 0 && this.colRightWidthPerc) {
                this.setColumnWidth(true);
            }
        },

        setColumnWidth: function (init) {
            var total = this.colLeftWidthPerc + this.colRightWidthPerc;
            //console.log("this.colLeftWidthPerc", this.colLeftWidthPerc);
            //console.log("this.colRightWidthPerc", this.colRightWidthPerc);
            

            if (total == 100 && this.colLeftWidthPerc > 10 && this.colRightWidthPerc > 30 || init === true) {
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
                    
                    var colLeftWidth = this.colLeftWidth - this.moved;
                    var colLeftWidthPercent = this.pxToPercentage(colLeftWidth);
                    
                    //console.log("this.colLeftWidthPerc", this.colLeftWidthPerc);
                    //console.log("this.colRightWidthPerc", this.colRightWidthPerc);

                    if (colLeftWidthPercent > 10 && colRightWidthPercent > 30) {
                        // commit values
                        this.colRightWidthPerc = colRightWidthPercent;
                        this.colLeftWidthPerc = colLeftWidthPercent;
                        this.setColumnWidth();
                    }
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
