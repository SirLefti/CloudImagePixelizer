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
        public int PixelSize = 32;
        public double MergeFactor = .025;
        public SKEncodedImageFormat OutputFormat = SKEncodedImageFormat.Jpeg;
        public int OutputQuality = 100;
        public FaceProcessing FaceProcessing = FaceProcessing.PixelateFaces;
        public CarProcessing CarProcessing = CarProcessing.PixelatePlatesAndTextOnCars;

        public CloudImagePixelizerClient(IConnector cloudConnector)
        {
            _cloudConnector = cloudConnector;
        }

        public async Task<Stream> PixelateSingleImage(string imagePath)
        {
            var bitmap = SKBitmap.Decode(imagePath);
            var origin = SKCodec.Create(imagePath).EncodedOrigin;
            return await Pixelate(bitmap, origin, _cloudConnector.AnalyseImage(imagePath));
        }
        
        // TODO cannot extract origin from a file stream, because codec from stream will be null
        public async Task<Stream> PixelateSingleImage(FileStream imageStream)
        {
            byte[] bytes = new BinaryReader(imageStream).ReadBytes((int) imageStream.Length);
            var bitmap = SKBitmap.Decode(bytes);
            return await Pixelate(bitmap, SKEncodedOrigin.Default, _cloudConnector.AnalyseImage(imageStream));
        }

        public async Task PixelateSingleImage(string imagePath, string outputPath)
        {
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
                    var faces = await featureExtractor.ExtractFacesAsync();
                    foreach (var face in faces)
                    {
                        Pixelate(canvas, SKRectI.Create(face.X, face.Y, face.Width, face.Height), bitmap,
                            PixelSize);
                    }

                    break;
                }
                case FaceProcessing.PixelatePersons:
                {
                    var persons = await featureExtractor.ExtractPersonsAsync();
                    foreach (var person in persons)
                    {
                        Pixelate(canvas, SKRectI.Create(person.X, person.Y, person.Width, person.Height), bitmap,
                            PixelSize);
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
                    var text = await featureExtractor.ExtractTextAsync();
                    var cars = await featureExtractor.ExtractCarsAsync();
                    var licensePlates = await featureExtractor.ExtractLicensePlatesAsync();

                    var mergeDistance = (int) (bitmap.Width * MergeFactor);
                    var merged = ImagePatchClusterizer.Clusterize(text, mergeDistance);

                    foreach (var plate in licensePlates)
                    {
                        Pixelate(canvas, SKRectI.Create(plate.X, plate.Y, plate.Width, plate.Height), bitmap,
                            PixelSize);
                    }

                    foreach (var car in cars)
                    {
                        foreach (var patch in merged)
                        {
                            // If patch is inside of the car borders
                            if (patch.Y >= car.Y && patch.X >= car.X && patch.Height - patch.Y <= car.Height - car.Y &&
                                patch.Width - patch.X <= car.Width - car.X)
                            {
                                Pixelate(canvas, SKRectI.Create(patch.X, patch.Y, patch.Width, patch.Height), bitmap,
                                    PixelSize);
                            }
                        }
                    }

                    break;
                }
                case CarProcessing.PixelateCars:
                {
                    var cars = await featureExtractor.ExtractCarsAsync();

                    foreach (var car in cars)
                    {
                        Pixelate(canvas, SKRectI.Create(car.X, car.Y, car.Width, car.Height), bitmap, PixelSize);
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
        /// <param name="pixelSize"></param>
        private static void Pixelate(SKCanvas canvas, SKRectI extractRect, SKBitmap original, int pixelSize)
        {
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