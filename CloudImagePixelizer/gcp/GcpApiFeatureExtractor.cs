using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Vision.V1;
using Google.Protobuf.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CloudImagePixelizer.gcp
{
	/// <summary>
	/// Feature extractor using a HTTP-POST to the Google Cloud Platform
	/// </summary>
	public class GcpApiFeatureExtractor : GcpFeatureExtractor
	{
		private const string AnnotateEndpoint = "https://vision.googleapis.com/v1/images:annotate";

		/// <summary>
		/// Constructor for a feature extractor using Google Cloud Platform by performing a raw HTTP-POST directly to
		/// the API endpoint, optimized for analysing a single image.
		/// </summary>
		/// <param name="imagePath"></param>
		/// <param name="apiKey"></param>
		public GcpApiFeatureExtractor(string imagePath, string apiKey)
		{
			var http = WebRequest.Create(AnnotateEndpoint + "?key=" + apiKey);
			http.Method = "POST";
			http.ContentType = "application/json";
			_http = http;
			_request = new InnerAnnotateImageRequest
			{
				Features =
				{
					new Feature("TEXT_DETECTION"),
					new Feature("FACE_DETECTION"),
					new Feature("OBJECT_LOCALIZATION")
				},
				Image = new Base64Image(imagePath)
			};
		}

		/// <summary>
		/// Constructor for a feature extractor using Google Cloud Platform by performing a raw HTTP-POST directly to
		/// the API endpoint, optimized for analysing a single image.
		/// </summary>
		/// <param name="imageStream"></param>
		/// <param name="apiKey"></param>
		public GcpApiFeatureExtractor(Stream imageStream, string apiKey)
		{
			var http = WebRequest.Create(AnnotateEndpoint + "?key=" + apiKey);
			http.Method = "POST";
			http.ContentType = "application/json";
			_http = http;
			_request = new InnerAnnotateImageRequest
			{
				Features =
				{
					new Feature("TEXT_DETECTION"),
					new Feature("FACE_DETECTION"),
					new Feature("OBJECT_LOCALIZATION")
				},
				Image = new Base64Image(imageStream)
			};
		}

		/// <summary>
		/// Constructor for a feature extractor using Google Cloud Platform by performing a raw HTTP-POST directly to
		/// the API endpoint, optimized to be used with a batch of images.
		/// </summary>
		/// <param name="imagePath"></param>
		/// <param name="http"></param>
		internal GcpApiFeatureExtractor(string imagePath, WebRequest http)
		{
			_http = http;
			_request = new InnerAnnotateImageRequest
			{
				Features =
				{
					new Feature("TEXT_DETECTION"),
					new Feature("FACE_DETECTION"),
					new Feature("OBJECT_LOCALIZATION")
				},
				Image = new Base64Image(imagePath)
			};
		}
		
		/// <summary>
		/// Constructor for a feature extractor using Google Cloud Platform by performing a raw HTTP-POST directly to
		/// the API endpoint, optimized to be used with a batch of images.
		/// </summary>
		/// <param name="imageStream"></param>
		/// <param name="http"></param>
		internal GcpApiFeatureExtractor(Stream imageStream, WebRequest http)
		{
			_http = http;
			_request = new InnerAnnotateImageRequest
			{
				Features =
				{
					new Feature("TEXT_DETECTION"),
					new Feature("FACE_DETECTION"),
					new Feature("OBJECT_LOCALIZATION")
				},
				Image = new Base64Image(imageStream)
			};
		}

		private readonly InnerAnnotateImageRequest _request;
		private readonly WebRequest _http;

		protected override async Task<AnnotateImageResponse> AsyncFetch()
		{
			var json = JsonConvert.SerializeObject(new RequestContainer(_request), new JsonSerializerSettings()
			{
				// Convert using camel case property names while using pascal case names
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			});
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
			return JsonConvert.DeserializeObject<ResponseContainer>(response, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			})
				?.Responses[0];
		}

		protected override AnnotateImageResponse Fetch()
		{
			var json = JsonConvert.SerializeObject(new RequestContainer(_request), new JsonSerializerSettings()
			{
				// Convert using camel case property names while using pascal case names
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			});
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
			return JsonConvert.DeserializeObject<ResponseContainer>(response, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			})
				?.Responses[0];
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