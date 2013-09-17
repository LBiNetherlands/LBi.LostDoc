(function ($) {
    window.lostDoc.search = {
        //versioning
        version: 0.1,

        // Main settings...
        settings: {
            'debug': true,
            'form': 'div.search form',
            'input': 'div.search form input',

            'keyupTimeout': 1000,
            
            'instantResultCount': 5,
            'fullResultCount': 20
        },

        init: function () {
            this._form = $(this.settings.form);
            this._input = $(this.settings.input);

            this._searchUri = this._form.get(0).action;

            this._input.bind('keyup', this._input_keyup.bind(this));
            this._input.bind('keydown', this._input_keydown.bind(this));
            $(window).bind('click', this._hideInstant.bind(this));
            $(window).bind('keydown', this._hideFull.bind(this));
            this._form.bind('click', function (ev) { ev.stopPropagation(); });
            this._form.submit(this._performSearch.bind(this));


            ko.applyBindings(this.viewModel);
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
            if (this.viewModel.instant()) {
                if (e.keyCode == 27) {
                    if (this.viewModel.instant().selected()) {
                        this.viewModel.instant().selected(null);
                    } else {
                        this.viewModel.instant(null);
                    }
                } else if (e.keyCode == 38) { // up
                    this.viewModel.instant().selectPrev();
                } else if (e.keyCode == 40) { // down
                    this.viewModel.instant().selectNext();
                } else if (e.keyCode == 13) { // enter
                    if (this.viewModel.instant().selected()) {
                        window.location.href = this.viewModel.instant().selected().url;
                    } else {
                        this._performSearch();
                    }
                    return false;
                }
            }
        },


        _hideInstant: function (e) {
            this.viewModel.instant(null);
        },

        _hideFull: function (e) {
            if (e.keyCode == 27 && !this.viewModel.instant()) {
                this.viewModel.resultSet(null);
            }
        },

        _on_keyup_timeout: function (e) {
            console.log("_on_keyup_timeout", this);
            this._keyup_timeout = null;
            var terms = this._input.val();

            this.viewModel.instant(new this._resultSet(this._searchUri, terms, this.settings.instantResultCount));
        },

        _performSearch: function (e) {

            // cancel old timeout
            if (this._keyup_timeout) {
                clearTimeout(this._keyup_timeout);
            }

            var terms = this._input.val();

            this.viewModel.instant(null);
            this.viewModel.resultSet(new this._resultSet(this._searchUri, terms, this.settings.fullResultCount));

            return false;
        },

        viewModel: {
            resultSet: ko.observable(),
            instant: ko.observable(),
        },

        _resultSet: function (searchUri, query, pageSize) {

            this._pageSize = pageSize;
            this._searchUri = searchUri;
            this.query = query;
            this.hitCount = ko.observable();
            this.results = ko.observableArray([]);

            this.selected = ko.observable();
            this._selected = -1;

            this.selectNext = function () {
                if (this.selected()) {
                    this._selected = Math.min(this._selected + 1, this.results().length - 1);
                } else {
                    // set first
                    this._selected = 0;
                }
                this.selected(this.results()[this._selected]);
            };

            this.selectPrev = function () {
                if (this.selected()) {
                    this._selected = Math.max(this._selected - 1, 0);
                } else {
                    // set last
                    this._selected = this.result().length - 1;
                }
                this.selected(this.results()[this._selected]);
            };

            this.loading = ko.computed(function () {
                return this.hitCount() == null;
            }, this);

            this.hasMore = ko.computed(function () {
                return this.hitCount() > this.results().length;
            }, this);

            this.noResults = ko.computed(function () {
                return this.hitCount() === 0;
            }, this);

            this.hasResults = ko.computed(function () {
                return this.hitCount() > 0;
            }, this);


            this._bindResults = function (data) {
                this.hitCount(data.HitCount);
                var results = data.Results.map(function (value, index, array) {
                    return {
                        title: value.Title,
                        blurb: value.Blurb,
                        url: value.Url,
                    };
                }, this);
                this.results(this.results().concat(results));
            };

            this.fetchNext = function () {

                // perform ajax request
                $.ajax({
                    type: 'GET',
                    url: this._searchUri + '/' + this.query + '?count=' + this._pageSize + '&offset=' + this.results().length,
                    // data to be added to query string:
                    //data: { name: 'Zepto.js' },
                    // type of data we are expecting in return:
                    dataType: 'json',
                    timeout: 5000,
                    //context: $('body'),
                    success: this._bindResults.bind(this),
                    error: function (xhr, type) {
                        console.error('Ajax error!', xhr, type);
                    }
                });
            };

            // initial load
            this.fetchNext();
        }
    };//end window
    $(function () {
        lostDoc.search.init();
    });
})(Zepto);
