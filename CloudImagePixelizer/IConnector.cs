using System.IO;

namespace CloudImagePixelizer
{
	/// <summary>
	/// 
	/// </summary>
	public interface IConnector
	{
		public IFeatureExtractor AnalyseImage(string imagePath);
		public IFeatureExtractor AnalyseImage(Stream imageStream);
	}
}