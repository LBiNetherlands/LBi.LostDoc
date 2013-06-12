/*
 * Copyright 2013 LBi Netherlands B.V.
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

$(function () {
    $("a.overlay").each(function () {
        $(this).colorbox({
            width: '70%',
            onComplete: function () {
                LostDoc.Tabs.Init($('div.tabs').get(0));
                var tree = $('div.output-tree');

                tree.fileTree(
                    {
                        script: tree.data('root'),
                        multiFolder: true,
                        onComplete: function () {
                        }
                    },
                    function (file) {
                    });
            }
        });
    });

    $.contextMenu({
        trigger: "left",
        selector: 'li.file',
        items: {
            view: {
                name: "View",
                callback: function (key, opt) {
                    var base = this.closest('div[data-view]').data('view');
                    var rel = this.find('a').attr('rel');
                    var open_link = window.open('', '_blank');
                    open_link.location = base + rel;
                }
            },
            download: {
                name: "Download",
                callback: function (key, opt) {
                    var base = this.closest('div[data-download]').data('download');
                    var rel = this.find('a').attr('rel');
                    var open_link = window.open('', '_blank');
                    open_link.location = base + rel;
                }
            }
        }
    });
});
