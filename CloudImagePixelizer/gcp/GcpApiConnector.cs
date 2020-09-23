using System.Drawing;
using System.IO;
using System.Net;
using CloudImagePixelizer;

namespace CloudImagePixelizer.gcp
{
	public class GcpApiConnector : IConnector
	{
		private const string AnnotateEndpoint = "https://vision.googleapis.com/v1/images:annotate";
		private readonly WebRequest _http;
		
		/// <summary>
		/// Constructor for an API connector.
		/// </summary>
		/// <param name="apiKey"></param>
		public GcpApiConnector(string apiKey)
		{
			var http = WebRequest.Create(AnnotateEndpoint + "?key=" + apiKey);
			http.Method = "POST";
			http.ContentType = "application/json";
			_http = http;
		}
		
		/// <summary>
		/// Returns a feature extractor. The actual API call will happen when accessing the features the first time.
		/// </summary>
		/// <param name="imagePath"></param>
		/// <returns></returns>
		public IFeatureExtractor AnalyseImage(string imagePath)
		{
			return new GcpApiFeatureExtractor(imagePath, _http);
		}

		/// <summary>
		/// Returns a feature extractor. The actual API call will happen when accessing the features the first time.
		/// </summary>
		/// <param name="imageStream"></param>
		/// <returns></returns>
		public IFeatureExtractor AnalyseImage(Stream imageStream)
		{
			return new GcpApiFeatureExtractor(imageStream, _http);
		}
	}
}