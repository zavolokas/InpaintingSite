$(function(){
    //Image uploaded
    $("#Image").change(function(){
        //init canvas
        maskHandler.removeCanvas();
        imageHandler.UpdateImage(this, function(){
            $('#preview-note').css("visibility", "hidden");
            $(this).css("visibility", "hidden");

            let img = imageHandler.GetLastBase64img();

            $('#Image')
                .css("position", "absolute")
                .css("visibility", "hidden");
            $('#preview')
                .css("background-image", "url('" + img.src + "')");
                //.css("width", "auto"); // remobe placeholder
        });
    });

    $('#inpaint').click(function () {
        $('#preview').addClass("img-busy");
        $("#inpaint").attr("disabled",true);
        $("#reset").attr("disabled",true);
        $("#undo").attr("disabled",true);
        $("#galery").attr("disabled",true);

        $('#mask').attr("src", maskHandler.getCanvas().toDataURL())
            .removeClass("mask-finished");

        var canvasFile = maskHandler.canvasToFile();
        //maskHandler.removeCanvas(); //delete canvas to make space for img #Mask
        maskHandler.getCanvas().style.display = "none";
        Inpaint.inpaintImage(imageHandler.GetLastimg(), canvasFile, function(){
            //callback on image done
            maskHandler.clearMask();
            $('#preview').css("background-image", "url('data:image/png;base64," + Inpaint.getLastResponse() + "')")
                .removeClass("img-busy");
            $("#mask").addClass("mask-finished");
            $("#inpaint").attr("disabled",false);
            $("#reset").attr("disabled",false);
            $("#undo").attr("disabled",false);
            $("#galery").attr("disabled",false);
        });
    });

    $("#masksize-slider").change(function(){
        maskHandler.setLineWidth(this.value);
    });

    $("#reset").click(function(){
        if(maskHandler.isCanvasUsed())
        {
            maskHandler.clearMask();
        }else{
            maskHandler.getCanvas().style.display = "none";
            $("#inpaint").removeClass("btn-hide");
            $('#preview').css("background-image", "")
                .removeClass("img-busy");
            $("#mask").addClass("mask-finished")
                .attr("src","");
            $('#Image')
                .css("position", "relative")
                .css("visibility", "visible")
                .val('');
            $('#preview-note').css("visibility", "visible");
            //$('#preview').css("width", "500px"); // placeholder
        }
    });

    $("#undo").click(function(){
        maskHandler.removeLastPaint();
    });

    $("#galery").click(function(){
        updateGalery();
    });

    $("#galery-close-bottom, #galery-close-top").click(function(){
        $("#content").css("display","block");
        $("#img-galery").css("display","none");
    });

    $(document).on("click",".close", function(){
        window.localStorage.setItem($(this).context.id,"removed");
        updateGalery();
    });

    // Desktop Mouse Movement Events 
    $('#preview').mousedown(function (e) {
        console.log("mousedown " + maskHandler.paints());
        let c = maskHandler.getCanvas();
        if(c != null)
        {
            let f = c.width / $(this).width();
            maskHandler.OnMouseDown(e, this.offsetLeft, this.offsetTop, f); //WARNING: this conflicts with mobile, gets called there
        }
    }).mousemove(function (e) {
        console.log("mousemove " + maskHandler.paints());
        let c = maskHandler.getCanvas();
        if(c != null)
        {
            let f = c.width / $(this).width();
            maskHandler.OnMouseMove(e, this.offsetLeft, this.offsetTop, f);
        }
    }).mouseleave(function (e) {
        console.log("mouseleave " + maskHandler.paints());
        maskHandler.OnMouseLeave(e);
    }).mouseup(function (e) {
        console.log("mouseleave " + maskHandler.paints());
        maskHandler.OnMouseUp(e);
    }
    // Mobile Mouse Movement Events
    ).on({'touchstart': function (e) {
        console.log("start " + maskHandler.paints());
        let c = maskHandler.getCanvas();
        if (c != null) {
            let f = c.width / $(this).width();
            maskHandler.OnMouseDown(e, this.offsetLeft, this.offsetTop, f);
        }
    }}).on({'touchmove' :function(e) {
        console.log("move " + maskHandler.paints());
        let c = maskHandler.getCanvas();
        if (c != null) {
            let f = c.width / $(this).width();
            maskHandler.OnMouseMove(e, this.offsetLeft, this.offsetTop, f);
        }
    }}).on({'touchend' :function (e) {
        maskHandler.OnMouseUp(e);
        console.log("up " + maskHandler.paints());
    }});
});

function updateGalery(){
    $("#galery-list").empty();
    $("#content").css("display","none");
    $("#img-galery").css("display","block");

    for(let i = 0; i < window.localStorage.length; i++)
    {
        let currentItem = window.localStorage.getItem("ls-img-id-" + i);
        if(currentItem != null && currentItem !== "removed")
        {
            $("#galery-list").prepend( "<li><div class='img-box'><span class='close' id='ls-img-id-" + i + "'>&times;</span><img class='galery-img' src=" + currentItem + "></div></li>");
        }
    }
}