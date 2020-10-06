let imageHandler = function () {

    let lastbase64img;

    let _init = function()
    {

    };

    let _UpdateImage = function(input, callback) {
        if (input.files && input.files[0]) {
            let reader = new FileReader();
            reader.onload = function (e) {
                let img = new Image();
                img.onload = function(){
                    //initialize Canvas
                    maskHandler.setCanvas(document.getElementById('preview')); //Testing, replace with jQuery or smthing
                    maskHandler.clearMask();
                    maskHandler.canvasResize(img.width, img.height);
                    lastbase64img = img;
                    callback();
                };
                img.src = e.target.result;
            };
            reader.readAsDataURL(input.files[0]);
        }

    };

    let _GetLastBase64img = function(){
        return lastbase64img;
    };

    let _SetLastBase64img = function(base64){
        lastbase64img = base64;
    };

    let _GetLastimg = function(){
        return convertBase64ToFile(lastbase64img.src);
    };

    let _saveLastResponseImage = function(){
        let storage = window.localStorage;
        storage.setItem("ls-img-id-" + storage.length, lastbase64img.src);
    };

    let _blobToImg = function(blob){
        let img = new Image();
        img.src = _arrayBufferToBase64(blob.buffer);
        return img;
    };

    //snippets from Stackoverflow
    const convertBase64ToFile = function (image) {
        const byteString = atob(image.split(',')[1]);
        const ab = new ArrayBuffer(byteString.length);
        const ia = new Uint8Array(ab);
        for (let i = 0; i < byteString.length; i += 1) {
            ia[i] = byteString.charCodeAt(i);
        }
        return new Blob([ab], {
            type: 'image/png',
        });
    };

    function _arrayBufferToBase64( buffer ) {
        let binary = '';
        let bytes = new Uint8Array( buffer );
        let len = bytes.byteLength;
        for (var i = 0; i < len; i++) {
            binary += String.fromCharCode( bytes[ i ] );
        }
        return window.btoa( binary );
    }

    return {
        init: _init,
        UpdateImage: _UpdateImage,
        GetLastBase64img: _GetLastBase64img,
        SetLastBase64img: _SetLastBase64img,
        GetLastimg: _GetLastimg,
        saveLastResponseImage: _saveLastResponseImage,
        blobToImg: _blobToImg
    };
}();

imageHandler.init();