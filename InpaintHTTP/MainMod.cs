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
//using Zavolokas.SeamCarving; // TODO: fix package
using Zavolokas.Structures;
using Newtonsoft.Json;
using Nancy.Extensions;

namespace InpaintHTTP
{
    public class ApiSettings
    {
        public static ApiSettings _Instance { get; private set; }
        public InpaintSettings InpaintSettings { get; set; }
        
        // TODO: add Seamcarving Settings to the ApiSettings object ;D
        
        public static void Initialize()
        {
            _Instance = new ApiSettings();
        }

        public ApiSettings()
        {
            InitInpaintSettings();
        }
        private void InitInpaintSettings()
        {
            // init defaultSettings with environment variables for Inpainting Settings
            this.InpaintSettings = new InpaintSettings();

            int maxInpaintIterations = 0;
            if (!Int32.TryParse(Environment.GetEnvironmentVariable("MAX_INPAINT_ITERATIONS"), out maxInpaintIterations) && maxInpaintIterations != 0)
                this.InpaintSettings.MaxInpaintIterations = maxInpaintIterations;

            string patchDistanceEnvVar = Environment.GetEnvironmentVariable("PATCH_DISTANCE_CALCULATOR");
            if (patchDistanceEnvVar != null && patchDistanceEnvVar.Equals("Cie2000", StringComparison.OrdinalIgnoreCase))
                this.InpaintSettings.PatchDistanceCalculator = ImagePatchDistance.Cie2000;

            int patchSize;
            if (!Int32.TryParse(Environment.GetEnvironmentVariable("PATCH_SIZE"), out patchSize) && patchSize != 0)
                this.InpaintSettings.PatchSize = (byte)patchSize;
        }

    }
    public class MainMod : NancyModule
    {
        public MainMod()
        {
            // initialize default settings
            ApiSettings.Initialize();

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

                    // Convert body request to settings
                    InpaintSettings userSettings = new InpaintSettings();
                    try
                    {
                        userSettings = JsonConvert.DeserializeObject<InpaintSettings>(Request.Body.AsString());
                    }
                    catch { }

                    // Now merge then, giving priority to the default
                    if (userSettings.PatchSize > ApiSettings._Instance.InpaintSettings.PatchSize)
                        userSettings.PatchSize = ApiSettings._Instance.InpaintSettings.PatchSize;

                    if (userSettings.MaxInpaintIterations > ApiSettings._Instance.InpaintSettings.MaxInpaintIterations)
                        userSettings.MaxInpaintIterations = ApiSettings._Instance.InpaintSettings.MaxInpaintIterations;


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

            Get("/api/settings", async x =>
            {
                return Response.AsJson<ApiSettings>(ApiSettings._Instance);
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
