using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SkiaSharp;

namespace CloudImagePixelizer
{
    public class CloudImagePixelizerClient
    {
        private readonly IConnector _cloudConnector;
        private string _imagePath;
        
        public Func<int, int, int, int, int> PixelSizeFunction = (imgWidth, imgHeight, patchWidth, patchHeight) => Math.Max(patchWidth, patchHeight) / 16;
        public double MergeFactor = .025;
        public SKEncodedImageFormat OutputFormat = SKEncodedImageFormat.Jpeg;
        public int OutputQuality = 100;
        public FaceProcessing FaceProcessing = FaceProcessing.PixelateFaces;
        public CarProcessing CarProcessing = CarProcessing.PixelatePlatesAndTextOnCars;
        public ILogger Logger = new NullLogger();
        public SKPaint Outline;

        public CloudImagePixelizerClient(IConnector cloudConnector)
        {
            _cloudConnector = cloudConnector;
        }

        public async Task<Stream> PixelateSingleImage(string imagePath)
        {
            _imagePath = imagePath;
            var bitmap = SKBitmap.Decode(imagePath);
            var origin = SKCodec.Create(imagePath).EncodedOrigin;
            return await Pixelate(bitmap, origin, _cloudConnector.AnalyseImage(imagePath));
        }

        // TODO cannot extract origin from a file stream, because codec from stream will be null
        public async Task<Stream> PixelateSingleImage(FileStream imageStream)
        {
            _imagePath = imageStream.Name;
            byte[] bytes = new BinaryReader(imageStream).ReadBytes((int) imageStream.Length);
            var bitmap = SKBitmap.Decode(bytes);
            return await Pixelate(bitmap, SKEncodedOrigin.Default, _cloudConnector.AnalyseImage(imageStream));
        }

        public async Task PixelateSingleImage(string imagePath, string outputPath)
        {
            _imagePath = imagePath;
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            var outputDirectory = new FileInfo(outputPath).Directory;
            if (outputDirectory != null && !outputDirectory.Exists)
            {
                outputDirectory.Create();
            }

            var bitmap = SKBitmap.Decode(imagePath);
            var origin = SKCodec.Create(imagePath).EncodedOrigin;

            var output = await Pixelate(bitmap, origin, _cloudConnector.AnalyseImage(imagePath));
            await using var fileStream = File.Create(outputPath);
            await output.CopyToAsync(fileStream);
        }

        public async Task PixelateImageBatch(string inputDirectory, string outputDirectory, bool recursively)
        {
            var imageFiles = GetImageFiles(inputDirectory, _cloudConnector.SupportedFileExtensions, recursively);
            foreach (var imageFile in imageFiles)
            {
                _imagePath = imageFile;
                await PixelateSingleImage(Path.Combine(inputDirectory, imageFile),
                    Path.Combine(outputDirectory, imageFile));
            }
        }

        private static IEnumerable<string> GetImageFiles(string rootPath, string[] allowedExtensions, bool recursively)
        {
            var root = new DirectoryInfo(rootPath);
            var imageFiles = root.GetFiles()
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                .Where(f => allowedExtensions.Contains(f.Extension))
                .Select(f => f.Name);
            if (!recursively) return imageFiles;

            foreach (var directory in root.GetDirectories())
            {
                imageFiles = imageFiles.Concat(GetImageFiles(directory.FullName, allowedExtensions, recursively)
                    .Select(f => directory.FullName.Substring(rootPath.Length + 1) + Path.DirectorySeparatorChar + f));
            }

            return imageFiles;
        }

