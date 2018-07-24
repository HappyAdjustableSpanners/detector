jQuery(function ($) {
    $(".test-ajax").on("click", function () {
        $.get("/test/date", function (data) {
            $(".test-ajax-output").html(
                data
            );
        })
        
    });

    setInterval(function () {
        // ajax call controller function to work out what state we are in for each brand
        $.ajax({
            url: '/brand/Status',
            dataType: "json",
            success: function (data) {
             
                var list = data;
                $.each(list, function (index, item) {
                    console.log(item);
                    var brandId = item.split(" ")[0];
                    var status = item.split(" ")[1];
                    var id = "#status-" + brandId; 
                    $(id).html(
                        status
                    );

                    var downloadGraphLinkId = "#download-" + brandId; 
                    var spinnerid = "#spinner-" + brandId;
                    if (status == "training" || status == "generating-augs") {
                        $(spinnerid).removeClass("hidden");             
                    }
                    else {
                        $(spinnerid).addClass("hidden");
                    }

                    if (status == "trained") {
                        $(downloadGraphLinkId).removeClass("hidden");
                    }
                    else {
                        $(downloadGraphLinkId).addClass("hidden");
                    }
                });
            }
        })
    }, 1000);
});

