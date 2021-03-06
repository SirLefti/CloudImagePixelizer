# CloudImagePixelizer
A C# interface to the most common cloud services to pixelate faces and license plates with a minimum amount of code.

## Introduction
This project provides an interface to the image recognition systems of Microsoft Azure, Amazon Web Services and the Google Cloud Platform. The analysis data will be prepared to pixelize your images with no extra code needed.

This project was developed as part of a Bachelor thesis at the [GOD mbH](https://www.god.de/), a German software development company. It is available at NuGet.

## How to use
First of all, you need authorized access to at least one of the cloud services mentioned above. If you do not have one, you can create one, they all provide a limited free access for evaluation. I recommend using either AWS or Google Cloud for best results. Azure works fine for faces, but not for license plates.

Depending on your chosen service, you can create a pixelizer client using the proper cloud connector. The client provides methods to pixelate either a single image or a batch of images. Although streams are accepted as input, the use of paths is recommended because rotated images may loose their rotation tag. 

When using batch processing the client filters the given input directory for files with compatible image formats (see API reference for used cloud service). Use JPEG and/or PNG to be on the safe side.

**Example:**
```C#
static async Task Main() {
	var connector = new GcpApiConnector("YOUR_API_KEY");
	var client = new CloudImagePixelizerClient(connector);
	await client.PixelateSingleImage("YOUR_INPUT_PATH", "YOUR_OUTPUT_PATH");
}
```

If you are an advanced user only needing the prepared detection data, you can use the `IConnector` and `IFeatureExtractor` classes separately.

**Examples:**

**Using IConnector:**
```C#
static async Task Main() {
	var connector = new GcpConnector(Settings.GcpCredentialsPath);
	var extractor = connector.AnalyseImage("YOUR_INPUT_PATH");
	var cars = await extractor.ExtractCarsAsync();
	foreach (var rectangle in cars)
	{
		Console.WriteLine(rectangle);
	}
}
```
**Using IFeatureExtractor:**
```C#
static async Task Main() {
	var extractor = new AzureFeatureExtractor("YOUR_INPUT_PATH", "YOUR_AZURE_ENDPOINT", "YOUR_AZURE_KEY");
	var faces = await extractor.ExtractFacesAsync();
	foreach (var rectangle in faces)
	{
		Console.WriteLine(rectangle);
	}
}
```

## Configuration
The `CloudImagePixelizerClient` contains multiple configuration options to improve the results on your images, however the defaults should work very well for the most use cases.

**Example:**
```C#
static async Task Main() {
	var connector = new GcpApiConnector("YOUR_API_KEY");
	var client = new CloudImagePixelizerClient(connector)
	{
		// Enable license plates detection using object and text detection
		CarProcessing = CarProcessing.PixelatePlatesAndTextOnCars,
		// Disable face detection
		FaceProcessing = FaceProcessing.Skip,
		// Set to distance to merge detected text blocks to 5%
		MergeFactor = 0.05,
		// Set the pixel size function to be always 32 pixels
		PixelSize = (imgWidth, imgHeight, patchWidth, patchHeight) => 32;
		// Set image output format to PNG
		OutputFormat = SKEncodedImageFormat.Png,
		// Set quality to 80
		OutputQuality = 80,
		// Set logger
		Logger = new ConsoleLogger()
		// Set an outline around all pixelated faces/persons
		FaceOutline = new SKPaint
		{
			Style = SKPaintStyle.Stroke,
			Color = SKColors.Aqua,
			StrokeWidth = 10	
		},
		// Sets an outline around all pixelated license plates/cars
		PlateOutline = new SKPaint
		{
			Style = SKPaintStyle.Stroke,
			Color = SKColors.Aqua,
			StrokeWidth = 10	
		}
	};
}
```
In case you do not need faces to be pixelated (i.e. you do not want to or you know that there are no faces) you can configure this using the `Skip` option in `FaceProcessing`, same goes vice versa for `CarProcessing`. If you are okay with a less accurate pixelation, you can try the options `PixelateCars` and `PixelatePersons`. The client will only use object detection, which reduces the costs compared to the full feature set. `PixelatePlatesAndTextOnCars` will call the object and text detection, since license plates are usually not detected separately, but cars and texts are. Using `PixelateFaces` calls the face detection. As default, the whole feature set will be used, resulting in three API calls as shown below.

| **API calls needed**            | **Skip** | **PixelatePersons** | **PixelateFaces** |
|---------------------------------|:--------:|:-------------------:|:-----------------:|
| **Skip**                        |    0     |          1          |         1         |
| **PixelateCars**                |    1     |          1          |         2         |
| **PixelatePlatesAndTextOnCars** |    2     |          2          |         3         |

Take a look into the pricing of each cloud system (may vary depending on the location) to choose the right processing modes for you.

`MergeFactor` is an option to prettify the pixelation results when using `PixelatePlatesAndTextOnCars`. The returned text blocks may not cover the whole license plate, instead there are often multiple part detections. Pixelating those individually may cause artifacts around the license plate. To avoid this, the results will be clustered and pixelated in one step for each calculated cluster. The `MergeFactor` provides the maximum distance between two textblocks or clusters to be merged together. The default is 0.025, which means 2.5% of the image width as pixels. 

`PixelSizeFunction` allows to set a custom function to calculate the perfect pixel size for an area to pixelate. The function takes four integer arguments, being in order image width, image height, areas width and areas height. Feel free to experiment to get the best result. The default implementation looks like the following.
```C#
PixelSizeFunction = (imgWidth, imgHeight, patchWidth, patchHeight) => Math.Max(patchWidth, patchHeight) / 16
```
This results in areas having 16 pixels along their longest border, so the size is determined by the areas size.

`OutputFormat` specifies the format when encoding the processed image back to your file system. The default is `Jpeg`.

`OutputQuality` specifies the encoding quality. You might want to reduce this when processing a large amount of high resolution images. The default is 100 (maximum value).

To keep track of the analysis results and processing, you can assign an `ILogger` implementing class to the `Logger` property. The default is `NullLogger` which means all logging events will be ignored. As an example implementation `ConsoleLogger` dumps all results into the console.

You can also add an `FaceOutline` and `PlateOutline` to mark the pixelated areas. The `Color` is up to you, depending on what colors are present in the image, but `Aqua`, `GreenYellow` or `OrangeRed` should work in most cases. The `StrokeWidth` should be around 10, depending on image resolution. The `style` should always be `Stroke`, the other options will also color the whole pixelated area. Try playing around with the other properties as well. The default value is null, so there will be no outline at all.

## Helpful Information
This library is in an early state, thus the error handling was not the focus yet. It was developed on MacOS using JetBrains Rider.

**Known issues:**
*   using a stream as image input will ignore the image rotation
*   when using the Azure F0 Free Tier, the Azure connector will run into the rate limit when batch processing very quickly and crash
*   image tags are not preserved

**Please note:**
*   the library neither checks the file size nor the file format before the image is passed to the cloud, do that beforehand and re-encode if necessary
*   the cloud providers may change the underlying model or the communicated result at any time

### Resources
*   [Microsoft Azure supported files](https://westcentralus.dev.cognitive.microsoft.com/docs/services/computer-vision-v3-ga/operations/56f91f2e778daf14a499f21b)
*   [Microsoft Azure pricing](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/)
*   [Amazon Web Services supported files](https://docs.aws.amazon.com/rekognition/latest/dg/limits.html)
*   [Amazon Web Services pricing](https://aws.amazon.com/rekognition/pricing/)
*   [Google Cloud Platform supported files](https://cloud.google.com/vision/docs/supported-files)
*   [Google Cloud Platform pricing](https://cloud.google.com/vision/pricing)