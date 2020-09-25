using System.IO;
using Google.Cloud.Vision.V1;

namespace CloudImagePixelizer.gcp
{
    public class GcpConnector : IConnector
    {
        private readonly ImageAnnotatorClient _client;

        public GcpConnector(string credentialsPath)
        {
            _client = new ImageAnnotatorClientBuilder()
            {
                CredentialsPath = credentialsPath
            }.Build();
        }

        /// <summary>
        /// Returns a feature extractor. The actual API call will happen when accessing the features the first time.
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public IFeatureExtractor AnalyseImage(string imagePath)
        {
            return new GcpFeatureExtractor(imagePath, _client);
        }

        /// <summary>
        /// Returns a feature extractor. The actual API call will happen when accessing the features the first time.
        /// </summary>
        /// <param name="imageStream"></param>
        /// <returns></returns>
        public IFeatureExtractor AnalyseImage(Stream imageStream)
        {
            return new GcpFeatureExtractor(imageStream, _client);
        }
    }
}