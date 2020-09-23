using System.IO;
using Amazon.Rekognition.Model;
using Newtonsoft.Json;

namespace CloudImagePixelizer.aws
{
	/// <summary>
	/// Feature extractor using cached json responses from Amazon Web Services. Will cause nasty stuff if files are not
	/// present, be careful.
	/// </summary>
	public class AwsCachedFeatureExtractor : AwsFeatureExtractor
	{
		public AwsCachedFeatureExtractor(string imagePath, int width, int height)
		{
			Height = height;
			Width = width;

			// Loading analysis files that are cached. This may cause exceptions if they are missing
			using (var reader = new StreamReader(imagePath + "-objects.json"))
			{
				var json = reader.ReadToEnd();
				ObjectsResponse = JsonConvert.DeserializeObject<DetectLabelsResponse>(json);
			}

			using (var reader = new StreamReader(imagePath + "-faces.json"))
			{
				var json = reader.ReadToEnd();
				FacesResponse = JsonConvert.DeserializeObject<DetectFacesResponse>(json);
			}

			using (var reader = new StreamReader(imagePath + "-text.json"))
			{
				var json = reader.ReadToEnd();
				TextResponse = JsonConvert.DeserializeObject<DetectTextResponse>(json);
			}
		}
	}
}