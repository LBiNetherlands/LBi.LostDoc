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
            'instantResults': 'div#instant-results',
            'keyupTimeout': 1000,
            'instantResultCount': 5,
            'fullResults': 'div.main-content',
            'fullResultCount': 20
        },

        init: function () {
            this._form = $(this.settings.form);
            this._input = $(this.settings.input);
            this._button = $(this.settings.button);
            this._instantResults = $(this.settings.instantResults);
            this._fullResults = $(this.settings.fullResults);
            this._searchUri = this._form.get(0).action;
            this._input.bind('keyup', this._input_keyup.bind(this));
            this._input.bind('keydown', this._input_keydown.bind(this));
            //this._input.bind('blur', this._input_blur.bind(this));
            $(window).bind('click', this._hideInstant.bind(this));
            this._form.bind('click', function (ev) { ev.stopPropagation(); });
            this._form.submit(this._performSearch.bind(this));
            console.log("_form", this._form);
            console.log("_input", this._input);
            console.log("_button", this._button);
            console.log("_instantResults", this._instantResults);
            console.log("_searchUri", this._searchUri);
            this._instantSelected = -1;
        },

        _input_keyup: function (e) {
            console.log("this", this);
            // set some timeout
            console.log("keyupTimeout", this.settings.keyupTimeout);

            // cancel old timeout
            if (this._keyup_timeout) {
                clearTimeout(this._keyup_timeout);
            }
            if (e.keyCode != 27 && e.keyCode != 38 && e.keyCode != 40 && e.keyCode != 13) {
                this._keyup_timeout = setTimeout(this._on_keyup_timeout.bind(this), this.settings.keyupTimeout);
            }
        },

        _input_keydown: function (e) {
            // escape
            if (e.keyCode == 27) {
                if (this._instantSelected == -1) {
                    this._instantResults.addClass("hidden");
                } else {
                    this._instantSelected = -1;
                    this._instantSelectResult(this._instantSelected);
                }
            } else if (e.keyCode == 38) { // up
                if (this._instantSelected > 0) {
                    this._instantSelected--;
                    this._instantSelectResult(this._instantSelected);
                }
            } else if (e.keyCode == 40) { // down
                if (this._instantSelected < (this._instantResultCount - 1)) {
                    this._instantSelected++;
                    this._instantSelectResult(this._instantSelected);
                }
            } else if (e.keyCode == 13) { // enter
                if (this._instantSelected == -1) {
                    this._performSearch();
                } else {
                    this._instantResults.find('li.selected a').each(function (i, item) {
                        window.location.href = $(item).attr('href');
                    });
                    return false;
                }
            }
        },

        _instantSelectResult: function (index) {
            this._instantResults.find('li').each(function (i, item) {
                if (index == i) {
                    $(item).addClass('selected');
                } else {
                    $(item).removeClass('selected');
                }
            });
        },

        _hideInstant: function (e) {
            this._instantResults.addClass("hidden");
        },

        _on_keyup_timeout: function (e) {
            console.log("_on_keyup_timeout", this);
            this._keyup_timeout = null;
            var terms = this._input.val();
            $.ajax({
                type: 'GET',
                url: this._searchUri + '/' + terms + '?count=' + this.settings.instantResultCount,
                // data to be added to query string:
                //data: { name: 'Zepto.js' },
                // type of data we are expecting in return:
                dataType: 'json',
                timeout: 300,
                //context: $('body'),
                success: this._renderInstantResults.bind(this),
                error: function (xhr, type) {
                    alert('Ajax error!');
                }
            });

            //alert('timeout!');
            // check for input box data
            // do search, call _renderInstantResult on successful callback
        },

        _performSearch: function (e) {

            // cancel old timeout
            if (this._keyup_timeout) {
                clearTimeout(this._keyup_timeout);
            }
            this._instantResults.addClass("hidden");

            var terms = this._input.val();
            $.ajax({
                type: 'GET',
                url: this._searchUri + '/' + terms + '?count=' + this.settings.fullResultCount,
                // data to be added to query string:
                //data: { name: 'Zepto.js' },
                // type of data we are expecting in return:
                dataType: 'json',
                timeout: 300,
                //context: $('body'),
                success: this._renderFullResults.bind(this),
                error: function (xhr, type) {
                    alert('Ajax error!');
                }
            });
            return false;
        },

        _renderInstantResults: function (data) {
            console.log("data", data);
            var resultHtml = '';
            //resultHtml = '<h1>results</h1>';
            if (data.HitCount > 0) {
                resultHtml += '<ul>';
                $.each(data.Results, function (i, item) {
                    // find some nice js templating language here
                    resultHtml += '<li>';
                    resultHtml += '<a href="' + item.Url + '">';
                    resultHtml += '<h4>';
                    resultHtml += item.Title;
                    resultHtml += '</h4>';
                    resultHtml += '<p>';
                    resultHtml += item.Blurb;
                    resultHtml += '</p>';
                    resultHtml += '</a>';
                    resultHtml += '</li>';
                });
                resultHtml += '</ul>';

                this._instantResults.html(resultHtml);
                this._instantSelected = -1;
                this._instantResultCount = data.Results.length;
                this._instantSelectResult(this._instantSelected);
                this._instantResults.find('a').bind('click', function (e) {
                    window.location.href = $(this).attr('href');
                    return false;
                });
            } else {
                this._instantResults.html("<h4>no results</h4>");
            }
            this._instantResults.removeClass("hidden");
        },

        _renderFullResults: function (data) {
            var resultHtml = '';
            resultHtml = '<h1>Results</h1>';
            if (data.HitCount > 0) {
                resultHtml += '<ul class="search-results">';
                $.each(data.Results, function (i, item) {
                    // find some nice js templating language here
                    resultHtml += '<li>';
                    resultHtml += '<a href="' + item.Url + '">';
                    resultHtml += '<h4>';
                    resultHtml += item.Title;
                    resultHtml += '</h4>';
                    resultHtml += '</a>';
                    resultHtml += '<p>';
                    resultHtml += item.Blurb;
                    resultHtml += '</p>';
                    resultHtml += '</li>';
                });
                resultHtml += '</ul>';

                this._fullResults.html(resultHtml);
            } else {
                this._fullResults.html("<h1>no results</h1>");
            }
            this._fullResults.removeClass("hidden");
        }
    };//end window
    $(function () {
        lostDoc.search.init();
    });
})(Zepto);
