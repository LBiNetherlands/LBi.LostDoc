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

function Notifications(section, handle){
    this.handle = handle;
    this.section = section;
    this.section.className = "closed";
    this.handle.addEventListener('click', function (evt) {
        if (this.section.className == 'open') {
            this.section.className = "closed";
        } else {
            this.section.className = "open";
        }
        // alert(this.section.className);
    }.bind(this));
}

// Add methods like this.  All Person objects will be able to invoke this
Notifications.prototype.speak = function() {
    alert("Howdy, my name is" + this.name);
};

