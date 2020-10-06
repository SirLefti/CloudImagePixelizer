using System.IO;
using Amazon;
using Amazon.Rekognition;
using Amazon.Runtime;

namespace CloudImagePixelizer.aws
{
	public class AwsConnector : IConnector
	{
		private readonly AmazonRekognitionClient _client;

		/// <summary>
		/// Constructor for a cloud connector using AWS.
		/// </summary>
		/// <param name="accessKey"></param>
		/// <param name="secretKey"></param>
		/// <param name="endpoint"></param>
		public AwsConnector(string accessKey, string secretKey, RegionEndpoint endpoint)
		{
			_client = new AmazonRekognitionClient(new BasicAWSCredentials(accessKey, secretKey), endpoint);
		}
		
		public IFeatureExtractor AnalyseImage(string imagePath)
		{
			return new AwsFeatureExtractor(imagePath, _client);
		}
		
		public IFeatureExtractor AnalyseImage(Stream imageStream)
		{
			return new AwsFeatureExtractor(imageStream, _client);
		}
	}
}