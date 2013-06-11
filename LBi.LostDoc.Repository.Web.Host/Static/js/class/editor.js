var editor = new (function($) {
    var editor = this;

    var Selection = editor.Selection = function() {
        if (arguments.length) {
            this._init.apply(this, arguments);
        }
    };

    Selection.prototype._init = function(sel, ranges) {
        this.sel = sel;
        this.ranges = ranges;
    };

    Selection.prototype.inline = function(nodename, attributes) {
        // sel.getRangeAt() clones a range, to get manipulated ranges back
        // into the selection we need to remove them and add the manipulated
        // clones
        this.sel.removeAllRanges();
        for (var i = 0; i < this.ranges.length; i++) {
            var range = this.ranges[i];
            //this._normalizeRange(range);

            // if the selected text is surrounded by a node named nodename,
            // remove that surrounding node, first determine the parent of
            // the everything that's selected
            var parent = range.commonAncestorContainer;
            if (parent.nodeType == 3 &&
                    range.startOffset == 0 &&
                    range.endOffset == parent.nodeValue.length) {
                parent = parent.parentNode;
            }
            // now if the parent has the node name as passed to this method,
            // and the parent's full text is selected, remove it
            if (parent.nodeName.toLowerCase() == nodename.toLowerCase() &&
                    $(parent).text() == range.toString()) {
                var textnodes = this._getTextNodes(parent);
                var last = textnodes[textnodes.length - 1];
                this._replaceNodeWithContents(parent);
                range.setStart(textnodes[0], 0);
                range.setEnd(last, last.length);
            } else {
                var clone = range.cloneContents();
                // if we contain the requested node already, remove it
                // rather than re-add (XXX should we remove first and then
                // re-add? what's the best usability-wise?)
                if (clone.querySelectorAll(nodename).length) {
                    this._removeInline(range, clone, nodename);
                } else {
                    this._addInline(range, clone, nodename, attributes);
                }
                var textnodes = this._getTextNodes(clone);
                var last = textnodes[textnodes.length - 1];
                range.deleteContents();
                range.insertNode(clone);
                range.setStart(textnodes[0], 0);
                range.setEnd(last, last.length);
            }
            this.sel.addRange(range.cloneRange());
        }
    };

    Selection.prototype.block = function(nodename, attributes) {
        this.sel.removeAllRanges();
        for (var i = 0; i < this.ranges.length; i++) {
            var range = this.ranges[i];
            var node = document.createElement(nodename);
            for (var attr in attributes) {
                node.setAttribute(attr, attributes[attr]);
            }
            var clone = range.cloneContents();
            while (clone.hasChildNodes()) {
                node.appendChild(clone.firstChild);
            }
            var textnodes = this._getTextNodes(node);
            var last = textnodes[textnodes.length - 1];
            range.deleteContents();
            range.insertNode(node);
            range.setStart(textnodes[0], 0);
            range.setEnd(last, last.length);
            this.sel.addRange(range);
        }
    };

    Selection.prototype._addInline =
            function(range, clone, nodename, attributes) {
        var textnodes = this._getTextNodes(clone);
        var last;
        for (var j = 0; j < textnodes.length; j++) {
            var textnode = textnodes[j];
            if (!textnode.nodeValue) {
                continue;
            }
            var newnode = document.createElement(nodename);
            if (attributes) {
                for (var attr in attributes) {
                    newnode.setAttribute(attr, attributes[attr]);
                }
            }
            textnode.parentNode.replaceChild(newnode, textnode);
            newnode.appendChild(textnode);
            last = textnode;
        }
    };

    Selection.prototype._removeInline = function(range, clone, nodename) {
        var els = clone.querySelectorAll(nodename);
        for (var i = 0; i < els.length; i++) {
            var el = els[i];
            this._replaceNodeWithContents(el);
        }
    };

    Selection.prototype._replaceNodeWithContents = function(node) {
        while (node.hasChildNodes()) {
            node.parentNode.insertBefore(node.firstChild, node);
        }
        node.parentNode.removeChild(node);
    };

    Selection.prototype._getTextNodes = function(el) {
        var ret = [];
        for (var i = 0; i < el.childNodes.length; i++) {
            var child = el.childNodes[i];
            if (child.nodeType == 3) {
                ret.push(child);
            } else if (child.nodeType == 1) {
                ret = ret.concat(this._getTextNodes(child));
            }
        }
        return ret;
    };

    var Editor = editor.Editor = function() {
        if (arguments.length) {
            this._init.apply(this, arguments);
        }
    };

    Editor.prototype._init = function(el) {
        this.el = el = $(el);
    };

    Editor.prototype.edit = function() {
        this.el.attr('contenteditable', !this.el.attr('contenteditable'));
    };

    Editor.prototype.inline = function(nodename, attrs) {
        var sel = this.getSelection();
        sel.inline(nodename, attrs);
    };

    Editor.prototype.block = function(nodename, attrs) {
        var sel = this.getSelection();
        sel.block(nodename, attrs);
    };

    Editor.prototype.getSelection = function() {
        var sel = document.getSelection();
        var range = sel.getRangeAt(0);
        var ranges_in_el = [];
        for (var i = 0; i < sel.rangeCount; i++) {
            var range = sel.getRangeAt(i);
            // use range only if it's inside the editable element
            var range_in_el = true;
            // first check start container el
            var current = range.startContainer;
            while (current !== this.el.get(0)) {
                current = current.parentNode;
                if (current == document.documentElement) {
                    range_in_el = false;
                    break;
                }
            }
            if (!range_in_el) {
                continue;
            }
            // now end container
            var current = range.endContainer;
            while (current !== this.el.get(0)) {
                current = current.parentNode;
                if (current == document.documentElement) {
                    range_in_el = false;
                    break;
                }
            }
            if (range_in_el) {
                ranges_in_el.push(range);
            }
        }
        if (!ranges_in_el.length) {
            throw new Error('nothing selected');
        }
        window.current_range = ranges_in_el[0];
        return new Selection(sel, ranges_in_el);
    };

    $.fn.edit = function(command, nodename, attributes) {
        var args = [];
        for (var i = 1; i < arguments.length; i++) {
            args.push(arguments[i]);
        }
        $(this).each(function(i, el) {
            var editor = $(el).get(0)._editor;
            if (!editor) {
                editor = new Editor(el);
                $(el).get(0)._editor = editor;
            }
            if (command) {
                editor[command].apply(editor, args);
            }
        });
        return $(this);
    };
})(Zepto);