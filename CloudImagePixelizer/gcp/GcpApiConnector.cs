using System.Drawing;
using System.IO;
using System.Net;
using CloudImagePixelizer;

namespace CloudImagePixelizer.gcp
{
	public class GcpApiConnector : IConnector
	{
		private const string AnnotateEndpoint = "https://vision.googleapis.com/v1/images:annotate";
		private readonly string _apiKey;
		private readonly WebRequest _http;
		
		/// <summary>
		/// Constructor for an API connector.
		/// </summary>
		/// <param name="apiKey"></param>
		public GcpApiConnector(string apiKey)
		{
			_apiKey = apiKey;
		}
		
		/// <summary>
		/// Returns a feature extractor. The actual API call will happen when accessing the features the first time.
		/// </summary>
		/// <param name="imagePath"></param>
		/// <returns></returns>
		public IFeatureExtractor AnalyseImage(string imagePath)
		{
			var http = WebRequest.Create(AnnotateEndpoint + "?key=" + _apiKey);
			http.Method = "POST";
			http.ContentType = "application/json";
			return new GcpApiFeatureExtractor(imagePath, http);
		}

		/// <summary>
		/// Returns a feature extractor. The actual API call will happen when accessing the features the first time.
		/// </summary>
		/// <param name="imageStream"></param>
		/// <returns></returns>
		public IFeatureExtractor AnalyseImage(Stream imageStream)
		{
			var http = WebRequest.Create(AnnotateEndpoint + "?key=" + _apiKey);
			http.Method = "POST";
			http.ContentType = "application/json";
			return new GcpApiFeatureExtractor(imageStream, http);
		}
	}
}