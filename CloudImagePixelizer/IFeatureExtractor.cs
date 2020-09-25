using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace CloudImagePixelizer
{
	/// <summary>
	/// Interface for different types of cloud systems for image recognition.
	/// </summary>
	public interface IFeatureExtractor
	{
		public IEnumerable<Rectangle> ExtractFaces();
		public IEnumerable<Rectangle> ExtractCars();
		public IEnumerable<Rectangle> ExtractText();
		public IEnumerable<Rectangle> ExtractPersons();
		
		public Task<IEnumerable<Rectangle>> ExtractFacesAsync();
		public Task<IEnumerable<Rectangle>> ExtractCarsAsync();
		public Task<IEnumerable<Rectangle>> ExtractTextAsync();
		public Task<IEnumerable<Rectangle>> ExtractPersonsAsync();
	}
}