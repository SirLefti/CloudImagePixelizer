using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CloudImagePixelizer.azure
{
    public class AzureFeatureExtractor : IFeatureExtractor
    {
        private Stream _imageStream;
        private const string BaseUrl = "http://{0}.cognitiveservices.azure.com/vision/v3.0/{1}";
        private const string Ocr = "ocr";
        private const string ObjectDetection = "detect";
        private const string FaceDetection = "analyse?visualFeatures=Faces";

        private readonly string _endpoint;
        private readonly string _key;

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new List<JsonConverter>{new OcrBoundingBoxConverter()}
        };

        public AzureFeatureExtractor(string imagePath, string endpoint, string key)
        {
            _imageStream = new MemoryStream();
            Image image = Image.FromFile(imagePath);
            image.Save(_imageStream, image.RawFormat);
            _endpoint = endpoint;
            _key = key;
        }

        public AzureFeatureExtractor(Stream imageStream, string endpoint, string key)
        {
            _imageStream = imageStream;
            _endpoint = endpoint;
            _key = key;
        }

        public IEnumerable<Rectangle> ExtractFaces()
        {
            var http = WebRequest.Create(string.Format(BaseUrl, _endpoint, FaceDetection));
            http.Method = WebRequestMethods.Http.Post;
            http.ContentType = "application/octet-stream";
            http.Headers.Add("Opc-Apim-Subscription-Key", _key);
            _imageStream.CopyTo(http.GetRequestStream());

            var stream = http.GetResponse().GetResponseStream();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, Settings)?.Faces.Select(face => face.FaceRectangle.AsRectangle());
        }

        public IEnumerable<Rectangle> ExtractCars()
        {
            var http = WebRequest.Create(string.Format(BaseUrl, _endpoint, ObjectDetection));
            http.Method = WebRequestMethods.Http.Post;
            http.ContentType = "application/octet-stream";
            http.Headers.Add("Opc-Apim-Subscription-Key", _key);
            _imageStream.CopyTo(http.GetRequestStream());

            var stream = http.GetResponse().GetResponseStream();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, Settings)?.Objects
                .Where(obj => obj.Object.Equals("car") || obj.Object.Equals("truck"))
                .Select(car => car.Rectangle.AsRectangle());
        }

        public IEnumerable<Rectangle> ExtractText()
        {
            var http = WebRequest.Create(string.Format(BaseUrl, _endpoint, Ocr));
            http.Method = WebRequestMethods.Http.Post;
            http.ContentType = "application/octet-stream";
            http.Headers.Add("Opc-Apim-Subscription-Key", _key);
            _imageStream.CopyTo(http.GetRequestStream());

            var stream = http.GetResponse().GetResponseStream();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<OcrResponse>(response, Settings)?.Regions
                .Select(region => region.BoundingBox.AsRectangle());
        }

        public IEnumerable<Rectangle> ExtractPersons()
        {
            var http = WebRequest.Create(string.Format(BaseUrl, _endpoint, ObjectDetection));
            http.Method = WebRequestMethods.Http.Post;
            http.ContentType = "application/octet-stream";
            http.Headers.Add("Opc-Apim-Subscription-Key", _key);
            _imageStream.CopyTo(http.GetRequestStream());

            var stream = http.GetResponse().GetResponseStream();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, Settings)?.Objects
                .Where(obj => obj.Object.Equals("person"))
                .Select(person => person.Rectangle.AsRectangle());
        }

        public async Task<IEnumerable<Rectangle>> ExtractFacesAsync()
        {
            var http = WebRequest.Create(string.Format(BaseUrl, _endpoint, FaceDetection));
            http.Method = WebRequestMethods.Http.Post;
            http.ContentType = "application/octet-stream";
            http.Headers.Add("Opc-Apim-Subscription-Key", _key);
            await _imageStream.CopyToAsync(await http.GetRequestStreamAsync());

            var stream = (await http.GetResponseAsync()).GetResponseStream();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, Settings)?.Faces.Select(face => face.FaceRectangle.AsRectangle());
        }

        public async Task<IEnumerable<Rectangle>> ExtractCarsAsync()
        {
            var http = WebRequest.Create(string.Format(BaseUrl, _endpoint, ObjectDetection));
            http.Method = WebRequestMethods.Http.Post;
            http.ContentType = "application/octet-stream";
            http.Headers.Add("Opc-Apim-Subscription-Key", _key);
            await _imageStream.CopyToAsync(await http.GetRequestStreamAsync());

            var stream = (await http.GetResponseAsync()).GetResponseStream();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, Settings)?.Objects
                .Where(obj => obj.Object.Equals("car") || obj.Object.Equals("truck"))
                .Select(car => car.Rectangle.AsRectangle());
        }

        public async Task<IEnumerable<Rectangle>> ExtractTextAsync()
        {
            var http = WebRequest.Create(string.Format(BaseUrl, _endpoint, Ocr));
            http.Method = WebRequestMethods.Http.Post;
            http.ContentType = "application/octet-stream";
            http.Headers.Add("Opc-Apim-Subscription-Key", _key);
            await _imageStream.CopyToAsync(await http.GetRequestStreamAsync());

            var stream = (await http.GetResponseAsync()).GetResponseStream();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<OcrResponse>(response, Settings)?.Regions
                .Select(region => region.BoundingBox.AsRectangle());
        }

        public async Task<IEnumerable<Rectangle>> ExtractPersonsAsync()
        {
            var http = WebRequest.Create(string.Format(BaseUrl, _endpoint, ObjectDetection));
            http.Method = WebRequestMethods.Http.Post;
            http.ContentType = "application/octet-stream";
            http.Headers.Add("Opc-Apim-Subscription-Key", _key);
            await _imageStream.CopyToAsync(await http.GetRequestStreamAsync());

            var stream = (await http.GetResponseAsync()).GetResponseStream();
            if (stream == null) return null;
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var response = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<AnalysisResponse>(response, Settings)?.Objects
                .Where(obj => obj.Object.Equals("person"))
                .Select(person => person.Rectangle.AsRectangle());
        }

        private class OcrBoundingBoxConverter : JsonConverter<Rectangle>
        {
            public override void WriteJson(JsonWriter writer, Rectangle value, JsonSerializer serializer)
            {
                writer.WriteValue($"{value.X},{value.Y},{value.Width},{value.Height}");
            }

            public override Rectangle ReadJson(JsonReader reader, Type objectType, Rectangle existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.Value == null) return new Rectangle();
                var split = reader.Value.ToString().Split(",").Select(int.Parse).ToList();
                if (split.Count != 4) return new Rectangle();
                var rect = new Rectangle()
                {
                    X = split[0],
                    Y = split[1],
                    Width = split[2],
                    Height = split[3]
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