using System.IO;
using Google.Cloud.Vision.V1;

namespace CloudImagePixelizer.gcp
{
    public class GcpConnector : IConnector
    {
        private readonly ImageAnnotatorClient _client;

        /// <summary>
        /// Constructor for a cloud connector using GCP.
        /// </summary>
        /// <param name="credentialsPath"></param>
        public GcpConnector(string credentialsPath)
        {
            _client = new ImageAnnotatorClientBuilder()
            {
                CredentialsPath = credentialsPath
            }.Build();
        }
        
        public IFeatureExtractor AnalyseImage(string imagePath)
        {
            return new GcpFeatureExtractor(imagePath, _client);
        }
        
        public IFeatureExtractor AnalyseImage(Stream imageStream)
        {
            return new GcpFeatureExtractor(imageStream, _client);
        }
    }
}