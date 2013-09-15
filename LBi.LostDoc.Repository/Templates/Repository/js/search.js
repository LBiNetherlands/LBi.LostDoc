(function ($) {
    window.lostDoc.search = {
        //versioning
        version: 0.1,

        // Main settings...
        settings: {
            'debug': true,
            'form': 'div.search form',
            'input': 'div.search form input',
            'button': 'div.search form button',
            'results': 'div#search-results',
            'keyupTimeout': 2000
        },

        init: function () {
            this._form = $(this.settings.form);
            this._input = $(this.settings.input);
            this._button = $(this.settings.button);
            this._results = $(this.settings.results);
            this._searchUri = this._form.get(0).action;
            this._input.bind('keyup', this._input_keyUp.bind(this));

            console.log("_form", this._form);
            console.log("_input", this._input);
            console.log("_button", this._button);
            console.log("_results", this._results);
            console.log("_searchUri", this._searchUri);
        },

        _input_keyUp: function (e) {
            alert('woo: ' + this._searchUri);
            console.log("this", this);
            // set some timeout
            this._keyup_timeout = setTimeout('lostDoc.search._on_keyup_timeout', this.settings.keyupTimeout);
        },

        _on_keyup_timeout: function (e) {
            alert('timeout!');
            // check for input box data
            // do search, call _renderInstantResult on successful callback
        },

        _renderInstantResults: function (data) {
            var resultHtml = '';
            $.each(data, function (i, item) {
                // find some nice js templating language here
                resultHtml = '<h1>results</h1>';
            });

            this._results.html(resultHtml);
        },
        
        _renderFullResults: function(data) {
            // make navigation pane xx% of the screen
            // lostDoc.setNavigationWidth(70);
        }
    };//end window
    $(function () {
        lostDoc.search.init();
    });
})(Zepto);
