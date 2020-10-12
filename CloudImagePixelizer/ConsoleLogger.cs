using System;
using System.Collections.Generic;
using System.Drawing;

namespace CloudImagePixelizer
{
    public class ConsoleLogger : ILogger
    {
        public void OnExtractedFaces(string fileName, IEnumerable<Rectangle> faces)
        {
            Console.WriteLine("faces in {0}: {1}", fileName, string.Join(", ", faces));
        }

        public void OnExtractedCars(string fileName, IEnumerable<Rectangle> cars)
        {
            Console.WriteLine("cars in {0}: {1}", fileName, string.Join(", ", cars));
        }

        public void OnExtractedText(string fileName, IEnumerable<Rectangle> text)
        {
            Console.WriteLine("text in {0}: {1}", fileName, string.Join(", ", text));
        }

        public void OnExtractedPersons(string fileName, IEnumerable<Rectangle> persons)
        {
            Console.WriteLine("persons in {0}: {1}", fileName, string.Join(", ", persons));
        }

        public void OnExtractedLicensePlates(string fileName, IEnumerable<Rectangle> licensePlates)
        {
            Console.WriteLine("license plates in {0}: {1}", fileName, string.Join(", ", licensePlates));
        }

        public void OnPixelatedFace(string fileName, Rectangle face)
        {
            Console.WriteLine("pixelated face in {0} at {1}", fileName, face);
        }

        public void OnPixelatedCar(string fileName, Rectangle car)
        {
            Console.WriteLine("pixelated car in {0} at {1}", fileName, car);
        }

        public void OnPixelatedText(string fileName, Rectangle text)
        {
            Console.WriteLine("pixelated text in {0} at {1}", fileName, text);
        }

        public void OnPixelatedPerson(string fileName, Rectangle person)
        {
            Console.WriteLine("pixelated person in {0} at {1}", fileName, person);
        }

        public void OnPixelatedLicensePlate(string fileName, Rectangle licensePlate)
        {
            Console.WriteLine("pixelated license plate in {0} at {1}", fileName, licensePlate);
        }
    }
}