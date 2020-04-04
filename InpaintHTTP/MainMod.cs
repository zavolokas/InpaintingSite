using System;
using Nancy;
using Nancy.ErrorHandling;
using System.Linq;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.Inpainting;
using Zavolokas.Structures;
using System.Threading.Tasks;
using Zavolokas.ImageProcessing.PatchMatch;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

//TODO: Cleanup the mess above & unused References

namespace InpaintHTTP
{
    public class MainMod : NancyModule
    {
        public MainMod()
        {
            Post("/api/inpaint", async x =>
            {
                try{
                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fffff") + "] Incomming request from " + this.Request.UserHostAddress);
                if (this.Request.Files.Count() < 2)
                {
                    Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fffff") + $"] Error, {this.Request.Files.Count()} files found");
                    return "Err";
                }
                  
                Bitmap BitmapImg = null, BitmapMask = null;
                var donors = new List<ZsImage>();
                foreach (var file in this.Request.Files)
                {
                    Bitmap tempBitmap;
                    byte[] ByteImg = new byte[file.Value.Length];
                    file.Value.Read(ByteImg, 0, (int)file.Value.Length);
                    using (MemoryStream ms = new MemoryStream(ByteImg))
                        tempBitmap = new Bitmap(ms);

                    if (BitmapImg == null)
                    {
                        BitmapImg = tempBitmap;
                    }
                    else if (BitmapMask == null)
                    {
                        BitmapMask = tempBitmap;
                    }
                    else {
                        donors.Add(tempBitmap.ToArgbImage());
                    }
                }

                var imageArgb = ConvertToArgbImage(BitmapImg);
                var markupArgb = ConvertToArgbImage(BitmapMask);

                var inpainter = new Inpainter();
                var settings = new InpaintSettings
                {
                    MaxInpaintIterations = 15,
                    PatchDistanceCalculator = ImagePatchDistance.Cie76
                };
                if (!Int32.TryParse(Environment.GetEnvironmentVariable("MAX_INPAINT_ITERATIONS"), out settings.MaxInpaintIterations))
                {
                    settings.MaxInpaintIterations = 15;
                }
                string patchDistanceEnvVar = Environment.GetEnvironmentVariable("PATCH_DISTANCE_CALCULATOR");
                if (patchDistanceEnvVar != null && patchDistanceEnvVar.Equals("Cie2000", StringComparison.OrdinalIgnoreCase)) {
                  settings.PatchDistanceCalculator = ImagePatchDistance.Cie2000;
                }

                Image finalResult = null;

                inpainter.IterationFinished += (sender, eventArgs) =>
                {
                    Bitmap iterationResult = eventArgs.InpaintedLabImage
                        .FromLabToRgb()
                        .FromRgbToBitmap();
                    finalResult = iterationResult;
                    Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fffff") + "] call on inpainter.IterationFinished (Level " + eventArgs.LevelIndex + ", Iteration " + eventArgs.InpaintIteration + ")"); //Debugging
                };

                await Task.Factory.StartNew(() => inpainter.Inpaint(imageArgb, markupArgb, settings, donors));

                Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fffff") + "] Processing finished");
#if DEBUG
                finalResult.Save(@"..\..\TESTAPP.PNG"); //Debugging
#endif

                MemoryStream stream = new MemoryStream();
                finalResult.Save(stream, ImageFormat.Png);
                return Convert.ToBase64String(stream.ToArray()); //this does the job ¯\_(ツ)_/¯
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return null;
                }
            });

            Get(@"/", _ =>
            {
                return Response.AsFile("TestWebsite/index.html", "text/html");
            });

            Get("/ping", _ => "Ping is successful");

        }

        private static ZsImage ConvertToArgbImage(Bitmap imageBitmap)
        {
            double maxSize = 2048;
            try {
                maxSize = Int32.Parse(Environment.GetEnvironmentVariable("MAX_IMAGE_DIMENSION"));
            }
            catch (ArgumentNullException) {}
            catch (FormatException)
            {
                Console.WriteLine("Invalid value for the MAX_IMAGE_DIMENSION. It must be an integer.");
            }
            catch (OverflowException)
            {
                Console.WriteLine("Invalid value for the MAX_IMAGE_DIMENSION. Out of 32-bit integer range.");
            }

            if (maxSize > 0) {
                if (imageBitmap.Width > maxSize || imageBitmap.Height > maxSize)
                {
                    var tmp = imageBitmap;
                    double percent = imageBitmap.Width > imageBitmap.Height
                        ? maxSize / imageBitmap.Width
                        : maxSize / imageBitmap.Height;
                    imageBitmap =
                        imageBitmap.CloneWithScaleTo((int)(imageBitmap.Width * percent), (int)(imageBitmap.Height * percent));
                    tmp.Dispose();
                }
            }

            var imageArgb = imageBitmap.ToArgbImage();
            return imageArgb;
        }

    }

    public class MyStatusHandler : IStatusCodeHandler
    {
        //TODO: return json error message?
        public bool HandlesStatusCode(global::Nancy.HttpStatusCode statusCode, NancyContext context)
        {
            return true;
        }

        public void Handle(global::Nancy.HttpStatusCode statusCode, NancyContext context)
        {
            return;
        }
    }
}
