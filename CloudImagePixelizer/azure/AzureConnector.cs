using System.IO;

namespace CloudImagePixelizer.azure
{
    public class AzureConnector : IConnector
    {

        private readonly string _endpoint;
        private readonly string _key;

        /// <summary>
        /// Constructor for a cloud connector using Microsoft Azure.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="key"></param>
        public AzureConnector(string endpoint, string key)
        {
            _endpoint = endpoint;
            _key = key;
        }
        
        public IFeatureExtractor AnalyseImage(string imagePath)
        {
            return new AzureFeatureExtractor(imagePath, _endpoint, _key);
        }
        
        public IFeatureExtractor AnalyseImage(Stream imageStream)
        {
            return new AzureFeatureExtractor(imageStream, _endpoint, _key);
        }

    }
}