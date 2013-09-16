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
            this.updateWindowHeight();
            $(window).resize(this.updateWindowHeight.bind(this));
            this.openLocalStorage();
            this.initColumnResizer();
        },

        updateWindowHeight: function () {
            var windowHeight = $(window).height();

            //var minHeight = this.autoHeightElements.reduce(function (aggregate, item, index, array) {
            //    return Math.max(aggregate, $(item).height());
            //}, windowHeight);

            //this.autoHeightElements.css({
            //    'min-height': minHeight
            //});
            
            this.autoHeightElements.css({
                'height': windowHeight
            });

            //this.autoHeightElements.each(function (i, item) {
            //    $(item).find('.auto-min-height').css('min-height', windowHeight);
            //});

            var handleTop = Math.round(windowHeight * .4);

            this.handle.css({
                'top': handleTop + 'px'
            });
        },

        pxToPercentage: function (px) {
            this.wrapperWidth = $('#wrapper').width();
            var temp = parseFloat(px) / parseFloat(this.wrapperWidth);
            var percentage = Math.round(1000 * temp) / 10;
            return percentage;
        },

        storeColumnWidth: function () {
            var rawValue = JSON.stringify(this.colLeftWidthPerc);
            localStorage.setItem("columnWidth", rawValue);
        },

        openLocalStorage: function () {
            var rawValue = localStorage.getItem("columnWidth");
            if (rawValue) {
                this.colLeftWidthPerc = JSON.parse(rawValue);
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
                
                this.column = e.clientX;
                
                this.colLeftWidth = this.leftColumn.width();
                var mouseMove = $(document).bind('mousemove', function (e) {
                    this.moved = this.column - e.clientX;
                    //now resize the columns
                    var newColLeftWidth = this.colLeftWidth - this.moved;
                    var colLeftWidthPercent = this.pxToPercentage(newColLeftWidth);
                    this.colLeftWidthPerc = Math.max(10, Math.min(70, colLeftWidthPercent));
                    this.colRightWidthPerc = 100 - colLeftWidthPercent;
                    
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
