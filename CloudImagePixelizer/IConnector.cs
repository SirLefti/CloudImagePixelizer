using System.IO;

namespace CloudImagePixelizer
{
	/// <summary>
	/// 
	/// </summary>
	public interface IConnector
	{
		/// <summary>
		/// Array of supported file extensions, will be used by <see cref="CloudImagePixelizerClient"/> when
		/// batch-processing a directory of images.
		/// </summary>
		string[] SupportedFileExtensions { get; }
		
		/// <summary>
		/// Returns a feature extractor. The actual API call will happen when accessing the features the first time.
		/// </summary>
		/// <param name="imagePath"></param>
		/// <returns></returns>
		public IFeatureExtractor AnalyseImage(string imagePath);
		
		/// <summary>
		/// Returns a feature extractor. The actual API call will happen when accessing the features the first time.
		/// </summary>
		/// <param name="imageStream"></param>
		/// <returns></returns>
		public IFeatureExtractor AnalyseImage(Stream imageStream);
	}
}