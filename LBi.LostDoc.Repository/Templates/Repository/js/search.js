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
            'content': 'main',

            'fullResults': 'div#full-results',
            'fullResultsList': '.search-results',
            'fullResultsNone': '.no-results',
            'fullResultsMore': 'button',
            'fullResultCount': 20
        },

        init: function () {
            this._form = $(this.settings.form);
            this._input = $(this.settings.input);
            this._button = $(this.settings.button);
            this._instantResults = $(this.settings.instantResults);
            this._fullResults = $(this.settings.fullResults);
            this._fullResultsList = this._fullResults.find(this.settings.fullResultsList);
            this._fullResultsNone = this._fullResults.find(this.settings.fullResultsNone);
            this._fullResultsMore = this._fullResults.find(this.settings.fullResultsMore);
            this._content = $(this.settings.content);
            this._searchUri = this._form.get(0).action;

            this._input.bind('keyup', this._input_keyup.bind(this));
            this._input.bind('keydown', this._input_keydown.bind(this));
            $(window).bind('click', this._hideInstant.bind(this));
            $(window).bind('keydown', this._hideFull.bind(this));
            this._form.bind('click', function (ev) { ev.stopPropagation(); });
            this._form.submit(this._performSearch.bind(this));
            this._instantSelected = -1;

            this._fullResultsMore.bind('click', this._performSearch.bind(this));

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

        _hideFull: function (e) {
            if (e.keyCode == 27 && !this._fullResults.hasClass('hidden')) {
                this._fullResults.addClass('hidden');
                this._content.removeClass("hidden");
                this._fullResultsQuery = null;
            }
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
                timeout: 5000,
                //context: $('body'),
                success: this._renderInstantResults.bind(this),
                error: function (xhr, type) {
                    alert('Ajax error!');
                }
            });
        },

        _performSearch: function (e) {

            // cancel old timeout
            if (this._keyup_timeout) {
                clearTimeout(this._keyup_timeout);
            }
            this._instantResults.addClass("hidden");

            var terms = this._input.val();

            this.viewModel.resultSet(new this._resultSet(this._searchUri, terms));

            //$.ajax({
            //    type: 'GET',
            //    url: this._searchUri + '/' + terms + '?count=' + this.settings.fullResultCount + offset,
            //    // data to be added to query string:
            //    //data: { name: 'Zepto.js' },
            //    // type of data we are expecting in return:
            //    dataType: 'json',
            //    timeout: 5000,
            //    //context: $('body'),
            //    success: this._renderFullResults.bind(this),
            //    error: function (xhr, type) {
            //        console.error('Ajax error!', xhr, type);
            //    }
            //});
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
            //var resultHtml = '';
            //if (data.HitCount > 0) {
            //    $.each(data.Results, function (i, item) {
            //        resultHtml += '<li>';
            //        resultHtml += '<a href="' + item.Url + '">';
            //        resultHtml += '<h4>';
            //        resultHtml += item.Title;
            //        resultHtml += '</h4>';
            //        resultHtml += '</a>';
            //        resultHtml += '<p>';
            //        resultHtml += item.Blurb;
            //        resultHtml += '</p>';
            //        resultHtml += '</li>';
            //    });

            //    if (data.HitCount > this._fullResultsList.find('li').length) {
            //        this._fullResultsMore.removeClass('hidden');
            //    } else {
            //        this._fullResultsMore.addClass('hidden');
            //    }

            //    this._fullResultsList.removeClass('hidden');
            //    this._fullResultsNone.addClass('hidden');
            //} else {
            //    this._fullResultsList.addClass('hidden');
            //    this._fullResultsNone.removeClass('hidden');
            //}
            //if (this._fullResultsQuery) {
            //    this._fullResultsList.append(resultHtml);
            //} else {
            //    this._fullResultsList.html(resultHtml);
            //}
            //this._content.addClass("hidden");
            //this._fullResults.removeClass("hidden");
            console.log("this", this);

        },

        viewModel: {
            resultSet: ko.observable(),
        },

        _resultSet: function (searchUri, query) {
            
            this._searchUri = searchUri;
            this.query = query;
            this.hitCount = ko.observable();
            this.results = ko.observableArray([]);

            var self = this;
 
            this.loading = ko.computed(function () {
                return self.hitCount() == null;
            }, this);

            this.hasMore = ko.computed(function() {
                return self.hitCount() > self.results().length;
            }, this);
            
            this.noResults = ko.computed(function () {
                return self.hitCount() === 0;
            }, this);
            
            this.hasResults = ko.computed(function () {
                return self.hitCount() > 0;
            }, this);
            

            this._bindResults = function (data) {
                this.hitCount(data.HitCount);
                var results = data.Results.map(function(value, index, array) {
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
                    url: this._searchUri + '/' + this.query + '?count=20' + '&offset=' + this.results().length,
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
