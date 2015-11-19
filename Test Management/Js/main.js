$(function () {
    if (navigator.userAgent.indexOf('MSIE 10') > -1) {
        $('html').addClass('ie10');
    }

    // add extra validation to form inputs
    try {
        $('form input.number').rules('add', 'number');
    } catch (e) { }

    try {
        $('input.int').inputmask('integer', { rightAlign: false });
        $('input.date').inputmask('m/d/y');
        $('input.time').inputmask('99:99 AM');
    }
    catch (e) { }

    setTimeout(function () {
        $('.alert button.close:not(.stay)').closest('.alert').fadeOut();
    }, 5000);

    $('.alert button.close').click(function () {
        $(this).closest('.alert').fadeOut();
    });

    calendarTooltips();

    /* since he tooltips don't work on disabled elements (for they don't trigger any events), let's:
       - remove the disabled attribute from these
       - add a disabled class
       - unbind click event
    */
    $('#times .btn:disabled').prop('disabled', false).addClass('disabled').unbind('click').tooltip({
        title: 'Not Available'
    });
});

function calendarTooltips() {
    setTimeout(function () {
        $('#dates .ui-datepicker-unselectable:not(.ui-datepicker-other-month) span').tooltip({
            title: 'Not Available'
        });
    }, 0);
}

function writeEmail(name, domain, ext) {
	var email = name + '@' + domain + '.' + ext;
	document.write('<a href="mailto:' + email + '">' + email + '</a>');
}

$.validator.setDefaults({
    highlight: function (element, errorClass, validClass) {
        var div = $(element).closest('.form-group');
        div.addClass('has-error');
        setTimeout(function () {
            if (div.find('.field-validation-error').length > 0) {
                div.find('.field-validation-error').addClass('text-danger');
                div.find('.field-validation-error i').remove();
                div.find('.field-validation-error').prepend($('<i/>').addClass('fa fa-times').css('margin-right', 3));
            }
            else {
                var error = div.find('.error');
                if (error.length == 0) {
                    error = div.next('.error');
                }
                else {
                    div.after(error);
                }
                error.addClass('text-danger').css({
                    'font-weight': 'normal',
                    'margin-top': -10
                });
                error.find('i').remove();
                error.prepend($('<i/>').addClass('fa fa-times').css('margin-right', 3));
                div.css('margin-bottom', 0);
            }
        }, 0);
    },
    unhighlight: function (element, errorClass, validClass) {
        var div = $(element).closest('.form-group');
        div.removeClass('has-error');
        if (div.find('.field-validation-error').length > 0) {
            div.find('.field-validation-error').removeClass('text-danger');
        }
        else {
            div.find('.error').removeClass('text-danger');
            div.append(div.next('.error'));
            div.css('margin-bottom', 10);
        }
    },
    submitHandler: function (form) {
        var fn = $(form).data('fn');
        if (fn !== undefined) {
            eval(fn + '()');
        } else {
            form.submit();
        }
    }
});