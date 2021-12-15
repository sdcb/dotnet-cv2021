<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Tesseract</Namespace>
  <Namespace>System.Net</Namespace>
</Query>

#load ".\01_detect-table"
#load "..\demo3-chs-table-detection\02-tesseract-ocr-prepair"

async Task Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo2");
	await TesseractHelper.Best.EnsureTrainedData(new[] { "chi_sim", "eng" }, QueryCancelToken);

	using var ocr = new TableOCR();
	foreach (string file in Directory.EnumerateFiles(@".\resources", "*.jpg").OrderBy(x => x).Take(1))
	{
		using Mat src = Cv2.ImRead(file);
		MatRow[] matTables = GetMatTable2(src).ToArray();
		ResultRow[] result = ocr.Process(matTables, QueryCancelToken);
		ResultRow.Visualize(result).Dump();

		//Mat[,] matTables = GetMatTable(src);
		//Mat[,] scaled = Scale(matTables, 2.0, 2.0);
		//Util.HorizontalRun(false, Image(src), ocr.Process(scaled)).Dump();
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
	TesseractEngine _eng = new(TesseractHelper.Best.DataPath, "eng");
	TesseractEngine _engStar = new(TesseractHelper.Best.DataPath, "eng");
	TesseractEngine _engNum = new(TesseractHelper.Best.DataPath, "eng");

	public TableOCR()
	{
		_engStar.SetVariable("tessedit_char_whitelist", "*-");
		_engNum.SetVariable("tessedit_char_whitelist", "1234567890");
	}

	public string[,] Process(Mat[,] src, CancellationToken cancellationToken = default)
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

	public ResultRow[] Process(MatRow[] src, CancellationToken cancellationToken = default, int level = 0)
	{
		return src.Select((r, y) =>
		{
			string[] texts = r.ColumnMats
				.Select((m, mi) =>
				{
					if (cancellationToken.IsCancellationRequested) return null;
					int x = level switch
					{
						0 => mi,
						1 => mi + 2,
						2 => mi + 3,
						_ => throw new NotSupportedException()
					};
					
					TesseractEngine engine = (y, x) switch 
					{
						(> 0, 1) => _engStar, 
						(> 0, 2) => _engNum, 
						_ => _eng
					};
					PageSegMode mode = (y, x) switch 
					{
						(_, 0) => PageSegMode.Auto, 
						_ => PageSegMode.Auto,
					};
					double scaledRate = (y, x) switch
					{
						(_, 2) => 1.2, 
						(_, 1) => 5, 
						_ => 2, 
					};
					
					using Mat scaled = m.Resize(Size.Zero, scaledRate, scaledRate);
					using Pix pix = Pix.LoadFromMemory(scaled.ToBytes(".bmp"));
					using Page page = engine.Process(pix, mode);
					return page.GetText();
				})
				.ToArray();

			return new ResultRow(texts, Process(r.ChildRows, cancellationToken, level + 1));
		}).ToArray();
	}

	public void Dispose()
	{
		_eng.Dispose();
		_engStar.Dispose();
	}
}