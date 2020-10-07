using System.IO;

namespace CloudImagePixelizer.gcp
{
    public class GcpApiConnector : IConnector
    {
        private readonly string _apiKey;

        /// <summary>
        /// Constructor for a cloud connector using GCP.
        /// </summary>
        /// <param name="apiKey"></param>
        public GcpApiConnector(string apiKey)
        {
            _apiKey = apiKey;
        }

        public string[] SupportedFileExtensions { get; } = {".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".raw", ".ico", ".pdf", ".tiff"};

        public IFeatureExtractor AnalyseImage(string imagePath)
        {
            return new GcpApiFeatureExtractor(imagePath, _apiKey);
        }
        
        public IFeatureExtractor AnalyseImage(Stream imageStream)
        {
            return new GcpApiFeatureExtractor(imageStream, _apiKey);
        }
    }
}