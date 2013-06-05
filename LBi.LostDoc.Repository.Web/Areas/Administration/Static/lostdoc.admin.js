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

$(".tilt").each(function() {
    $(this).mousedown(function(event) {
        // Does the click reside in the center of the object 
        if (event.pageX > $(this).offset().left + ($(this).outerWidth() / 2) - (0.1 * $(this).outerWidth()) &&
            event.pageX < $(this).offset().left + ($(this).outerWidth() / 2) + (0.1 * $(this).outerWidth()) &&
            event.pageY > $(this).offset().top + ($(this).outerHeight() / 2) - (0.1 * $(this).outerHeight()) &&
            event.pageY < $(this).offset().top + ($(this).outerHeight() / 2) + (0.1 * $(this).outerHeight())) {
            $(this).css("transform", "perspective(500px) translateZ(-15px)");
        } else {
            var slope = $(this).outerHeight() / $(this).outerWidth(),
                descendingY = (slope * (event.pageX - $(this).offset().left)) + $(this).offset().top,
                ascendingY = (-slope * (event.pageX - $(this).offset().left)) + $(this).offset().top + $(this).outerHeight();

            if (event.pageY < descendingY) {
                if (event.pageY < ascendingY) {
                    // top region
                    $(this).css("transform", "perspective(500px) rotateX(8deg)");
                } else {
                    // right region
                    $(this).css("transform", "perspective(500px) rotateY(8deg)");
                }
            } else {
                if (event.pageY > ascendingY) {
                    // bottom region
                    $(this).css("transform", "perspective(500px) rotateX(-8deg)");
                } else {
                    // left region
                    $(this).css("transform", "perspective(500px) rotateY(-8deg)");
                }
            }
        }
    });

    // TODO this is super flaky/weird, ask Pim
    var link = $(this);
    var container = link.parent();
    container.mouseout(function (event) {
        link.css("transform", "");
    });
});


$(function() {
    n = new Notifications($('section[role=alert]').get(0), $('div.handle').get(0));
    
});