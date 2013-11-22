﻿/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

var LostDoc = LostDoc || {};

new function (LostDoc) {

    LostDoc.Layout = function (settings, userSettings) {

        this._settings = {
            'handleSelector': '.handle',
            'containerSelector': '#wrapper',
            'leftColumnSelector': 'div.left-col-outer',
            'rightColumnSelector': 'div.right-col-outer',
            'explicitWidthSelector': '.explicit-width',
            'resizingClass': 'resizing',
            'detachableContentSelector': '.detachable',
            'detachedContentClass': 'detached',
            'detachedContentTop': '15'
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
        this._leftContent = this._leftColumn.find(this._settings.detachableContentSelector);
        this._rightContent = this._rightColumn.find(this._settings.detachableContentSelector);
        this._leftContentTop = this._leftContent.offset().top;
        this._rightContentTop = this._rightContent.offset().top;

        this._leftDetachedContentTop = Math.min(this._leftContentTop, this._settings.detachedContentTop);
        this._rightDetachedContentTop = Math.min(this._rightContentTop, this._settings.detachedContentTop);

        $(window).on("resize", this._onResize.bind(this));
        $(window).on("scroll", this._onScroll.bind(this));

        var mouseDown = this._handle.bind('mousedown', function (e) {
            $('body').addClass(this._settings.resizingClass);

            this._oldClientX = e.clientX;

            this._leftColtWidth = this._leftColumn.width();

            var mouseMove = $(document).bind('mousemove', function (e) {
                var offset = this._oldClientX - e.clientX;

                // TODO move constants into settings
                this._leftColWidthPercent = Math.max(10, Math.min(70, this._pxToPercentage(this._leftColtWidth - offset)));
                // console.log(offset, this._leftColWidthPercent);
                this._resize();
            }.bind(this));
        }.bind(this));

        var mouseUp = $(document).bind('mouseup', function () {
            $(document).unbind('mousemove');

            this._userSettings.layout.leftColWidth = this._leftColWidthPercent;
            this._userSettings.save();
            $('body').removeClass(this._settings.resizingClass);
        }.bind(this));

        this._resize();
    };

    LostDoc.Layout.prototype._onResize = function (ev) {
        var leftColWidth = this._container.width() * (this._leftColWidthPercent / 100);
        var newLeftColWidthPercent = this._pxToPercentage(leftColWidth);

        if (newLeftColWidthPercent != this._leftColWidthPercent) {
            this._leftColWidthPercent = newLeftColWidthPercent;
            this._resize();
        }
        this._scroll();
    };

    LostDoc.Layout.prototype._onScroll = function (ev) {
        this._scroll();
    };


    LostDoc.Layout.prototype._pxToPercentage = function (px) {
        var containerWidth = this._container.width();
        var temp = parseFloat(px) / parseFloat(containerWidth);
        var percentage = Math.round(10000 * temp) / 100;
        return percentage;
    };

    LostDoc.Layout.prototype._resize = function () {
        var leftWidth = this._leftColWidthPercent + '%';
        var rightWidth = (100 - this._leftColWidthPercent) + '%';

        this._handle.css('left', leftWidth);

        this._leftColumn.css('width', leftWidth);
        if (this._leftContent && this._isDetached(this._leftContent)) {
            this._leftContent.css('width', leftWidth);
        }
        
        this._rightColumn.css('width', rightWidth);
        if (this._rightContent && this._isDetached(this._rightContent)) {
            this._rightContent.css('width', rightWidth);
        }
    };

    LostDoc.Layout.prototype._scroll = function () {

        var leftWidth = this._leftColWidthPercent + '%';
        var rightWidth = (100 - this._leftColWidthPercent) + '%';

        if (this._leftContent) {
            this._sticky(this._leftContent, this._leftContentTop, this._leftDetachedContentTop, leftWidth);
        }

        if (this._rightContent) {
            this._sticky(this._rightContent, this._rightContentTop, this._rightDetachedContentTop, rightWidth);
        }
    };

    LostDoc.Layout.prototype._sticky = function (content, originalContentTop, detachedContentTop, contentWidth) {
        var wndHeight = $(window).height();
        var scrollTop = $(window).scrollTop();
        var contentHeight = content.height();

        if (contentHeight < wndHeight) {
            if (!this._isDetached(content) && scrollTop > originalContentTop - detachedContentTop) {
                this._detach(content, detachedContentTop, contentWidth);
                console.log("detaching", content);
            } else if (this._isDetached(content) && scrollTop <= originalContentTop - detachedContentTop) {
                this._attach(content);
                console.log("attaching", content);
            }
        } else {
            this._attach(content);
        }
    };

    LostDoc.Layout.prototype._isDetached = function (detachable) {
        return detachable.hasClass(this._settings.detachedContentClass);
    };

    LostDoc.Layout.prototype._attach = function (detachable) {
        detachable.removeClass(this._settings.detachedContentClass);
        detachable.css('top', null);
        detachable.css('width', null);
    };

    LostDoc.Layout.prototype._detach = function (detachable, top, width) {
        detachable.addClass(this._settings.detachedContentClass);
        detachable.css('top', top + 'px');
        detachable.css('width', width + '%');
    };
}(LostDoc); // note that the namespace is instantiated immediately
