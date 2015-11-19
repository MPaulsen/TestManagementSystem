$.widget("custom.combobox", {
    _create: function () {
        this.wrapper = $("<span>")
          .addClass("ui-combobox input-group")
          .insertAfter(this.element);

        this.element.hide();
        this._createAutocomplete();
        this._createShowAllButton();
    },

    _createAutocomplete: function () {
        var selected = this.element.children(":selected"),
          value = selected.val() ? selected.text() : "";

        var ddl = $(this.element);

        this.input = $("<input>")
          .appendTo(this.wrapper)
          .val(value)
          .attr("title", "")
          //.addClass( "ui-combobox-input ui-widget ui-widget-content ui-state-default ui-corner-left" )
          .addClass('form-control')
          //.data('val-required', 'The Course field is required.')
          //.data('val', 'true')
          .autocomplete({
              delay: 0,
              minLength: 0,
              source: $.proxy(this, "_source")
          })
          .tooltip({
              tooltipClass: "ui-state-highlight"
          });

        /*this.input.blur(function() {
            var val = $(this).val();
            setTimeout(function () {
                // default dropdow to a blank value if the combobox is blank
                if (val == '') {
                    ddl.val('');
                }
            }, 1000);
        });*/

        this._on(this.input, {
            autocompleteselect: function (event, ui) {
                ui.item.option.selected = true;
                this._trigger("select", event, {
                    item: ui.item.option
                });
            },

            autocompletechange: "_removeIfInvalid"
        });
    },

    _createShowAllButton: function () {
        var input = this.input,
          wasOpen = false;

        $('<div class="input-group-addon"><i class="fa fa-sort-down"></i></div>')
          .css('cursor', 'pointer')
          //.attr( "tabIndex", -1 )
          //.attr( "title", "Show All Items" )
          //.tooltip()
          .appendTo(this.wrapper)
          /*.button({
              icons: {
                  primary: "ui-icon-triangle-1-s"
              },
              text: false
          })
          .removeClass( "ui-corner-all" )*/
          //.addClass( "ui-combobox-toggle ui-corner-right" )
          .mousedown(function () {
              wasOpen = input.autocomplete("widget").is(":visible");
          })
          .click(function () {
              //input.focus();
              if ($('.ui-autocomplete').is(':visible')) {
                  $('.ui-autocomplete').hide();
              }
              else {
                  $('.ui-autocomplete').show();
              }

              // Close if already visible
              if (wasOpen) {
                  return;
              }

              // Pass empty string as value to search for, displaying all results
              input.autocomplete("search", "");
          });
    },

    _source: function (request, response) {
        var matcher = new RegExp($.ui.autocomplete.escapeRegex(request.term), "i");
        response(this.element.children("option").map(function () {
            var text = $(this).text();
            if (this.value && (!request.term || matcher.test(text)))
                return {
                    label: text,
                    value: text,
                    option: this
                };
        }));
    },

    _removeIfInvalid: function (event, ui) {

        // Selected an item, nothing to do
        if (ui.item) {
            return;
        }

        // Search for a match (case-insensitive)
        var value = this.input.val(),
          valueLowerCase = value.toLowerCase(),
          valid = false;
        this.element.children("option").each(function () {
            if ($(this).text().toLowerCase() === valueLowerCase) {
                this.selected = valid = true;
                return false;
            }
        });

        // Found a match, nothing to do
        if (valid) {
            return;
        }

        // Remove invalid value
        this.input
          .val("")
          .attr("title", value + " didn't match any item")
          .tooltip("open");
        this.element.val("");
        this._delay(function () {
            this.input.tooltip("close").attr("title", "");
        }, 2500);
        this.input.autocomplete("instance").term = "";
    },

    _destroy: function () {
        this.wrapper.remove();
        this.element.show();
    }
});