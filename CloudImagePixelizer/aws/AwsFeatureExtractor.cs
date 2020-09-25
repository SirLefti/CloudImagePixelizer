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
            Height = size.Height;
            Width = size.Width;

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
            Height = size.Height;
            Width = size.Width;

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
            Height = size.Height;
            Width = size.Width;
            
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
            Height = size.Height;
            Width = size.Width;
            
            _rekognitionImage = new Image();
            
            var data = new byte[imageStream.Length];
            imageStream.Read(data, 0, (int) imageStream.Length);
            _rekognitionImage.Bytes = new MemoryStream(data);
            
            _client = client;
        }

        protected AwsFeatureExtractor()
        {
        }

        private readonly Image _rekognitionImage;
        private readonly AmazonRekognitionClient _client;
        protected DetectLabelsResponse ObjectsResponse;
        protected DetectFacesResponse FacesResponse;
        protected DetectTextResponse TextResponse;

        protected int Width;
        protected int Height;

        public async Task<IEnumerable<Rectangle>> ExtractFacesAsync()
        {
            if (FacesResponse == null)
            {
                var facesRequest = new DetectFacesRequest()
                {
                    Image = _rekognitionImage
                };
                FacesResponse = await _client.DetectFacesAsync(facesRequest);
            }

            return FacesResponse.FaceDetails.Select(f =>
                AmazonRekognitionCoordinateTranslator.RelativeBoxToAbsolute(f.BoundingBox, Width, Height));
        }

        public IEnumerable<Rectangle> ExtractFaces()
        {
            if (FacesResponse == null)
            {
                var facesRequest = new DetectFacesRequest()
                {
                    Image = _rekognitionImage
                };
                FacesResponse = _client.DetectFacesAsync(facesRequest).Result;
            }

            return FacesResponse.FaceDetails.Select(f =>
                AmazonRekognitionCoordinateTranslator.RelativeBoxToAbsolute(f.BoundingBox, Width, Height));
        }

        public async Task<IEnumerable<Rectangle>> ExtractCarsAsync()
        {
            if (ObjectsResponse == null)
            {
                var objectsRequest = new DetectLabelsRequest()
                {
                    Image = _rekognitionImage
                };
                ObjectsResponse = await _client.DetectLabelsAsync(objectsRequest);
            }

            return ObjectsResponse.Labels.Where(l => l.Name == "Car")
                .Select(l => l.Instances).SingleOrDefault()?.Select(i =>
                    AmazonRekognitionCoordinateTranslator.RelativeBoxToAbsolute(i.BoundingBox,
                        Width, Height)) ?? Enumerable.Empty<Rectangle>();
        }

        public IEnumerable<Rectangle> ExtractCars()
        {
            if (ObjectsResponse == null)
            {
                var objectsRequest = new DetectLabelsRequest()
                {
                    Image = _rekognitionImage
                };
                ObjectsResponse = _client.DetectLabelsAsync(objectsRequest).Result;
            }

            return ObjectsResponse.Labels.Where(l => l.Name == "Car")
                .Select(l => l.Instances).SingleOrDefault()?.Select(i =>
                    AmazonRekognitionCoordinateTranslator.RelativeBoxToAbsolute(i.BoundingBox,
                        Width, Height)) ?? Enumerable.Empty<Rectangle>();
        }

        public async Task<IEnumerable<Rectangle>> ExtractTextAsync()
        {
            if (TextResponse == null)
            {
                var textRequest = new DetectTextRequest()
                {
                    Image = _rekognitionImage
                };
                TextResponse = await _client.DetectTextAsync(textRequest);
            }

            return TextResponse.TextDetections.Select(t => t.Geometry.Polygon)
                .Select(p => AmazonRekognitionCoordinateTranslator.RelativePolygonToAbsolute(p, Width,
                    Height));
        }

        public IEnumerable<Rectangle> ExtractText()
        {
            if (TextResponse == null)
            {
                var textRequest = new DetectTextRequest()
                {
                    Image = _rekognitionImage
                };
                TextResponse = _client.DetectTextAsync(textRequest).Result;
            }

            return TextResponse.TextDetections.Select(t => t.Geometry.Polygon)
                .Select(p => AmazonRekognitionCoordinateTranslator.RelativePolygonToAbsolute(p, Width,
                    Height));
        }

        public async Task<IEnumerable<Rectangle>> ExtractPersonsAsync()
        {
            if (ObjectsResponse == null)
            {
                var objectsRequest = new DetectLabelsRequest()
                {
                    Image = _rekognitionImage
                };
                ObjectsResponse = await _client.DetectLabelsAsync(objectsRequest);
            }

            return ObjectsResponse.Labels.Where(l => l.Name == "Person")
                .Select(l => l.Instances).SingleOrDefault()?.Select(i =>
                    AmazonRekognitionCoordinateTranslator.RelativeBoxToAbsolute(i.BoundingBox, Width,
                        Height)) ?? Enumerable.Empty<Rectangle>();
        }

        public IEnumerable<Rectangle> ExtractPersons()
        {
            if (ObjectsResponse == null)
            {
                var objectsRequest = new DetectLabelsRequest()
                {
                    Image = _rekognitionImage
                };
                ObjectsResponse = _client.DetectLabelsAsync(objectsRequest).Result;
            }

            return ObjectsResponse.Labels.Where(l => l.Name == "Person")
                .Select(l => l.Instances).SingleOrDefault()?.Select(i =>
                    AmazonRekognitionCoordinateTranslator.RelativeBoxToAbsolute(i.BoundingBox, Width,
                        Height)) ?? Enumerable.Empty<Rectangle>();
        }
    }
}