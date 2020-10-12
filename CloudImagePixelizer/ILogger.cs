using System.Collections.Generic;
using System.Drawing;

namespace CloudImagePixelizer
{
    public interface ILogger
    {
        public void OnExtractedFaces(string fileName, IEnumerable<Rectangle> faces);
        public void OnExtractedCars(string fileName, IEnumerable<Rectangle> cars);
        public void OnExtractedText(string fileName, IEnumerable<Rectangle> text);
        public void OnExtractedPersons(string fileName, IEnumerable<Rectangle> persons);
        public void OnExtractedLicensePlates(string fileName, IEnumerable<Rectangle> licensePlates);
        public void OnPixelatedFace(string fileName, Rectangle face);
        public void OnPixelatedCar(string fileName, Rectangle car);
        public void OnPixelatedText(string fileName, Rectangle text);
        public void OnPixelatedPerson(string fileName, Rectangle person);
        public void OnPixelatedLicensePlate(string fileName, Rectangle licensePlate);
    }
}