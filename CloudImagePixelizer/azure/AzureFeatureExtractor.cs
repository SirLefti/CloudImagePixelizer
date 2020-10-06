using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CloudImagePixelizer.azure
{
    public class AzureFeatureExtractor : IFeatureExtractor
    {
        private const string BaseUrl = "{0}vision/v3.0/{1}";
        private const string TextDetection = "ocr";
        private const string ObjectDetection = "detect";
        private const string FaceDetection = "analyze?visualFeatures=Faces";

        private readonly HttpClient _client;
        private readonly ByteArrayContent _content;
        private readonly string _endpoint;

        // serializer settings for analysis responses detecting faces and objects. No need for custom converters, but
        // stupid enough that objects and faces are using different bounding box classes. Only microsoft knows why.
        private static readonly JsonSerializerSettings ObjectSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        // serializer settings for ocr responses because they are dumping the coordinates comma-separated in a string
        // for some stupid reason. Have fun parsing this (that's the reason why OcrBoundingBoxConverter exists)
        private static readonly JsonSerializerSettings OcrSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new List<JsonConverter> {new OcrBoundingBoxConverter()}
        };

        /// <summary>
        /// Constructor for a feature extractor using Microsoft Azure API. Endpoint and key are located in your azure
        /// resource under "resource management" in "keys and endpoint". Use the whole endpoint as string which should
        /// look like <a href="https://your-resource-endpoint.cognitiveservices.azure.com/"/>. 
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="endpoint"></param>
        /// <param name="key"></param>
        public AzureFeatureExtractor(string imagePath, string endpoint, string key)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
            _content = new ByteArrayContent(File.ReadAllBytes(imagePath));
            _content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            
            _endpoint = endpoint;
        }

        /// <summary>
        /// Constructor for a feature extractor using Microsoft Azure API. Endpoint and key are located in your azure
        /// resource under "resource management" in "keys and endpoint". Use the whole endpoint as string which should
        /// look like <a href="https://your-resource-endpoint.cognitiveservices.azure.com/"/>. 
        /// </summary>
        /// <param name="imageStream"></param>
        /// <param name="endpoint"></param>
        /// <param name="key"></param>
        public AzureFeatureExtractor(Stream imageStream, string endpoint, string key)
        {
            var br = new BinaryReader(imageStream);
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
            _content = new ByteArrayContent(br.ReadBytes((int)imageStream.Length));
            _content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            
            _endpoint = endpoint;
        }

        public IEnumerable<Rectangle> ExtractFaces()
        {
            var responseMessage = _client.PostAsync(string.Format(BaseUrl, _endpoint, FaceDetection), _content).Result;
            var stream = responseMessage.Content.ReadAsStreamAsync().Result;
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, ObjectSettings)?.Faces.Select(face => face.FaceRectangle.AsRectangle());
        }

        public IEnumerable<Rectangle> ExtractCars()
        {
            var responseMessage = _client.PostAsync(string.Format(BaseUrl, _endpoint, ObjectDetection), _content).Result;
            var stream = responseMessage.Content.ReadAsStreamAsync().Result;
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, ObjectSettings)?.Objects
                .Where(obj => obj.Object.Equals("car") || obj.Object.Equals("truck"))
                .Select(car => car.Rectangle.AsRectangle());
        }

        public IEnumerable<Rectangle> ExtractText()
        { 
            var responseMessage = _client.PostAsync(string.Format(BaseUrl, _endpoint, TextDetection), _content).Result;
            var stream = responseMessage.Content.ReadAsStreamAsync().Result;
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<OcrResponse>(response, OcrSettings)?.Regions
                .Select(region => region.BoundingBox.AsRectangle());
        }

        public IEnumerable<Rectangle> ExtractPersons()
        {
            var responseMessage = _client.PostAsync(string.Format(BaseUrl, _endpoint, ObjectDetection), _content).Result;
            var stream = responseMessage.Content.ReadAsStreamAsync().Result;
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, ObjectSettings)?.Objects
                .Where(obj => obj.Object.Equals("person"))
                .Select(person => person.Rectangle.AsRectangle());
        }

        public async Task<IEnumerable<Rectangle>> ExtractFacesAsync()
        {
            var responseMessage = await _client.PostAsync(string.Format(BaseUrl, _endpoint, FaceDetection), _content);
            var stream = await responseMessage.Content.ReadAsStreamAsync();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, ObjectSettings)?.Faces.Select(face => face.FaceRectangle.AsRectangle());
        }

        public async Task<IEnumerable<Rectangle>> ExtractCarsAsync()
        {
            var responseMessage = await _client.PostAsync(string.Format(BaseUrl, _endpoint, ObjectDetection), _content);
            var stream = await responseMessage.Content.ReadAsStreamAsync();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, ObjectSettings)?.Objects
                .Where(obj => obj.Object.Equals("car") || obj.Object.Equals("truck"))
                .Select(car => car.Rectangle.AsRectangle());
        }

        public async Task<IEnumerable<Rectangle>> ExtractTextAsync()
        {
            var responseMessage = await _client.PostAsync(string.Format(BaseUrl, _endpoint, TextDetection), _content);
            var stream = await responseMessage.Content.ReadAsStreamAsync();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<OcrResponse>(response, OcrSettings)?.Regions
                .Select(region => region.BoundingBox.AsRectangle());
        }

        public async Task<IEnumerable<Rectangle>> ExtractPersonsAsync()
        {
            var responseMessage = await _client.PostAsync(string.Format(BaseUrl, _endpoint, ObjectDetection), _content);
            var stream = await responseMessage.Content.ReadAsStreamAsync();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, ObjectSettings)?.Objects
                .Where(obj => obj.Object.Equals("person"))
                .Select(person => person.Rectangle.AsRectangle());
        }

        private class OcrBoundingBoxConverter : JsonConverter<DetectedRectangle>
        {
            public override void WriteJson(JsonWriter writer, DetectedRectangle value, JsonSerializer serializer)
            {
                writer.WriteValue($"{value.X},{value.Y},{value.W},{value.H}");
            }

            public override DetectedRectangle ReadJson(JsonReader reader, Type objectType, DetectedRectangle existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.Value == null) return new DetectedRectangle();
                var split = reader.Value.ToString().Split(",").Select(int.Parse).ToList();
                if (split.Count != 4) return new DetectedRectangle();
                var rect = new DetectedRectangle()
                {
                    X = split[0],
                    Y = split[1],
                    W = split[2],
                    H = split[3]
                };
                return rect;
            }
        }

        public class AnalysisResponse
        {
            public IEnumerable<DetectedObject> Objects = new List<DetectedObject>();
            public IEnumerable<DetectedFace> Faces = new List<DetectedFace>();
        }

        public class OcrResponse
        {
            public string Language;
            public IEnumerable<DetectedRegion> Regions;
        }

        public class DetectedObject
        {
            public string Object;
            public double Confidence;
            public DetectedObject Parent;
            public DetectedRectangle Rectangle;

        }

        public class DetectedFace
        {
            public int Age;
            public string Gender;
            public DetectedFaceRectangle FaceRectangle;
        }

        public class DetectedRegion
        {
            public DetectedRectangle BoundingBox;
            public IEnumerable<DetectedLine> Lines;
        }

        public class DetectedLine
        {
            public DetectedRectangle BoundingBox;
            public IEnumerable<DetectedWord> Words;
        }

        public class DetectedWord
        {
            public DetectedRectangle BoundingBox;
            public string Text;
        }
        
        public class DetectedRectangle
        {
            public int X;
            public int Y;
            public int W;
            public int H;

            public Rectangle AsRectangle()
            {
                return new Rectangle(X, Y, W, H);
            }
        }
        public class DetectedFaceRectangle
        {
            public int Left;
            public int Top;
            public int Width;
            public int Height;

            public Rectangle AsRectangle()
            {
                return new Rectangle(Left, Top, Width, Height);
            }
        }
    }
}