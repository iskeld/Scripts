/*
* jquery-clicks.js
* 
* a simple jQuery plugin, which helps in handling both click and double-click events on the same element. 
* Requires Bacon.js library (https://baconjs.github.io/)
* 
* example:
* $("#myButton").clicks(function(evt) {
        console.log("single click");
    }, function (evt) {
        console.log("double click");
    },  // these are optional:
    { 
        delay: 300, // maximum delay between clicks. Default: 300ms 
        clickCount: 2 // number of clicks for a multi-click event to be fired. Default: 2 (double-click)
    });
*/

(function($) {
    $.fn.clicks = function(click, dblClick, options) {
        function isFunction(value) { return typeof value == 'function' || false; }          
        if (!(isFunction(click) && isFunction(dblClick))) {
            throw new TypeError('Expected a function');
        }

        var settings = $.extend({ delay: 300, clickCount: 2 }, options);
        var clickCount = settings.clickCount;
        this.asEventStream("click")
            .bufferWithTimeOrCount(settings.delay, clickCount)
            .onValue(function(events) {
              return events.length == clickCount ? dblClick(events) : click(events[0]);
            });
        return this;
    };

    /* uncomment these to allow handlers assignments from the HTML
    ** example: <button onclicks="singleHander,doubleHandler">test</button>
    $("[onclicks]").each(function(idx, element) {
        var handlers = $(element).attr("onclicks").split(',');
        if (handlers.length !== 2) {
            throw new RangeError("Invalid number of functions passed to the 'onclicks' handler");
        }
        $(element).clicks(window[handlers[0]], window[handlers[1]]);
    });
    */
}(jQuery));
