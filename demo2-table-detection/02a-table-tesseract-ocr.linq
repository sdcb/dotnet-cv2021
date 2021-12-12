<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Tesseract</Namespace>
</Query>

#load ".\01_detect-table"
#load "..\demo3-chs-table-detection\02-tesseract-ocr-prepair"

async Task Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo2");
	await TesseractHelper.Fast.EnsureTrainedData(new[] { "chi_sim", "eng" }, QueryCancelToken);

	using var ocr = new TableOCR();
	foreach (string file in Directory.EnumerateFiles(@".\resources", "*.jpg").OrderBy(x => x).Take(1))
	{
		using Mat src = Cv2.ImRead(file);
		//var matTables = GetMatTable2(src);
		//
		//Mat[,] scaledLeft = Scale(matTables.left, 2, 2);
		//Mat[,] scaledMiddle = Scale(matTables.middle, 2, 2);
		//Mat[,] scaledRight = Scale(matTables.right, 2, 2);
		//Util.HorizontalRun(false, Image(src), ocr.Process(scaledLeft), ocr.Process(scaledMiddle), ocr.Process(scaledRight)).Dump();

		Mat[,] matTables = GetMatTable(src);
		Mat[,] scaled = Scale(matTables, 2.0, 2.0);
		Util.HorizontalRun(false, Image(matTables), ocr.Process(scaled)).Dump();
	}
}

Mat[,] Scale(Mat[,] src, double fx, double fy)
{
	var result = new Mat[src.GetLength(0), src.GetLength(1)];
	for (int y = 0; y < src.GetLength(0); ++y)
	{
		for (int x = 0; x < src.GetLength(1); ++x)
		{
			Mat cell = src[y, x];
			Mat scaled = cell.Resize(Size.Zero, fx, fy);
			result[y, x] = scaled;
		}
	}
	return result;
}

public class TableOCR : IDisposable
{
	TesseractEngine _eng = new(TesseractHelper.Fast.DataPath, "eng");

	public TableOCR()
	{
		//_eng.SetVariable("tessedit_char_whitelist", "1234567890");
	}

	public string[,] Process(Mat[,] src)
	{
		int rows = src.GetLength(0);
		int cols = src.GetLength(1);
		var result = new string[rows, cols];

		for (int y = 0; y < rows; ++y)
		{
			for (int x = 0; x < cols; ++x)
			{
				Mat cell = src[y, x];
				using Pix pix = Pix.LoadFromMemory(cell.ToBytes(".bmp"));
				using Page page = _eng.Process(pix, PageSegMode.Auto);
				result[y, x] = page.GetText();
			}
		}
		return result;
	}

	public void Dispose()
	{
		_eng.Dispose();
	}
}