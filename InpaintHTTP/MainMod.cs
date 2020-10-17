using Nancy;
using Nancy.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Zavolokas.GdiExtensions;
using Zavolokas.ImageProcessing.Inpainting;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;
using Newtonsoft.Json;
using Nancy.Extensions;

namespace InpaintHTTP
{
    public class MainMod : NancyModule
    {
        public static InpaintSettings defaultSettings = new InpaintSettings();
        public MainMod()
        {
            // init defaultSettings with environment variables
            Int32.TryParse(Environment.GetEnvironmentVariable("MAX_INPAINT_ITERATIONS"), out defaultSettings.MaxInpaintIterations);

            string patchDistanceEnvVar = Environment.GetEnvironmentVariable("PATCH_DISTANCE_CALCULATOR");
            if(patchDistanceEnvVar != null && patchDistanceEnvVar.Equals("Cie2000", StringComparison.OrdinalIgnoreCase))
                defaultSettings.PatchDistanceCalculator = ImagePatchDistance.Cie2000;

            int patchSize;
            if(!Int32.TryParse(Environment.GetEnvironmentVariable("PATCH_SIZE"), out patchSize));
                defaultSettings.PatchSize = (byte)patchSize;

            Post("/api/inpaint", async x =>
            {
                try
                {
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
                        else
                        {
                            donors.Add(tempBitmap.ToArgbImage());
                        }
                    }

                    var imageArgb = ConvertToArgbImage(BitmapImg);
                    var markupArgb = ConvertToArgbImage(BitmapMask);

                    var inpainter = new Inpainter();

                    // NOTE: This should no longer be used since the creation of InpaintSettings comes with default variables itself?
                    //var settings = new InpaintSettings
                    //{
                    //    MaxInpaintIterations = 15,
                    //    PatchDistanceCalculator = ImagePatchDistance.Cie76,
                    //    PatchSize = 11
                    //};

                    // Convert body request to settings
                    InpaintSettings userSettings = new InpaintSettings();
                    try
                    {
                        userSettings = JsonConvert.DeserializeObject<InpaintSettings>(Request.Body.AsString());
                    }
                    catch { }

                    // NOTE: Now merge then? giving priority to the default one?

                    if (userSettings.PatchSize > defaultSettings.PatchSize)
                        userSettings.PatchSize = defaultSettings.PatchSize;

                    if (userSettings.MaxInpaintIterations > defaultSettings.MaxInpaintIterations)
                        userSettings.MaxInpaintIterations = defaultSettings.MaxInpaintIterations;


                    // NOTE: I think we can remove the old Forum propertys and use the above JSON parsed ones?
                    
                    //// amount of iterations will be run to find better values for the area to fill
                    //if (!Int32.TryParse(Request.Form["MAX_INPAINT_ITERATIONS"], out settings.MaxInpaintIterations)) // set API value if present
                    //    Int32.TryParse(Environment.GetEnvironmentVariable("MAX_INPAINT_ITERATIONS"), out settings.MaxInpaintIterations); // set environment default

                    //// determines the algorithm to use for calculating color differences
                    //string patchDistanceEnvVar = Request.Form["PATCH_DISTANCE_CALCULATOR"];
                    //if(patchDistanceEnvVar == null)
                    //    patchDistanceEnvVar = Environment.GetEnvironmentVariable("PATCH_DISTANCE_CALCULATOR");

                    //if (patchDistanceEnvVar != null && patchDistanceEnvVar.Equals("Cie2000", StringComparison.OrdinalIgnoreCase))
                    //    settings.PatchDistanceCalculator = ImagePatchDistance.Cie2000;

                    //// PATCH_SIZE
                    //int patchSize = settings.PatchSize;
                    //if (!Int32.TryParse(Request.Form["PATCH_SIZE"], out patchSize))
                    //    Int32.TryParse(Environment.GetEnvironmentVariable("PATCH_SIZE"), out patchSize);

                    //settings.PatchSize = (byte)patchSize;


                    Image finalResult = null;

                    inpainter.IterationFinished += (sender, eventArgs) =>
                    {
                        Bitmap iterationResult = eventArgs.InpaintedLabImage
                            .FromLabToRgb()
                            .FromRgbToBitmap();
                        finalResult = iterationResult;
                        Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fffff") + "] call on inpainter.IterationFinished (Level " + eventArgs.LevelIndex + ", Iteration " + eventArgs.InpaintIteration + ")"); //Debugging
                    };

                    await Task.Factory.StartNew(() => inpainter.Inpaint(imageArgb, markupArgb, userSettings, donors));

                    Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fffff") + "] Processing finished");
#if DEBUG
                    finalResult.Save(@"..\..\TESTAPP.PNG"); //Debugging
#endif

                    MemoryStream stream = new MemoryStream();
                    finalResult.Save(stream, ImageFormat.Png);
                    return Convert.ToBase64String(stream.ToArray()); //this does the job ¯\_(ツ)_/¯
                }
                catch (Exception ex)
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
            try
            {
                maxSize = Int32.Parse(Environment.GetEnvironmentVariable("MAX_IMAGE_DIMENSION"));
            }
            catch (ArgumentNullException) { }
            catch (FormatException)
            {
                Console.WriteLine("Invalid value for the MAX_IMAGE_DIMENSION. It must be an integer.");
            }
            catch (OverflowException)
            {
                Console.WriteLine("Invalid value for the MAX_IMAGE_DIMENSION. Out of 32-bit integer range.");
            }

            if (maxSize > 0)
            {
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
