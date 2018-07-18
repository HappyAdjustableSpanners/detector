jQuery(function ($) {
    $(".test-ajax").on("click", function () {
        $.get("/test/date", function (data) {
            $(".test-ajax-output").html(
                data
            );
        })
        
    });

});