<Query Kind="Program">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
</Query>

#load ".\01_perspective-correct"
#load ".\02_perspective-correct2"

void Main()
{
	Environment.CurrentDirectory = Util.GetPassword("dotnet2021-cv-zhoujie-demo5");
	using Mat src = ImReadAsPerspectiveCorrectAgain(@".\resources\demo.webp");
	using SheetContext ctx = GetSheetContext(src);
	AnswerAreas.GetAllAnswers(ctx).Dump();
}

SheetContext GetSheetContext(Mat src)
{
	using Mat gray = src.CvtColor(ColorConversionCodes.RGB2GRAY);
	Mat binary = gray.Threshold(127, 255, ThresholdTypes.BinaryInv);
	CellSpan[] rows = GetRowSpans(binary);
	CellSpan[] cols = GetControlCols(binary, rows).ToArray();
	
	return new (binary, rows, cols);
}

Mat ImReadAsPerspectiveCorrectAgain(string path)
{
	using Mat src = Cv2.ImRead(path);
	using Mat corrected = PerspectiveCorrect(src);
	return PerspectiveCorrectAgain(corrected);
}

public record AnswerArea(int RowStart, int Column, int Length)
{
	public int RowEnd => RowStart + Length;
	
	public (int index, double score) GetSelectedIndex(SheetContext ctx)
	{
		return GetSelectedIndexes(ctx, threshold: 0.1)
			.OrderByDescending(x => x.score)
			.First();
	}

	public IEnumerable<(int index, double score)> GetSelectedIndexes(SheetContext ctx, double threshold = 0.5)
	{
		for (int ri = RowStart; ri < RowStart + Length; ++ri)
		{
			CellSpan row = ctx.Rows[ri];
			CellSpan col = ctx.Cols[Column];
			using Mat roi = ctx.BinaryInv[row.Start, row.End, col.Start, col.End];
			double score = 1.0 * roi.Sum().Val0 / row.Length / col.Length / 255;
			if (score > threshold)
			{
				yield return (ri - RowStart, score);
			}
		}
	}
}

public record SheetContext(Mat BinaryInv, CellSpan[] Rows, CellSpan[] Cols) : IDisposable
{
	public void Dispose() => BinaryInv.Dispose();
}

public abstract record AnswerAreaGroup(string Name, AnswerArea[] Areas)
{
	public abstract string GetResult(SheetContext ctx);
}
public record SingleSelectionAnswer(string Name, AnswerArea Area) : AnswerAreaGroup(Name, new[] { Area })
{
	public override string GetResult(SheetContext ctx) => Area.GetSelectedIndex(ctx).index switch 
	{
		0 => "A", 
		1 => "B", 
		2 => "C", 
		3 => "D", 
		_ => "Error", 
	};
}

public record MultipleSelectionAnswer(string Name, AnswerArea Area) : AnswerAreaGroup(Name, new[] { Area })
{
	public override string GetResult(SheetContext ctx) => string.Concat(Areas[0].GetSelectedIndexes(ctx).Select(idx => idx.index switch
	{
		0 => "A",
		1 => "B",
		2 => "C",
		3 => "D",
		_ => "Error",
	}));
}

public record SubjectAnswer() : AnswerAreaGroup("科目", new[] { new AnswerArea(RowStart: 1, Column: 22, Length: 11) })
{
	public override string GetResult(SheetContext ctx) => Areas[0].GetSelectedIndex(ctx).index switch
	{
		0 => "政治",
		1 => "语文",
		2 => "数学",
		3 => "物理",
		4 => "化学",
		5 => "外语", 
		6 => "历史", 
		7 => "地理", 
		8 => "生物", 
		9 => "文综", 
		10 => "理综", 
		_ => "Error", 
	};
}

public record IdAnswer() : AnswerAreaGroup("学号", GetAreas().ToArray())
{
	private static IEnumerable<AnswerArea> GetAreas()
	{
		for (int i = 0; i < 9; ++i)
		{
			yield return new AnswerArea(1, 10 + i, 10);
		}
	}
	public override string GetResult(SheetContext ctx) => string.Concat(Areas.Select(a => a.GetSelectedIndex(ctx).index switch
	{
		> 9 => "Error", 
		var x => x.ToString()
	}));
}

public static class AnswerAreas
{
	public readonly static SubjectAnswer Subject = new SubjectAnswer();
	public readonly static IdAnswer Id = new IdAnswer();
	public static IEnumerable<AnswerAreaGroup> GetAll()
	{
		yield return Id;
		yield return Subject;
		for (int row = 0; row < 2; ++row)
		{
			for (int group = 0; group < 4; ++group)
			{
				for (int t = 0; t < 5; ++t)
				{
					string title = $"单选题-第{row * 20 + group * 5 + t + 1}题";
					
					yield return new SingleSelectionAnswer(title, new AnswerArea(15 + 6 * row, group * 6 + t, 4));
				}
			}
		}

		for (int row = 0; row < 2; ++row)
		{
			for (int group = 0; group < 4; ++group)
			{
				for (int t = 0; t < 5; ++t)
				{
					string title = $"多选题-第{row * 20 + group * 5 + t + 1 + 40}题";

					yield return new MultipleSelectionAnswer(title, new AnswerArea(27 + 6 * row, group * 6 + t, 4));
				}
			}
		}
	}

	public static Dictionary<string, string> GetAllAnswers(SheetContext ctx) => GetAll()
		.ToDictionary(x => x.Name, x => x.GetResult(ctx));
}