let maskHandler = function () {
    let canvas;
    let clickX;
    let clickY;
    let clickDrag;
    let paint;
    let dotWidth;
    let sLineWidth = 20;
    let dotX;
    let dotY;
    let lastClickX;
    let lastClickY;

    let linePart; //weird way to identify shiet
    let lineID = 0; //increases to identify what dots belongs to which (visible) line

    let _init = function(){
        clickX = [];
        clickY = [];
        clickDrag = [];
        dotWidth = [];
        linePart = [];
    };

    //TEST1: document.getElementById("preview").appendChild(canvas)
    //TEST2: MaskHandler.setCanvas(document.getElementById("preview"));
    let _setCanvas = function(ParentObj)
    {
        canvas = document.createElement("canvas");
        canvas.setAttribute("id","canvas");
        ParentObj.appendChild(canvas);
    };

    let _removeCanvas = function(ParentObj)
    {
        //canvas.style.display = 'none';
        //canvas = null;
    };

    let _canvasResize = function(width, height){
        if(canvas == null)
        {
            console.log("_canvasResize: Canvas not set");
            return;
        }
        canvas.setAttribute("width", width);
        canvas.setAttribute("height",height);
    };

    //clears the mask
    let _clearMask = function(count){

        if(canvas == null)
        {
            console.log("_clearMask: Canvas not set");
            return;
        }
        if(!count)
            count = 0;
        clickX.length = count;
        clickY.length = count;
        clickDrag.length = count;
        dotWidth.length = count;
        linePart.length = count;
        if(count === 0)
        {
            let context = canvas.getContext("2d");
            context.clearRect(0, 0, context.canvas.width, context.canvas.height); //Clear canvas
            _killDot(); //kill dot
        }else{
            //set dot to last
            dotX = clickX[clickX.length-1];
            dotY = clickY[clickY.length-1];
            _reDraw();
        }

    };

    let _initDot = function(x, y)
    {
        if(!dotX && !dotY)
        {
            console.log("dot placed at " + x + "," + y);
            dotX = x;
            dotY = y;
        }
    };

    let _killDot = function()
    {
        if(dotX && dotY)
        {
            dotX = null;
            dotY = null;
        }
    };

    let _isDotActive = function()
    {
        return dotX != null;
    };

    let _geDotXY = function()
    {
        return {dotX, dotY};
    };

    let _addClick = function(x, y, dragging){
        clickX.push(x);
        clickY.push(y);
        clickDrag.push(dragging);
        dotWidth.push(sLineWidth);
        linePart.push(lineID);
    };

    let _reDraw = function(){
        if(canvas == null)
        {
            console.log("_reDraw: Canvas not set");
            return;
        }
        let context = canvas.getContext("2d");
        context.clearRect(0, 0, context.canvas.width, context.canvas.height); // Clears the canvas

        context.strokeStyle = "#8510d8";
        context.lineJoin = "round";
        //context.lineWidth = sLineWidth;

        for(let i=0; i < clickX.length; i++) {
            context.beginPath();
            context.lineWidth = dotWidth[i];
            if(clickX[i] === dotX && clickY[i] === dotY)
                context.strokeStyle = "#5f9ea0";
            if(clickDrag[i] && i){
                context.moveTo(clickX[i-1], clickY[i-1]);
            }else{
                context.moveTo(clickX[i]-1, clickY[i]);
            }
            context.lineTo(clickX[i], clickY[i]);
            context.closePath();
            context.stroke();
        }
    };

    let _setLineWidth = function(newLineWith)
    {
        sLineWidth = newLineWith;
        if(_isDotActive())
            dotWidth[dotWidth.length-1] = sLineWidth;
        _reDraw();
    };

    //converts canvas to blob
    let _canvasToFile = function(){
        if(canvas == null)
        {
            console.log("_canvasToFile: Canvas not set");
            return;
        }
        //notify!
        //TODO: add option to ignore/dismiss
        if(_getPaintAmount() > 60000)
            alert("WARNING: you used to much paint and the image processing might fail, please select a smaller area if the results aren't good.\nFYI: you can process a image multiple times");
        var canvasBase64 = canvas.toDataURL();
        return convertBase64ToFile(canvasBase64);
    };

    //Warn user when he s using to much paint! (server might not be able to handle in time)
    let _getPaintAmount = function()
    {
        let lineDistance = 0;
        for(let i = 0; i < clickX.length; i++)
        {
            if(clickDrag[i] && i){
                lineDistance = lineDistance + (Math.sqrt(((clickX[i] - clickX[i-1])*(clickX[i] - clickX[i-1])) + ((clickY[i] - clickY[i-1])*(clickY[i] - clickY[i-1]))) * dotWidth[i]);
            }
        }
        return lineDistance
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

    //snippets from Stackoverflow
    function _arrayBufferToBase64( buffer ) {
        let binary = '';
        let bytes = new Uint8Array( buffer );
        let len = bytes.byteLength;
        for (var i = 0; i < len; i++) {
            binary += String.fromCharCode( bytes[ i ] );
        }
        return window.btoa( binary );
    }

    //MouseEvents for Drawing
    let _OnMouseDown = function(e, offsetLeft, offsetTop, f){
        lineID++;
        paint = true;
        if(e.pageX) // desktop detected
        {
            lastClickX = (e.pageX) * f;
            lastClickY = (e.pageY) * f;
            if(!_isDotActive())
            {
                _addClick(lastClickX, lastClickY);
                _initDot(lastClickX, lastClickY);
            }else{
                let dist = Math.sqrt(((lastClickX - dotX)*(lastClickX - dotX)) + ((lastClickY - dotY)*(lastClickY - dotY)));
                if(dist <= sLineWidth * 0.55)
                {
                    _killDot();
                }
            }
            _reDraw();
        }else if(e.originalEvent.changedTouches) // mobile detected
        {
            lastClickX = (e.originalEvent.changedTouches[0].pageX - offsetLeft) * f;
            lastClickY = (e.originalEvent.changedTouches[0].pageY - offsetLeft) * f;
            if(!_isDotActive())
            {
                _addClick(lastClickX, lastClickY);
                _initDot(lastClickX, lastClickY);
            }else{
                let dist = Math.sqrt(((lastClickX - dotX)*(lastClickX - dotX)) + ((lastClickY - dotY)*(lastClickY - dotY)));
                console.log("distance: " + dist);
                if(dist <= sLineWidth * 0.55)
                {
                    _killDot();
                    console.log("kill");
                }
            }
            _reDraw();
        }
    };

    let _OnMouseMove = function(e, offsetLeft, offsetTop, f){
        if(paint) {
            if(e.pageX)
            {
                if(_isDotActive())
                {
                    //Do math :D
                    let newDotX = dotX + (((e.pageX - offsetLeft) * f) - lastClickX);
                    let newDotY = dotY + (((e.pageY - offsetLeft) * f) - lastClickY);

                    _addClick(newDotX, newDotY, true);
                    dotX = newDotX;
                    dotY = newDotY;
                    //_addClick((e.originalEvent.changedTouches[0].pageX - offsetLeft) * f, (e.originalEvent.changedTouches[0].pageY - offsetTop) * f, true);
                    lastClickX = (e.pageX - offsetLeft) * f;
                    lastClickY = (e.pageY - offsetLeft) * f;
                    _reDraw();
                }
                _reDraw();
            }else if(e.originalEvent.changedTouches)
            {
                if(_isDotActive())
                {
                    //Do Math :D
                    let newDotX = dotX + (((e.originalEvent.changedTouches[0].pageX - offsetLeft) * f) - lastClickX);
                    let newDotY = dotY + (((e.originalEvent.changedTouches[0].pageY - offsetLeft) * f) - lastClickY);

                    _addClick(newDotX, newDotY, true);
                    dotX = newDotX;
                    dotY = newDotY;
                    //_addClick((e.originalEvent.changedTouches[0].pageX - offsetLeft) * f, (e.originalEvent.changedTouches[0].pageY - offsetTop) * f, true);
                    lastClickX = (e.originalEvent.changedTouches[0].pageX - offsetLeft) * f;
                    lastClickY = (e.originalEvent.changedTouches[0].pageY - offsetLeft) * f;
                    _reDraw();
                }
            }
        }
    };

    let _OnMouseUp = function(e)
    {
        paint = false;
    };

    let _OnMouseLeave = function(e) {
        paint = false;
    };

    let _getCanvas = function(){
        if(canvas == null)
        {
            console.log("_getCanvas: Canvas not set");
            return;
        }
        return canvas;
    };

    let _isCanvasUsed = function () {
        return clickX.length > 0;
    };

    let _removeLastPaint = function (){
        let LastPartID = linePart[linePart.length-1];
        let PartIdCount = 0;

        for(let i=0; i < linePart.length; i++) {
            if(linePart[i] === LastPartID)
            {
                PartIdCount++;
            }
        }

        _clearMask(linePart.length-PartIdCount);
    };

    let _paints = function(){
        return paint;
    };

    return {
        init: _init,
        canvasResize: _canvasResize,
        clearMask: _clearMask,
        canvasToFile: _canvasToFile,
        addClick: _addClick,
        reDraw: _reDraw,
        OnMouseDown: _OnMouseDown,
        OnMouseMove: _OnMouseMove,
        OnMouseUp: _OnMouseUp,
        OnMouseLeave: _OnMouseLeave,
        setCanvas: _setCanvas,
        getCanvas: _getCanvas,
        setLineWidth: _setLineWidth,
        removeCanvas: _removeCanvas,
        isCanvasUsed: _isCanvasUsed,
        paints: _paints,
        removeLastPaint: _removeLastPaint,
        isDotActive: _isDotActive,
        initDot: _initDot,
        killDot: _killDot,
        getDotXY: _geDotXY,
        getPaintAmount: _getPaintAmount
    };
}();

maskHandler.init();