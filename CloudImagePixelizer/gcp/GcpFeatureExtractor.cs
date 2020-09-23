using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Google.Cloud.Vision.V1;
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
			var size = System.Drawing.Image.FromFile(imagePath).Size;
			_width = size.Width;
			_height = size.Height;
			var visionImage = Image.FromFile(imagePath);
			_client = new ImageAnnotatorClientBuilder()
			{
				CredentialsPath = credentialsPath
			}.Build();
			_request = new AnnotateImageRequest()
			{
				Image = visionImage,
				Features =
				{
					new Feature {Type = Feature.Types.Type.ObjectLocalization},
					new Feature {Type = Feature.Types.Type.FaceDetection},
					new Feature {Type = Feature.Types.Type.TextDetection}
				}
			};
		}
		
		/// <summary>
		/// Constructor for a feature extractor using Google Cloud Platform SDK, optimized for analysing a single image.
		/// </summary>
		/// <param name="imageStream"></param>
		/// <param name="credentialsPath"></param>
		public GcpFeatureExtractor(Stream imageStream, string credentialsPath)
		{
			var size = System.Drawing.Image.FromStream(imageStream).Size;
			_width = size.Width;
			_height = size.Height;
			var visionImage = Image.FromStream(imageStream);
			_client = new ImageAnnotatorClientBuilder()
			{
				CredentialsPath = credentialsPath
			}.Build();
			_request = new AnnotateImageRequest()
			{
				Image = visionImage,
				Features =
				{
					new Feature {Type = Feature.Types.Type.ObjectLocalization},
					new Feature {Type = Feature.Types.Type.FaceDetection},
					new Feature {Type = Feature.Types.Type.TextDetection}
				}
			};
		}

		/// <summary>
		/// Constructor for a feature extractor using Google Cloud Platform SDK, using a given annotator client,
		/// optimized to be used with a batch of images.
		/// </summary>
		/// <param name="imagePath"></param>
		/// <param name="client"></param>
		internal GcpFeatureExtractor(string imagePath, ImageAnnotatorClient client)
		{
			var size = System.Drawing.Image.FromFile(imagePath).Size;
			_width = size.Width;
			_height = size.Height;
			_client = client;
			_request = new AnnotateImageRequest()
			{
				Image = Image.FromFile(imagePath),
				Features =
				{
					new Feature {Type = Feature.Types.Type.ObjectLocalization},
					new Feature {Type = Feature.Types.Type.FaceDetection},
					new Feature {Type = Feature.Types.Type.TextDetection}
				}
			};
		}
		
		/// <summary>
		/// Constructor for a feature extractor using Google Cloud Platform SDK, using a given annotator client,
		/// optimized to be used with a batch of images.
		/// </summary>
		/// <param name="imageStream"></param>
		/// <param name="client"></param>
		internal GcpFeatureExtractor(Stream imageStream, ImageAnnotatorClient client)
		{
			var size = System.Drawing.Image.FromStream(imageStream).Size;
			_width = size.Width;
			_height = size.Height;
			_client = client;
			_request = new AnnotateImageRequest()
			{
				Image = Image.FromStream(imageStream),
				Features =
				{
					new Feature {Type = Feature.Types.Type.ObjectLocalization},
					new Feature {Type = Feature.Types.Type.FaceDetection},
					new Feature {Type = Feature.Types.Type.TextDetection}
				}
			};
		}

		/// <summary>
		/// Protected constructor just for the purpose of allowing an inherited alternative extractor.
		/// </summary>
		protected GcpFeatureExtractor()
		{
		}
		
		private readonly int _width;
		private readonly int _height;

		private readonly ImageAnnotatorClient _client;
		private readonly AnnotateImageRequest _request;
		private AnnotateImageResponse _response;

		public async Task<IEnumerable<Rectangle>> AsyncExtractFaces()
		{
			_response ??= await AsyncFetch();
			return ExtractFaces();
		}
		
		public IEnumerable<Rectangle> ExtractFaces()
		{
			_response ??= Fetch();

			return _response.FaceAnnotations
				.Select(f => GoogleVisionCoordinateTranslator.AbsolutePolyToRectangle(f.BoundingPoly));
		}
		
		public async Task<IEnumerable<Rectangle>> AsyncExtractCars()
		{
			_response ??= await AsyncFetch();
			return ExtractCars();
		}

		public IEnumerable<Rectangle> ExtractCars()
		{
			_response ??= Fetch();
			
			return _response.LocalizedObjectAnnotations.Where(e => e.Name == "Car")
				.Select(e => GoogleVisionCoordinateTranslator.RelativePolyToRectangle(e.BoundingPoly, _width, _height));
		}

		public async Task<IEnumerable<Rectangle>> AsyncExtractText()
		{
			_response ??= await AsyncFetch();
			return ExtractText();
		}

		public IEnumerable<Rectangle> ExtractText()
		{
			_response ??= Fetch();

			// filter text annotations that contains line breaks, this is probably the first one covering the whole image
			return _response.TextAnnotations.Where(t => !t.Description.Contains("\n"))
				.Select(t => GoogleVisionCoordinateTranslator.AbsolutePolyToRectangle(t.BoundingPoly));
		}

		public async Task<IEnumerable<Rectangle>> AsyncExtractPersons()
		{
			_response ??= await AsyncFetch();
			return ExtractPersons();
		}
		public IEnumerable<Rectangle> ExtractPersons()
		{
			_response ??= Fetch();

			return _response.LocalizedObjectAnnotations.Where(e => e.Name == " Person")
				.Select(e => GoogleVisionCoordinateTranslator.RelativePolyToRectangle(e.BoundingPoly, _width, _height));
		}

		protected virtual async Task<AnnotateImageResponse> AsyncFetch()
		{
			return await _client.AnnotateAsync(_request);
		}

		protected virtual AnnotateImageResponse Fetch()
		{
			return _client.Annotate(_request);
		}
	}
}