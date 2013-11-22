/*
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
    LostDoc.Settings = function (defaultSettings) {
        if (defaultSettings) {
            $.extend(this, defaultSettings);
        }
        if (localStorage) {
            var storedSettings = localStorage.getItem('settings');
            if (storedSettings) {
                var parsedSettings = JSON.parse(storedSettings);
                $.extend(this, parsedSettings);
            }
        }
    };

    LostDoc.Settings.prototype.save = function () {
        if (localStorage) {
            var rawSettings = JSON.stringify(this);
            localStorage.setItem('settings', rawSettings);
            console.log("saving settings", rawSettings);
        }
    };
}(LostDoc);