        private async Task<Stream> Pixelate(SKBitmap bitmap, SKEncodedOrigin origin, IFeatureExtractor featureExtractor)
        {
            // fix orientation if encoded origin is not TopLeft/Default
            bitmap = FixOrientation(bitmap, origin);

            // a surface is something like a table we need to actually draw stuff
            var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
            // get the canvas where we actually draw onto
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            // apply the original image to the canvas
            canvas.DrawBitmap(bitmap, 0, 0);

            // extract the image features
            switch (FaceProcessing)
            {
                case FaceProcessing.PixelateFaces:
                {
                    var faces = (await featureExtractor.ExtractFacesAsync()).ToArray();
                    Logger.OnExtractedFaces(_imagePath, faces);
                    foreach (var face in faces)
                    {
                        if (Outline != null)
                        {
                            Pixelate(canvas, SKRectI.Create(face.X, face.Y, face.Width, face.Height), bitmap, Outline,
                                PixelSizeFunction);
                        }
                        else
                        {
                            Pixelate(canvas, SKRectI.Create(face.X, face.Y, face.Width, face.Height), bitmap,
                                PixelSizeFunction);
                        }

                        Logger.OnPixelatedFace(_imagePath, face);
                    }

                    break;
                }
                case FaceProcessing.PixelatePersons:
                {
                    var persons = (await featureExtractor.ExtractPersonsAsync()).ToArray();
                    Logger.OnExtractedPersons(_imagePath, persons);
                    foreach (var person in persons)
                    {
                        if (Outline != null)
                        {
                            Pixelate(canvas, SKRectI.Create(person.X, person.Y, person.Width, person.Height), bitmap,
                                Outline, PixelSizeFunction);
                        }
                        else
                        {
                            Pixelate(canvas, SKRectI.Create(person.X, person.Y, person.Width, person.Height), bitmap,
                                PixelSizeFunction);
                        }

                        Logger.OnPixelatedPerson(_imagePath, person);
                    }

                    break;
                }
                case FaceProcessing.Skip:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (CarProcessing)
            {
                case CarProcessing.PixelatePlatesAndTextOnCars:
                {
                    var text = (await featureExtractor.ExtractTextAsync()).ToArray();
                    Logger.OnExtractedText(_imagePath, text);
                    var cars = (await featureExtractor.ExtractCarsAsync()).ToArray();
                    Logger.OnExtractedCars(_imagePath, cars);
                    var licensePlates = (await featureExtractor.ExtractLicensePlatesAsync()).ToArray();
                    Logger.OnExtractedLicensePlates(_imagePath, licensePlates);

                    var mergeDistance = (int) (bitmap.Width * MergeFactor);
                    var merged = ImagePatchClusterizer.Clusterize(text, mergeDistance).ToArray();

                    foreach (var car in cars)
                    {
                        foreach (var patch in merged)
                        {
                            // If patch is inside of the car borders
                            if (patch.Y >= car.Y
                                && patch.X >= car.X
                                && patch.Y <= car.Y + car.Height
                                && patch.X <= car.X + car.Width
                                && patch.Width <= car.Width + car.X - patch.X
                                && patch.Height <= car.Height + car.Y - patch.Y)
                            {
                                if (Outline != null)
                                {
                                    Pixelate(canvas, SKRectI.Create(patch.X, patch.Y, patch.Width, patch.Height),
                                        bitmap, Outline, PixelSizeFunction);
                                }
                                else
                                {
                                    Pixelate(canvas, SKRectI.Create(patch.X, patch.Y, patch.Width, patch.Height),
                                        bitmap, PixelSizeFunction);
                                }

                                Logger.OnPixelatedText(_imagePath, patch);
                            }
                        }
                    }

                    foreach (var plate in licensePlates)
                    {
                        if (Outline != null)
                        {
                            Pixelate(canvas, SKRectI.Create(plate.X, plate.Y, plate.Width, plate.Height), bitmap,
                                Outline, PixelSizeFunction);
                        }
                        else
                        {
                            Pixelate(canvas, SKRectI.Create(plate.X, plate.Y, plate.Width, plate.Height), bitmap,
                                PixelSizeFunction);
                        }

                        Logger.OnPixelatedLicensePlate(_imagePath, plate);
                    }

                    break;
                }
                case CarProcessing.PixelateCars:
                {
                    var cars = (await featureExtractor.ExtractCarsAsync()).ToArray();
                    Logger.OnExtractedCars(_imagePath, cars);
                    foreach (var car in cars)
                    {
                        if (Outline != null)
                        {
                            Pixelate(canvas, SKRectI.Create(car.X, car.Y, car.Width, car.Height), bitmap, Outline,
                                PixelSizeFunction);
                        }
                        else
                        {
                            Pixelate(canvas, SKRectI.Create(car.X, car.Y, car.Width, car.Height), bitmap,
                                PixelSizeFunction);
                        }

                        Logger.OnPixelatedCar(_imagePath, car);
                    }

                    break;
                }
                case CarProcessing.Skip:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return surface.Snapshot().Encode(OutputFormat, OutputQuality).AsStream();
        }

        /// <summary>
        /// Pixelates a given image area defined by extractRect of the original image and draws it to the canvas.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="extractRect"></param>
        /// <param name="original"></param>
        /// <param name="outline"></param>
        /// <param name="pixelSizeFunction"></param>
        private static void Pixelate(SKCanvas canvas, SKRectI extractRect, SKBitmap original, SKPaint outline,
            Func<int, int, int, int, int> pixelSizeFunction)
        {
            Pixelate(canvas, extractRect, original, pixelSizeFunction);
            canvas.DrawRect(extractRect, outline);
        }

        /// <summary>
        /// Pixelates a given image area defined by extractRect of the original image and draws it to the canvas.
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="extractRect"></param>
        /// <param name="original"></param>
        /// <param name="pixelSizeFunction"></param>
        private static void Pixelate(SKCanvas canvas, SKRectI extractRect, SKBitmap original,
            Func<int, int, int, int, int> pixelSizeFunction)
        {
            var pixelSize =
                pixelSizeFunction.Invoke(original.Width, original.Height, extractRect.Width, extractRect.Height);

            var downscaled = new SKBitmap(extractRect.Width / pixelSize, extractRect.Height / pixelSize);
            var upscaled = new SKBitmap(extractRect.Width, extractRect.Height);
            var sub = new SKBitmap();
            original.ExtractSubset(sub, extractRect);
            sub.ScalePixels(downscaled, SKFilterQuality.None);
            downscaled.ScalePixels(upscaled, SKFilterQuality.None);
            canvas.DrawBitmap(upscaled, extractRect);
        }

        /// <summary>
        /// If the image has a different orientation than the default, this method rotates the image as it would be
        /// displayed in your image viewer. Cloud image analyzes are also usually returning coordinates of objects as
        /// they would appear on the image as displayed in your image viewer, so this is necessary to pixelate the right
        /// parts of the image. 
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        private static SKBitmap FixOrientation(SKBitmap bitmap, SKEncodedOrigin orientation)
        {
            SKBitmap rotated;
            switch (orientation)
            {
                case SKEncodedOrigin.BottomRight:

                    using (var canvas = new SKCanvas(bitmap))
                    {
                        canvas.RotateDegrees(180, bitmap.Width / 2, bitmap.Height / 2);
                        canvas.DrawBitmap(bitmap.Copy(), 0, 0);
                    }

                    return bitmap;

                case SKEncodedOrigin.RightTop:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);

                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Translate(rotated.Width, 0);
                        canvas.RotateDegrees(90);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }

                    return rotated;

                case SKEncodedOrigin.LeftBottom:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);

                    using (var canvas = new SKCanvas(rotated))
                    {
                        canvas.Translate(0, rotated.Height);
                        canvas.RotateDegrees(270);
                        canvas.DrawBitmap(bitmap, 0, 0);
                    }

                    return rotated;

                default:
                    return bitmap;
            }
        }
    }
}