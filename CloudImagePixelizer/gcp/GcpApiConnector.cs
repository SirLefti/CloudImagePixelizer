using System.IO;

namespace CloudImagePixelizer.gcp
{
    public class GcpApiConnector : IConnector
    {
        private readonly string _apiKey;

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
            return new GcpApiFeatureExtractor(imagePath, _apiKey);
        }

        /// <summary>
        /// Returns a feature extractor. The actual API call will happen when accessing the features the first time.
        /// </summary>
        /// <param name="imageStream"></param>
        /// <returns></returns>
        public IFeatureExtractor AnalyseImage(Stream imageStream)
        {
            return new GcpApiFeatureExtractor(imageStream, _apiKey);
        }
    }
}