(function($){
    window.lostDoc = {
		//versioning
        version: 0.1,

        // Main settings...
        settings: {
            'debug' : true,
            'handleClass' : 'handle',
            'leftColumnSelector' : 'nav[role="main-navigation"]',
            'rightColumnSelector' : 'section[role="content"]',
            'grabClass' : 'grabbing'
        },

		init: function() {
            this.editor = [];
            this.handle = $('.' + this.settings['handleClass']);
            this.leftColumn = $(this.settings['leftColumnSelector']);
            this.rightColumn = $(this.settings['rightColumnSelector']);
            this.setRightColumnHeight();
            this.openLocalStorage();
            this.initColumnResizer();
            this.startEditing();
		},

        setRightColumnHeight: function() {
            var height = $(window).height();
            this.rightColumn.css({
                'min-height' : height
            });
        },

        pxToPercentage: function(px) {
            this.wrapperWidth = $('#wrapper').width();
            var temp = parseFloat(px) / parseFloat(this.wrapperWidth);
            var percentage =  Math.round(1000*temp) / 10;
            return percentage;
        },

        storeColumnWidth: function() {
            localStorage.setItem("columnWidth", this.colLeftWidthPerc);
        },

        openLocalStorage: function() {
            this.colLeftWidthPerc = localStorage.getItem("columnWidth");
            this.colRightWidthPerc = 100 - this.colLeftWidthPerc;
            if(this.colLeftWidthPerc > 0 && this.colRightWidthPerc) {
                this.setColumnWidth(true);
            }
        },

        setColumnWidth: function(init) {
            var total = this.colLeftWidthPerc + this.colRightWidthPerc;
            if(total == 100 && this.colLeftWidthPerc > 15 && this.colRightWidthPerc > 30 || init === true) {
                this.leftColumn.css({
                    'width' : this.colLeftWidthPerc + '%'
                });
                this.rightColumn.css({
                    'width' : this.colRightWidthPerc + '%'
                });
            }
        },

        initColumnResizer: function(e) {
            var mouseDown = this.handle.bind('mousedown', function(e) {
            $('body').addClass(this.settings['grabClass']);
            this.column = this.handle.offset().left;
            this.colRightWidth = this.rightColumn.width();
            this.colLeftWidth = this.leftColumn.width();
               var mouseMove = $(document).bind('mousemove', function(e) {
                    this.moved = this.column - e.clientX;
                    //now resize the columns
                    var colRightWidth = (this.colRightWidth-1) + this.moved;
                    this.colRightWidthPerc = this.pxToPercentage(colRightWidth);
                    var colLeftWidth =  this.colLeftWidth - this.moved;
                    this.colLeftWidthPerc = this.pxToPercentage(colLeftWidth);
                    this.setColumnWidth();
                }.bind(this));
            }.bind(this));
            var mouseUp = $(document).bind('mouseup', function() {
               $(document).unbind('mousemove');
               $('body').removeClass(this.settings['grabClass']);
               this.storeColumnWidth();
            }.bind(this));
        },

        startEditing: function() {
            $('.edit').bind('click', function(e) {
                var id = $(e.target).data('id');
                var parent = $(e.target).parent();
                    parent.addClass('editing');
                var textArea = $("<textarea>")
                    .attr('id',id);
                //buttons
                var saveButton = $("<a>");
                    saveButton.text('Save changes')
                        .addClass('button save');
                var previewButton = $("<a>");
                    previewButton.text('Preview edited source')
                        .addClass('button preview');
                var form = $('<form>');
                    form.attr('data-id',id)
                        .append(textArea)
                        .append(previewButton)
                        .append(saveButton);
                parent.html(form);
                this.initInlineEditing(id);
            }.bind(this));
        },

        initInlineEditing: function(id) {
            CodeMirror.xmlHints['<'] = [
                'ul',
                'li'
            ];
            this.editor[id] = CodeMirror.fromTextArea(document.getElementById(id), {
                value: '',
                mode: 'text/html',
                lineNumbers: true,
                indentUnit: 4,
                lineWrapping: true,
                fixedGutter: false,
                extraKeys: {
                    "' '": function(cm) { CodeMirror.xmlHint(cm, ' '); },
                    "'<'": function(cm) { CodeMirror.xmlHint(cm, '<'); },
                    "Ctrl-Space": function(cm) { CodeMirror.xmlHint(cm, ''); }
                },
                autoCloseTags: true
            });
            this.editor[id].focus();
            this.initBindEditingEvents(this.editor[id]);
        },

        initBindEditingEvents: function(editor) {
            $('.save').bind('click',function(e){
                var container = $(e.target).closest('form');
                var backButton = $("<a>");
                    backButton.text('Back')
                        .addClass('button cancel');
                var sureButton = $("<a>");
                    sureButton.text('sure')
                        .addClass('button sure');
                container.append(backButton)
                    .append(sureButton);
                $(e.target).hide();
            }.bind(this));
        //cancel
            $('.cancel').live('click',function(e){
                var container = $(e.target).closest('form');
                var saveButton = container.find('.save');
                    saveButton.show();
                var sureButton = container.find('.sure');
                    sureButton.remove();
                var cancelButton = container.find('.cancel');
                    cancelButton.remove();
            });
        //sure save
            $('.sure').live('click',function(e){
                var id = $(e.target).parent('form').data('id');
                var value = this.editor[id].getValue();
                $.ajax({
                  type: 'GET',
                  url: 'index.shtml',
                  // data to be added to query string:
                  data: value,
                  success: function(data){
                    alert('Ajax succes!');
                  },
                  error: function(xhr, type){
                    alert('Ajax error!');
                  }
                });
            }.bind(this));
        //preview
            $('.preview').bind('click',function(e){
                e.preventDefault();
                var id = $(e.target).parent('form').data('id');
                var value = this.editor[id].getValue();
                var container = $(e.target).closest('.flip-container');
                var previewCodeArea = container.find('.back');
                var backButton = $("<a>");
                    backButton.text('Back')
                        .addClass('button backToCode');
                previewCodeArea
                    .append(value)
                    .append(backButton);
                container.addClass('previewCode');
                e.preventDefault();
            }.bind(this));
        //backToCode
            $('.backToCode').live('click',function(e){
                e.preventDefault();
                var container = $(e.target).closest('.flip-container');
                var previewCodeArea = container.find('.back');
                    previewCodeArea.empty();
                container.removeClass('previewCode');
                e.preventDefault();
            }.bind(this));
        }
	};//end window
    $(function() {
        lostDoc.init();
    });
})(Zepto);