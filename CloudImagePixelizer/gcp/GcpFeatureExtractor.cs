using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Vision.V1;
using SkiaSharp;
using Image = Google.Cloud.Vision.V1.Image;

namespace CloudImagePixelizer.gcp
{
    /// <summary>
    /// Feature extractor using the Google Cloud Platform.
    /// </summary>
    public class GcpFeatureExtractor : IFeatureExtractor
    {
        /// <summary>
        /// Constructor for a feature extractor using Google Cloud Platform SDK, optimized for analysing a single image.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="credentialsPath"></param>
        public GcpFeatureExtractor(string imagePath, string credentialsPath)
        {
            SetRotation(SKCodec.Create(imagePath).EncodedOrigin);
            var size = System.Drawing.Image.FromFile(imagePath).Size;
            Width = size.Width;
            Height = size.Height;
            var visionImage = Image.FromFile(imagePath);
            _client = new ImageAnnotatorClientBuilder()
            {
                CredentialsPath = credentialsPath
            }.Build();
            _image = Image.FromFile(imagePath);
        }

        /// <summary>
        /// Constructor for a feature extractor using Google Cloud Platform SDK, optimized for analysing a single image.
        /// </summary>
        /// <param name="imageStream"></param>
        /// <param name="credentialsPath"></param>
        public GcpFeatureExtractor(Stream imageStream, string credentialsPath)
        {
            SetRotation(SKCodec.Create(imageStream).EncodedOrigin);
            var size = System.Drawing.Image.FromStream(imageStream).Size;
            Width = size.Width;
            Height = size.Height;
            var visionImage = Image.FromStream(imageStream);
            _client = new ImageAnnotatorClientBuilder()
            {
                CredentialsPath = credentialsPath
            }.Build();
            _image = Image.FromStream(imageStream);
        }

        /// <summary>
        /// Constructor for a feature extractor using Google Cloud Platform SDK, using a given annotator client,
        /// optimized to be used with a batch of images.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="client"></param>
        internal GcpFeatureExtractor(string imagePath, ImageAnnotatorClient client)
        {
            SetRotation(SKCodec.Create(imagePath).EncodedOrigin);
            var size = System.Drawing.Image.FromFile(imagePath).Size;
            Width = size.Width;
            Height = size.Height;
            _image = Image.FromFile(imagePath);
            _client = client;
        }

        /// <summary>
        /// Constructor for a feature extractor using Google Cloud Platform SDK, using a given annotator client,
        /// optimized to be used with a batch of images.
        /// </summary>
        /// <param name="imageStream"></param>
        /// <param name="client"></param>
        internal GcpFeatureExtractor(Stream imageStream, ImageAnnotatorClient client)
        {
            SetRotation(SKCodec.Create(imageStream).EncodedOrigin);
            var size = System.Drawing.Image.FromStream(imageStream).Size;
            Width = size.Width;
            Height = size.Height;
            _image = Image.FromStream(imageStream);
            _client = client;
        }
        
        protected GcpFeatureExtractor()
        {
        }

        /// <summary>
        /// Sets the rotation-to-apply-property to the right enum value. Currently, this supports only non-flipping
        /// origins since the translator only supports non-flipped images as well. The will be extended if I can get an
        /// image, that is flipped, otherwise this is practically not testable
        /// </summary>
        /// <param name="origin"></param>
        protected void SetRotation(SKEncodedOrigin origin)
        {
            switch (origin)
            {
                case SKEncodedOrigin.Default:
                    _rotationToApplyForFaceProcessing = RotateFlipType.RotateNoneFlipNone;
                    break;
                case SKEncodedOrigin.RightTop:
                    _rotationToApplyForFaceProcessing = RotateFlipType.Rotate90FlipNone;
                    break;
                case SKEncodedOrigin.BottomRight:
                    _rotationToApplyForFaceProcessing = RotateFlipType.Rotate180FlipNone;
                    break;
                case SKEncodedOrigin.LeftBottom:
                    _rotationToApplyForFaceProcessing = RotateFlipType.Rotate270FlipNone;
                    break;
            }
        }
        
        protected int Width;
        protected int Height;
        
        private RotateFlipType _rotationToApplyForFaceProcessing;
        private readonly Image _image;
        private readonly ImageAnnotatorClient _client;
        
        protected AnnotateImageResponse ObjectsResponse;
        protected AnnotateImageResponse TextResponse;
        protected AnnotateImageResponse FacesResponse;

        private readonly string[] _vehicles = {"Car", "Bus", "Motorcycle", "Truck"};

