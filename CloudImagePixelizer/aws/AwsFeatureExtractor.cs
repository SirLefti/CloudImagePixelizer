using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using SkiaSharp;
using Image = Amazon.Rekognition.Model.Image;

namespace CloudImagePixelizer.aws
{
    /// <summary>
    /// Feature extractor using the Amazon Web Services
    /// </summary>
    public class AwsFeatureExtractor : IFeatureExtractor
    {
        /// <summary>
        /// Constructor for a feature extractor using Amazon Web Services SDK, optimized for analysing a single image.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="accessKey"></param>
        /// <param name="secretKey"></param>
        /// <param name="endpoint"></param>
        public AwsFeatureExtractor(string imagePath, string accessKey, string secretKey, RegionEndpoint endpoint)
        {
            var size = System.Drawing.Image.FromFile(imagePath).Size;
            var orientation = SKCodec.Create(imagePath).EncodedOrigin;

            if ((int) orientation % 2 == 1)
            {
                // use standard bounding if image is not rotated or rotated by 180 degrees
                _height = size.Height;
                _width = size.Width;    
            }
            else
            {
                // flip height and width if image is rotated by 90 or 270 degrees
                _height = size.Width;
                _width = size.Height;
            }

            _rekognitionImage = new Image();

            using FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            var data = new byte[fs.Length];
            fs.Read(data, 0, (int) fs.Length);
            _rekognitionImage.Bytes = new MemoryStream(data);

            _client = new AmazonRekognitionClient(new BasicAWSCredentials(accessKey, secretKey), endpoint);
        }
        
        /// <summary>
        /// Constructor for a feature extractor using Amazon Web Services SDK, optimized for analysing a single image.
        /// </summary>
        /// <param name="imageStream"></param>
        /// <param name="accessKey"></param>
        /// <param name="secretKey"></param>
        /// <param name="endpoint"></param>
        public AwsFeatureExtractor(Stream imageStream, string accessKey, string secretKey, RegionEndpoint endpoint)
        {
            var size = System.Drawing.Image.FromStream(imageStream).Size;
            var orientation = SKCodec.Create(imageStream).EncodedOrigin;

            if ((int) orientation % 2 == 1)
            {
                // use standard bounding if image is not rotated or rotated by 180 degrees
                _height = size.Height;
                _width = size.Width;    
            }
            else
            {
                // flip height and width if image is rotated by 90 or 270 degrees
                _height = size.Width;
                _width = size.Height;
            }

            _rekognitionImage = new Image();
            
            var data = new byte[imageStream.Length];
            imageStream.Read(data, 0, (int) imageStream.Length);
            _rekognitionImage.Bytes = new MemoryStream(data);

            _client = new AmazonRekognitionClient(new BasicAWSCredentials(accessKey, secretKey), endpoint);
        }

        /// <summary>
        /// Constructor for a feature extractor using Amazon Web Services SDK, using a given annotator client,
        /// optimized to be used with a batch of images.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="client"></param>
        internal AwsFeatureExtractor(string imagePath, AmazonRekognitionClient client)
        {
            var size = System.Drawing.Image.FromFile(imagePath).Size;
            var orientation = SKCodec.Create(imagePath).EncodedOrigin;

            if ((int) orientation % 2 == 1)
            {
                // use standard bounding if image is not rotated or rotated by 180 degrees
                _height = size.Height;
                _width = size.Width;    
            }
            else
            {
                // flip height and width if image is rotated by 90 or 270 degrees
                _height = size.Width;
                _width = size.Height;
            }
            
            _rekognitionImage = new Image();

            using FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            var data = new byte[fs.Length];
            fs.Read(data, 0, (int) fs.Length);
            _rekognitionImage.Bytes = new MemoryStream(data);
            
            _client = client;
        }
        
        /// <summary>
        /// Constructor for a feature extractor using Amazon Web Services SDK, using a given annotator client,
        /// optimized to be used with a batch of images.
        /// </summary>
        /// <param name="imageStream"></param>
        /// <param name="client"></param>
        internal AwsFeatureExtractor(Stream imageStream, AmazonRekognitionClient client)
        {
            var size = System.Drawing.Image.FromStream(imageStream).Size;
            var orientation = SKCodec.Create(imageStream).EncodedOrigin;

            if ((int) orientation % 2 == 1)
            {
                // use standard bounding if image is not rotated or rotated by 180 degrees
                _height = size.Height;
                _width = size.Width;    
            }
            else
            {
                // flip height and width if image is rotated by 90 or 270 degrees
                _height = size.Width;
                _width = size.Height;
            }
            
            _rekognitionImage = new Image();
            
            var data = new byte[imageStream.Length];
            imageStream.Read(data, 0, (int) imageStream.Length);
            _rekognitionImage.Bytes = new MemoryStream(data);
            
            _client = client;
        }

        private readonly Image _rekognitionImage;
        private readonly AmazonRekognitionClient _client;
        private DetectLabelsResponse _objectsResponse;
        private DetectFacesResponse _facesResponse;
        private DetectTextResponse _textResponse;

