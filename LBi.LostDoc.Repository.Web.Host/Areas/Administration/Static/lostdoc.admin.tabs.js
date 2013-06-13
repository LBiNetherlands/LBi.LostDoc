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

if (!('LostDoc' in window)) {
    window.LostDoc = {};
}

LostDoc.Tabs = new (function() {
    var Tabs = this;
    Tabs.Init = function (element) {
        $(element).find('>div').hide();
        $(element).find('>div:first').show();
        $(element).find('>ul li:first').addClass('active');

        $(element).find('>ul>li>a').click(function () {
            $(element).find('>ul>li').removeClass('active');
            $(this).parent().addClass('active');
            var currentTab = $(this).attr('href');
            $(element).find('>div').hide();
            $(currentTab).show();
            return false;
        });
    };
})();
