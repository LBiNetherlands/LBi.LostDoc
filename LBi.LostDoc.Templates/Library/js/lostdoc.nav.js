var LostDoc = LostDoc || {};

new function (LostDoc) {

    LostDoc.Nav = function (settings) {
        this._settings = {
            'navSelector': 'nav',
        };

        if (settings != null) {
            $.extend(this._settings, settings);
        }

        this._nav = $(this._settings.navSelector);

        this._navTop = this._nav.position().top;

        $(window).on('scroll', this._onScroll.bind(this));
        $(window).on('resize', this._onResize.bind(this));
    };


    LostDoc.Nav.prototype._onScroll = function (ev) {

        var scrollTop = $(window).scrollTop();
        if (scrollTop > this._navTop) {
            this._nav.css({ 'position': 'fixed', top: this._navTop + 'px' });
        }
        console.log("scrollTop()", scrollTop);
        console.log("scroll", ev);
    };


    LostDoc.Nav.prototype._onResize = function (ev) {
        console.log("resize", ev);
    };

}(LostDoc); // note that the namespace is instantiated immediately
