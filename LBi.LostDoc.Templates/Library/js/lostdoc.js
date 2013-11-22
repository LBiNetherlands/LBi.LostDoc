/*
 * Copyright 2012-2013 DigitasLBi Netherlands B.V.
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
    LostDoc.App = function () {
        //versioning
        this.version = 0.1;

        this._settings = new LostDoc.Settings({});

        this._layout = new LostDoc.Layout({
                'handleSelector': '.handle',
                'containerSelector': '#wrapper',
                'leftColumnSelector': 'div.left-col-outer',
                'rightColumnSelector': 'div.right-col-outer',
                'resizingClass': 'resizing',
                'detachableContentSelector': '.detachable'
            },
            this._settings);
    };


}(LostDoc); // note that the namespace is instantiated immediately

app = new LostDoc.App();