<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>Tesseract</Namespace>
</Query>

#load ".\01_detect-table"
#load ".\02-tesseract-ocr-prepair"

async Task Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo2");
	await TesseractHelper.Fast.EnsureTrainedData(new[] { "chi_sim", "eng" }, QueryCancelToken);
	
	using var ocr = new TableOCR();
	foreach (string file in Directory.EnumerateFiles(@".\resources", "*.jpg").OrderBy(x => x).Take(1))
	{
		using Mat src = Cv2.ImRead(file);
		Mat[,] matTable = GetMatTable(src);
		Mat[,] scaledMat = Scale(matTable, 2, 2);
		Image(scaledMat).Dump();
		ocr.Process(scaledMat).Dump();
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
	TesseractEngine _eng3 = new(TesseractHelper.Fast.DataPath, "eng");
	TesseractEngine _eng4 = new(TesseractHelper.Fast.DataPath, "eng");
	TesseractEngine _chs = new(TesseractHelper.Best.DataPath, "chi_sim");
	
	public TableOCR()
	{
		_eng3.SetVariable("tessedit_char_whitelist", "1234567890");
		_eng4.SetVariable("tessedit_char_whitelist", "1234567890-.%");
		//_chs.SetVariable("tessedit_char_whitelist", "昊天大厦公寓楼鸿富韭菜园环卫局宿舍芙蓉广场月诸葛挂牌均价元平涨跌幅度城区商圈小名");
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
				if (y == 0)
				{
					using Page page = _chs.Process(pix, PageSegMode.Auto);
					result[y, x] = page.GetText();
				}
				else if (x == 3)
				{
					using Page page = _eng3.Process(pix, PageSegMode.SingleBlock);
					result[y, x] = page.GetText();
				}
				else if (x == 4)
				{
					using Page page = _eng4.Process(pix, PageSegMode.SingleBlock);
					result[y, x] = page.GetText();
				}
				else
				{
					using Page page = _chs.Process(pix, PageSegMode.SingleBlock);
					result[y, x] = page.GetText();
				}
			}
		}
		return result;
	}

	public void Dispose()
	{
		_eng3.Dispose();
		_eng4.Dispose();
		_chs.Dispose();
	}
}