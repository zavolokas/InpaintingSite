# InpaintingSite

A web interface for inpainting. See [zavolokas/Inpainting](https://github.com/zavolokas/Inpainting/blob/master/README.md) for algorithm details.

## Environment Variables

When running this application, you can use environmental variables to change a few of the inpainting settings. These can be set when starting the Docker container with the `-e` option.

| Variable Name | Values |
| ------------- | ------ |
| MAX_INPAINT_ITERATIONS | The equivalent of the MaxInpaintIterations setting of the underlying library: `MaxInpaintIterations determines how many iterations will be run to find better values for the area to fill. The more iterations you run, the better the result you'll get.`<br />Any integer number larger than 0 is allowed.<br />Default value: `15`. |
| PATCH_DISTANCE_CALCULATOR | The equivalent of the PatchDistanceCalculator setting of the underlying library: `PatchDistanceCalculator determines the algorithm to use for calculating color differences`.<br />Allowed values are `Cie76` (fastest) and `Cie2000` (more accurate).<br/>Default value: `Cie76`|
| MAX_IMAGE_DIMENSION | This sets a limit on the size of an image and mask before processing. If either dimension of an uploaded image is larger than MAX_IMAGE_DIMENSION, it will be scaled down to fit within the specified dimensions while preserving the image's aspect ratio.<br />Any integer number is allowed. If the number is less than or equal to 0, there will be no limit on the image size.<br />Default value: `2048` |
