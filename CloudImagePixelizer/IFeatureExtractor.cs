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
		/// <summary>
		/// Extracts faces from given image. If faces have never been fetched yet, this will call the cloud to analyze
		/// and extract faces. See <see cref="ExtractFacesAsync"/> for async approach.
		/// </summary>
		/// <returns>Enumerable of rectangles each representing a face</returns>
		public IEnumerable<Rectangle> ExtractFaces();
		
		/// <summary>
		/// Extracts cars from given image. If cars have never been fetched yet, this will call the cloud to analyze
		/// and extract cars. See <see cref="ExtractCarsAsync"/> for async approach.
		/// </summary>
		/// <returns>Enumerable of rectangles each representing a cars</returns>
		public IEnumerable<Rectangle> ExtractCars();
		
		/// <summary>
		/// Extracts text from given image. If text have never been fetched yet, this will call the cloud to analyze
		/// and extract text. See <see cref="ExtractTextAsync"/> for async approach.
		/// </summary>
		/// <returns>Enumerable of rectangles each representing a text block (position only)</returns>
		public IEnumerable<Rectangle> ExtractText();
		
		/// <summary>
		/// Extracts persons from given image. If persons have never been fetched yet, this will call the cloud to analyze
		/// and extract persons. See <see cref="ExtractPersonsAsync"/> for async approach.
		/// </summary>
		/// <returns>Enumerable of rectangles each representing a person</returns>
		public IEnumerable<Rectangle> ExtractPersons();
		
		/// <summary>
		/// Extracts faces from given image. If faces have never been fetched yet, this will call the cloud to analyze
		/// and extract faces. See <see cref="ExtractFaces"/> for non-async approach.
		/// </summary>
		/// <returns>Enumerable of rectangles each representing a face</returns>
		public Task<IEnumerable<Rectangle>> ExtractFacesAsync();
		
		/// <summary>
		/// Extracts cars from given image. If cars have never been fetched yet, this will call the cloud to analyze
		/// and extract cars. See <see cref="ExtractCars"/> for non-async approach.
		/// </summary>
		/// <returns>Enumerable of rectangles each representing a cars</returns>
		public Task<IEnumerable<Rectangle>> ExtractCarsAsync();
		
		/// <summary>
		/// Extracts text from given image. If text have never been fetched yet, this will call the cloud to analyze
		/// and extract text. See <see cref="ExtractText"/> for non-async approach.
		/// </summary>
		/// <returns>Enumerable of rectangles each representing a text block (position only)</returns>
		public Task<IEnumerable<Rectangle>> ExtractTextAsync();
		
		/// <summary>
		/// Extracts persons from given image. If persons have never been fetched yet, this will call the cloud to analyze
		/// and extract persons. See <see cref="ExtractPersons"/> for non-async approach.
		/// </summary>
		/// <returns>Enumerable of rectangles each representing a person</returns>
		public Task<IEnumerable<Rectangle>> ExtractPersonsAsync();
	}
}