        private readonly int _width;
        private readonly int _height;

        private readonly string[] _vehicles = {"Car", "Bus", "Motorcycle", "Truck"};

        public async Task<IEnumerable<Rectangle>> ExtractFacesAsync()
        {
            if (_facesResponse == null)
            {
                var facesRequest = new DetectFacesRequest()
                {
                    Image = _rekognitionImage
                };
                _facesResponse = await _client.DetectFacesAsync(facesRequest);
            }

            return ExtractFaces();
        }

        public IEnumerable<Rectangle> ExtractFaces()
        {
            if (_facesResponse == null)
            {
                var facesRequest = new DetectFacesRequest()
                {
                    Image = _rekognitionImage
                };
                _facesResponse = _client.DetectFacesAsync(facesRequest).Result;
            }

            return _facesResponse.FaceDetails.Select(f =>
                AmazonRekognitionCoordinateTranslator.RelativeBoxToAbsolute(f.BoundingBox, _width, _height));
        }

        public async Task<IEnumerable<Rectangle>> ExtractCarsAsync()
        {
            if (_objectsResponse == null)
            {
                var objectsRequest = new DetectLabelsRequest()
                {
                    Image = _rekognitionImage
                };
                _objectsResponse = await _client.DetectLabelsAsync(objectsRequest);
            }

            return ExtractCars();
        }

        public IEnumerable<Rectangle> ExtractCars()
        {
            if (_objectsResponse == null)
            {
                var objectsRequest = new DetectLabelsRequest()
                {
                    Image = _rekognitionImage
                };
                _objectsResponse = _client.DetectLabelsAsync(objectsRequest).Result;
            }

            return _objectsResponse.Labels.Where(l => _vehicles.Contains(l.Name))
                .Select(l => l.Instances).SingleOrDefault()?.Select(i =>
                    AmazonRekognitionCoordinateTranslator.RelativeBoxToAbsolute(i.BoundingBox,
                        _width, _height)) ?? Enumerable.Empty<Rectangle>();
        }

        public async Task<IEnumerable<Rectangle>> ExtractTextAsync()
        {
            if (_textResponse == null)
            {
                var textRequest = new DetectTextRequest()
                {
                    Image = _rekognitionImage
                };
                _textResponse = await _client.DetectTextAsync(textRequest);
            }

            return ExtractText();
        }

        public IEnumerable<Rectangle> ExtractText()
        {
            if (_textResponse == null)
            {
                var textRequest = new DetectTextRequest()
                {
                    Image = _rekognitionImage
                };
                _textResponse = _client.DetectTextAsync(textRequest).Result;
            }

            return _textResponse.TextDetections.Select(t => t.Geometry.Polygon)
                .Select(p => AmazonRekognitionCoordinateTranslator.RelativePolygonToAbsolute(p, _width,
                    _height));
        }

        public async Task<IEnumerable<Rectangle>> ExtractPersonsAsync()
        {
            if (_objectsResponse == null)
            {
                var objectsRequest = new DetectLabelsRequest()
                {
                    Image = _rekognitionImage
                };
                _objectsResponse = await _client.DetectLabelsAsync(objectsRequest);
            }

            return ExtractPersons();
        }

        public IEnumerable<Rectangle> ExtractPersons()
        {
            if (_objectsResponse == null)
            {
                var objectsRequest = new DetectLabelsRequest()
                {
                    Image = _rekognitionImage
                };
                _objectsResponse = _client.DetectLabelsAsync(objectsRequest).Result;
            }

            return _objectsResponse.Labels.Where(l => l.Name == "Person")
                .Select(l => l.Instances).SingleOrDefault()?.Select(i =>
                    AmazonRekognitionCoordinateTranslator.RelativeBoxToAbsolute(i.BoundingBox, _width,
                        _height)) ?? Enumerable.Empty<Rectangle>();
        }
        
        public async Task<IEnumerable<Rectangle>> ExtractLicensePlatesAsync()
        {
            if (_objectsResponse == null)
            {
                var objectsRequest = new DetectLabelsRequest()
                {
                    Image = _rekognitionImage
                };
                _objectsResponse = await _client.DetectLabelsAsync(objectsRequest);
            }

            return ExtractLicensePlates();
        }
        
        public IEnumerable<Rectangle> ExtractLicensePlates()
        {
            if (_objectsResponse == null)
            {
                var objectsRequest = new DetectLabelsRequest()
                {
                    Image = _rekognitionImage
                };
                _objectsResponse = _client.DetectLabelsAsync(objectsRequest).Result;
            }

            return _objectsResponse.Labels.Where(l => l.Name == "License Plate")
                .Select(l => l.Instances).SingleOrDefault()?.Select(i =>
                    AmazonRekognitionCoordinateTranslator.RelativeBoxToAbsolute(i.BoundingBox, _width,
                        _height)) ?? Enumerable.Empty<Rectangle>();
        }
    }
}