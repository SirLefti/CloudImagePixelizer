using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Vision.V1;
using Google.Protobuf.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Image = System.Drawing.Image;

namespace CloudImagePixelizer.gcp
{
    public class GcpReworkedApiFeatureExtractor : GcpReworkedFeatureExtractor
    {
        private const string AnnotateEndpoint = "https://vision.googleapis.com/v1/images:annotate";

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> {new LandmarkEnumConverter(), new LikelihoodEnumConverter()}
        };

        /// <summary>
        /// Constructor for a feature extractor using Google Cloud Platform by performing a raw HTTP-POST directly to
        /// the API endpoint, optimized for analysing a single image.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="apiKey"></param>
        public GcpReworkedApiFeatureExtractor(string imagePath, string apiKey)
        {
            var size = Image.FromFile(imagePath).Size;
            Width = size.Width;
            Height = size.Height;

            var http = WebRequest.Create(AnnotateEndpoint + "?key=" + apiKey);
            http.Method = "POST";
            http.ContentType = "application/json";
            _http = http;
            _image = new Base64Image(imagePath);
        }

        /// <summary>
        /// Constructor for a feature extractor using Google Cloud Platform by performing a raw HTTP-POST directly to
        /// the API endpoint, optimized for analysing a single image.
        /// </summary>
        /// <param name="imageStream"></param>
        /// <param name="apiKey"></param>
        public GcpReworkedApiFeatureExtractor(Stream imageStream, string apiKey)
        {
            var size = Image.FromStream(imageStream).Size;
            Width = size.Width;
            Height = size.Height;

            var http = WebRequest.Create(AnnotateEndpoint + "?key=" + apiKey);
            http.Method = "POST";
            http.ContentType = "application/json";
            _http = http;
            _image = new Base64Image(imageStream);
        }

        /// <summary>
        /// Constructor for a feature extractor using Google Cloud Platform by performing a raw HTTP-POST directly to
        /// the API endpoint, optimized to be used with a batch of images.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="http"></param>
        internal GcpReworkedApiFeatureExtractor(string imagePath, WebRequest http)
        {
            var size = Image.FromFile(imagePath).Size;
            Width = size.Width;
            Height = size.Height;
            _http = http;
            _image = new Base64Image(imagePath);
        }

        /// <summary>
        /// Constructor for a feature extractor using Google Cloud Platform by performing a raw HTTP-POST directly to
        /// the API endpoint, optimized to be used with a batch of images.
        /// </summary>
        /// <param name="imageStream"></param>
        /// <param name="http"></param>
        internal GcpReworkedApiFeatureExtractor(Stream imageStream, WebRequest http)
        {
            var size = Image.FromStream(imageStream).Size;
            Width = size.Width;
            Height = size.Height;
            _http = http;
            _image = new Base64Image(imageStream);
        }

        private readonly WebRequest _http;
        private readonly Base64Image _image;

        private AnnotateImageResponse Fetch(InnerAnnotateImageRequest request)
        {
            var json = JsonConvert.SerializeObject(new RequestContainer(request), Settings);
            var bytes = Encoding.UTF8.GetBytes(json);

            _http.GetRequestStream().Write(bytes, 0, bytes.Length);

            var stream = _http.GetResponse().GetResponseStream();
            if (stream == null) return null;

            using var reader = new StreamReader(stream, Encoding.UTF8);
            // convert types due to different internal handling
            var response = reader.ReadToEnd().Replace("HYPHEN", "Hyphen")
                .Replace("SPACE", "Space")
                .Replace("UNKNOWN", "Unknown")
                .Replace("LINE_BREAK", "LineBreak")
                .Replace("SURE_SPACE", "SureSpace")
                .Replace("EOL_SURE_SPACE", "EolSureSpace");
            return JsonConvert.DeserializeObject<ResponseContainer>(response, Settings)?.Responses[0];
        }

        private async Task<AnnotateImageResponse> FetchAsync(InnerAnnotateImageRequest request)
        {
            var json = JsonConvert.SerializeObject(new RequestContainer(request), Settings);
            var bytes = Encoding.UTF8.GetBytes(json);

            await (await _http.GetRequestStreamAsync()).WriteAsync(bytes, 0, bytes.Length);

            var stream = (await _http.GetResponseAsync()).GetResponseStream();
            if (stream == null) return null;

            using var reader = new StreamReader(stream, Encoding.UTF8);
            // convert types due to different internal handling
            var response = (await reader.ReadToEndAsync()).Replace("HYPHEN", "Hyphen")
                .Replace("SPACE", "Space")
                .Replace("UNKNOWN", "Unknown")
                .Replace("LINE_BREAK", "LineBreak")
                .Replace("SURE_SPACE", "SureSpace")
                .Replace("EOL_SURE_SPACE", "EolSureSpace");

            return JsonConvert.DeserializeObject<ResponseContainer>(response, Settings)?.Responses[0];
        }

        public new IEnumerable<Rectangle> ExtractFaces()
        {
            FacesResponse ??= Fetch(new InnerAnnotateImageRequest
            {
                Features = {new Feature("FACE_DETECTION")},
                Image = _image
            });

            return base.ExtractFaces();
        }

        public new async Task<IEnumerable<Rectangle>> ExtractFacesAsync()
        {
            FacesResponse ??= await FetchAsync(new InnerAnnotateImageRequest
            {
                Features = {new Feature("FACE_DETECTION")},
                Image = _image
            });

            return await base.ExtractFacesAsync();
        }

