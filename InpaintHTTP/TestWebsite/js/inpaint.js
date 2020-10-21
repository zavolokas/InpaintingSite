var Inpaint = function () {

    let options = {
        iterations: 3     //default is 100 (ms)
    };

    let LastResponse;

    let init = function () {
        //start();
    };

    let _inpaintDone = function (result, callback) {

        if(result == null)
        {
            //TODO: Do correct error handeling... or prevent user from clicking #Inpaint when no img is selected
            callback();
            return;
        }
        navigator.vibrate(500);
        console.log("Finished Request");
        LastResponse = result;
        let img = new Image();
        img.src = "data:image/png;base64," + LastResponse;
        imageHandler.SetLastBase64img(img);
        imageHandler.saveLastResponseImage();
        callback();
    };

    let _inpaintImage = function (imagefile, maskfile, callback) {

        var funcResult = null;
        var fd = new FormData();

        // images
        fd.append('file',imagefile);
        fd.append('file', maskfile);

        fd.append('body', JSON.stringify({
            SelectedAlgorithm: "Inpaint",
            InpaintSettings: {
                PatchDistanceCalculator: $('#patch-distance-calcuator-slider').val(),
                MaxInpaintIterations: parseInt($('#max-iterations-slider').val()),
                PatchMatch: {
                    PatchSize: parseInt($('#patch-size-slider').val())
                }
            }
        }));

        console.log("Started Request");

        $.ajax({
            url: '/api/inpaint',
            type: 'post',
            data: fd,
            contentType: false,
            processData: false,
            timeout: 300000, 		// 300 second timeout to avoid connection from breaking while server is still loading
            success: function(data){ _inpaintDone(data, callback) },
            error: function(data){ _inpaintDone(null, callback) }
        });
    };

    let _getLastRespone = function(){
        return LastResponse;
    };

    return {
        init: init,
        inpaintImage: _inpaintImage,
        inpaintDone: _inpaintDone,
        getLastResponse: _getLastRespone
    };
}();

Inpaint.init();