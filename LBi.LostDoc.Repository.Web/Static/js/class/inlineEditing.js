(function($) {
    var inlineEditing = lostDoc.inlineEditing = function() {
		this.init.apply(this,arguments);
	};

	inlineEditing.prototype.settings = {

	};

	inlineEditing.prototype.init = function(settings) {
		this.settings = $.extend({}, this.settings, settings || {} );

	},

	inlineEditing.prototype.createTextArea = function(myValue) {
		var textArea = $('<textarea>');
	},

	inlineEditing.prototype.insertInTextarea = function(myValue) {
		var textArea = $('textarea');
		var startPos = textArea.selectionStart;
		var endPos = textArea.selectionEnd;
		var scrollTop = textArea.scrollTop;
		console.log(textArea);
		textArea.value = textArea.value.substring(0, startPos)+myValue+textArea.value.substring(endPos,textArea.value.length);
		textArea.focus();
		textArea.selectionStart = startPos + myValue.length;
		textArea.selectionEnd = startPos + myValue.length;
		textArea.scrollTop = scrollTop;
	},

	$.fn.inlineEditing = function(settings) {
		$(this).each(function() {
			$(this).data('lib-inlineEditing', new inlineEditing(settings));
		});
	};
})(Zepto);