        public virtual IEnumerable<Rectangle> ExtractFaces()
        {
            FacesResponse ??= _client.Annotate(new AnnotateImageRequest()
            {
                Image = _image,
                Features =
                {
                    new Feature {Type = Feature.Types.Type.FaceDetection}
                }
            });

            return FacesResponse.FaceAnnotations
                .Select(f => GoogleVisionCoordinateTranslator.AbsolutePolyToRectangle(f.BoundingPoly))
                .Select(f => GoogleVisionCoordinateTranslator.RotateRectangle(f, _rotationToApplyForFaceProcessing, Width, Height));
        }

        public virtual IEnumerable<Rectangle> ExtractCars()
        {
            ObjectsResponse ??= _client.Annotate(new AnnotateImageRequest()
            {
                Image = _image,
                Features =
                {
                    new Feature {Type = Feature.Types.Type.ObjectLocalization}
                }
            });

            return ObjectsResponse.LocalizedObjectAnnotations.Where(e => _vehicles.Contains(e.Name))
                .Select(e => GoogleVisionCoordinateTranslator.RelativePolyToRectangle(e.BoundingPoly, Width, Height));
        }

        public virtual IEnumerable<Rectangle> ExtractText()
        {
            TextResponse ??= _client.Annotate(new AnnotateImageRequest()
            {
                Image = _image,
                Features =
                {
                    new Feature {Type = Feature.Types.Type.TextDetection}
                }
            });

            // filter text annotations that contains line breaks, this is probably the first one covering the whole image
            return TextResponse.TextAnnotations.Where(t => !t.Description.Contains("\n"))
                .Select(t => GoogleVisionCoordinateTranslator.AbsolutePolyToRectangle(t.BoundingPoly));
        }

        public virtual IEnumerable<Rectangle> ExtractPersons()
        {
            ObjectsResponse ??= _client.Annotate(new AnnotateImageRequest()
            {
                Image = _image,
                Features =
                {
                    new Feature {Type = Feature.Types.Type.ObjectLocalization}
                }
            });

            return ObjectsResponse.LocalizedObjectAnnotations.Where(e => e.Name == "Person")
                .Select(e => GoogleVisionCoordinateTranslator.RelativePolyToRectangle(e.BoundingPoly, Width, Height));
        }

        public IEnumerable<Rectangle> ExtractLicensePlates()
        {
            ObjectsResponse ??= _client.Annotate(new AnnotateImageRequest()
            {
                Image = _image,
                Features =
                {
                    new Feature {Type = Feature.Types.Type.ObjectLocalization}
                }
            });

            return ObjectsResponse.LocalizedObjectAnnotations.Where(e => e.Name == "License plate")
                .Select(e => GoogleVisionCoordinateTranslator.RelativePolyToRectangle(e.BoundingPoly, Width, Height));
        }

        public virtual async Task<IEnumerable<Rectangle>> ExtractFacesAsync()
        {
            FacesResponse ??= await _client.AnnotateAsync(new AnnotateImageRequest()
            {
                Image = _image,
                Features =
                {
                    new Feature {Type = Feature.Types.Type.FaceDetection}
                }
            });

            return ExtractFaces();
        }

        public virtual async Task<IEnumerable<Rectangle>> ExtractCarsAsync()
        {
            ObjectsResponse ??= await _client.AnnotateAsync(new AnnotateImageRequest()
            {
                Image = _image,
                Features =
                {
                    new Feature {Type = Feature.Types.Type.ObjectLocalization}
                }
            });

            return ExtractCars();
        }

        public virtual async Task<IEnumerable<Rectangle>> ExtractTextAsync()
        {
            TextResponse ??= await _client.AnnotateAsync(new AnnotateImageRequest()
            {
                Image = _image,
                Features =
                {
                    new Feature {Type = Feature.Types.Type.TextDetection}
                }
            });

            return ExtractText();
        }

        public virtual async Task<IEnumerable<Rectangle>> ExtractPersonsAsync()
        {
            ObjectsResponse ??= await _client.AnnotateAsync(new AnnotateImageRequest()
            {
                Image = _image,
                Features =
                {
                    new Feature {Type = Feature.Types.Type.ObjectLocalization}
                }
            });

            return ExtractPersons();
        }

        public async Task<IEnumerable<Rectangle>> ExtractLicensePlatesAsync()
        {
            ObjectsResponse ??= await _client.AnnotateAsync(new AnnotateImageRequest()
            {
                Image = _image,
                Features =
                {
                    new Feature {Type = Feature.Types.Type.ObjectLocalization}
                }
            });

            return ExtractLicensePlates();
        }
    }
}