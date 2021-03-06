<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
  <Namespace>static OpenCvSharp.Mat</Namespace>
</Query>

#load ".\01_detect-mtf-score"
#load ".\02_detect-table-border"

void Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo1");

	Directory.EnumerateFiles(@".\resources", "*.png")
		.Select(x =>
		{
			using Mat src = Cv2.ImRead(x);
			using Mat mainSrc = src[GetMainArea(src)];
			using Mat l50 = MtfLines.L50.GetBinary(mainSrc);
			l50.GetRectangularArray(out byte[,] data);

			int width = data.GetLength(1);
			int height = data.GetLength(0);
			var result = Enumerable.Range(0, 10)
				.Select(x => x * width / 10)
				.Select(x => GetFirstWhite(data, x))
				.Select(x =>
				(
					y1: Math.Round(100 - 100.0 * x.y1 / height, 2),
					y2: Math.Round(100 - 100.0 * x.y2 / height, 2)
				))
				.ToArray();

			return new
			{
				Title = Path.GetFileNameWithoutExtension(x).Replace("-", " ").Replace("_", " "), 
				Center = result[0].y1, 
				MiddleFrame = result[5].y1, 
				Edge = result[9].y1
			};
		})
		.OrderByDescending(x => x.Center)
		.Dump();
}

(int y1, int y2) GetFirstWhite(byte[,] data, int x)
{
	int height = data.GetLength(0);
	int y1 = -1, y2 = -1;
	for (int y = 0; y < height; ++y)
	{
		if (data[y, x] != 0)
		{
			y1 = y;
			break;
		}
	}
	for (int y = height - 1; y >= 0; --y)
	{
		if (data[y, x] != 0)
		{
			y2 = y;
			break;
		}
	}
	return (y1, y2);
}