        public new IEnumerable<Rectangle> ExtractCars()
        {
            ObjectsResponse ??= Fetch(new InnerAnnotateImageRequest
            {
                
                Features = {new Feature("OBJECT_LOCALIZATION")},
                Image = _image
            });

            return base.ExtractCars();
        }

        public new async Task<IEnumerable<Rectangle>> ExtractCarsAsync()
        {
            ObjectsResponse ??= await FetchAsync(new InnerAnnotateImageRequest
            {
                Features = {new Feature("OBJECT_LOCALIZATION")},
                Image = _image
            });

            return await base.ExtractCarsAsync();
        }

        public new IEnumerable<Rectangle> ExtractText()
        {
            TextResponse ??= Fetch(new InnerAnnotateImageRequest
            {
                Features = {new Feature("TEXT_DETECTION")},
                Image = _image
            });

            return base.ExtractText();
        }

        public new async Task<IEnumerable<Rectangle>> ExtractTextAsync()
        {
            TextResponse ??= await FetchAsync(new InnerAnnotateImageRequest
            {
                Features = {new Feature("TEXT_DETECTION")},
                Image = _image
            });

            return await base.ExtractTextAsync();
        }

        public new IEnumerable<Rectangle> ExtractPersons()
        {
            ObjectsResponse ??= Fetch(new InnerAnnotateImageRequest
            {
                Features = {new Feature("OBJECT_LOCALIZATION")},
                Image = _image
            });

            return base.ExtractPersons();
        }

        public new async Task<IEnumerable<Rectangle>> ExtractPersonsAsync()
        {
            ObjectsResponse ??= await FetchAsync(new InnerAnnotateImageRequest
            {
                Features = {new Feature("OBJECT_LOCALIZATION")},
                Image = _image
            });

            return await base.ExtractPersonsAsync();
        }

        private class LikelihoodEnumConverter : JsonConverter<Likelihood>
        {
            public override void WriteJson(JsonWriter writer, Likelihood value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }

            public override Likelihood ReadJson(JsonReader reader, Type objectType, Likelihood existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.Value == null) return Likelihood.Unknown;
                var converted = reader.Value.ToString().ToLower().Split("_").Select(FirstToUpper)
                    .Aggregate((a, b) => a + b);
                try
                {
                    return Enum.Parse<Likelihood>(converted);
                }
                catch (ArgumentException e)
                {
                    return Likelihood.Unknown;
                }
            }

            private static string FirstToUpper(string input)
            {
                var firstLetter = input.ToCharArray().First().ToString().ToUpper();
                return string.IsNullOrEmpty(input) ? input : firstLetter + string.Join("", input.ToCharArray().Skip(1));
            }
        }

        private class LandmarkEnumConverter : JsonConverter<FaceAnnotation.Types.Landmark.Types.Type>
        {
            public override void WriteJson(JsonWriter writer, FaceAnnotation.Types.Landmark.Types.Type value,
                JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }

            public override FaceAnnotation.Types.Landmark.Types.Type ReadJson(JsonReader reader, Type objectType,
                FaceAnnotation.Types.Landmark.Types.Type existingValue, bool hasExistingValue,
                JsonSerializer serializer)
            {
                if (reader.Value == null) return FaceAnnotation.Types.Landmark.Types.Type.UnknownLandmark;
                var converted = reader.Value.ToString().ToLower().Split("_").Select(FirstToUpper)
                    .Aggregate((a, b) => a + b);
                try
                {
                    return Enum.Parse<FaceAnnotation.Types.Landmark.Types.Type>(converted);
                }
                catch (ArgumentException e)
                {
                    return FaceAnnotation.Types.Landmark.Types.Type.UnknownLandmark;
                }
            }

            private static string FirstToUpper(string input)
            {
                var firstLetter = input.ToCharArray().First().ToString().ToUpper();
                return string.IsNullOrEmpty(input) ? input : firstLetter + string.Join("", input.ToCharArray().Skip(1));
            }
        }

        private class ResponseContainer
        {
            public RepeatedField<AnnotateImageResponse> Responses;
        }

        private class RequestContainer
        {
            public RequestContainer()
            {
                Requests = new RepeatedField<InnerAnnotateImageRequest>();
            }

            public RequestContainer(params InnerAnnotateImageRequest[] requests)
            {
                Requests = new RepeatedField<InnerAnnotateImageRequest> {requests};
            }

            public RepeatedField<InnerAnnotateImageRequest> Requests;
        }

        private class InnerAnnotateImageRequest
        {
            public RepeatedField<Feature> Features = new RepeatedField<Feature>();

            public Base64Image Image;
        }

        private class Base64Image
        {
            public Base64Image(string imagePath)
            {
                Content = Convert.ToBase64String(File.ReadAllBytes(imagePath));
            }

            public Base64Image(Stream imageStream)
            {
                var memoryStream = new MemoryStream();
                imageStream.CopyTo(memoryStream);
                Content = Convert.ToBase64String(memoryStream.ToArray());
            }

            public readonly string Content;
        }

        private class Feature
        {
            public Feature(string type)
            {
                Type = type;
            }

            public Feature(string type, int maxResults)
            {
                Type = type;
                MaxResults = maxResults;
            }

            public int MaxResults = 50;
            public string Type;
        }
    }
}