<Query Kind="Statements">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>LINQPad.Controls</Namespace>
  <Namespace>OpenCvSharp</Namespace>
</Query>

Util.HtmlHead.AddStyles(@"
.red { border-color: red }
.green { border-color: green }
.blue { border-color: blue }
");
Util.Metatext("HLS = hue, lightness, saturation").Dump();

var rbox = new TextBox { Width = "2em", Text = "86", CssClass = "red" };
var gbox = new TextBox { Width = "2em", Text = "35", CssClass = "green" };
var bbox = new TextBox { Width = "2em", Text = "224", CssClass = "blue" };
var checkBtn = new Button("Check");
var inputs = new[] { rbox, gbox, bbox };
Util.HorizontalRun(false, rbox, gbox, bbox, checkBtn).Dump("RGB");
var dc = new DumpContainer().Dump("Result");
checkBtn.Click += (o, e) => Update();
Update();

void Update()
{
	if (
		int.TryParse(rbox.Text, out int r) &&
		int.TryParse(gbox.Text, out int g) &&
		int.TryParse(bbox.Text, out int b))
	{
		using Mat mat = new(new Size(1, 1), MatType.CV_8UC3, new Scalar(b, g, r));
		using Mat hlsMat = mat.CvtColor(ColorConversionCodes.RGB2HLS);
		using Mat hsvMat = mat.CvtColor(ColorConversionCodes.RGB2HSV);
		Vec3b hls = hlsMat.At<Vec3b>(0);
		Vec3b hsv = hsvMat.At<Vec3b>(0);
		dc.Content = new
		{
			RGB = $"{r}, {g}, {b}", 
			HLS = $"{hls.Item0}, {hls.Item1}, {hls.Item2}",
			HSV = $"{hsv.Item0}, {hsv.Item1}, {hsv.Item2}",
		};
	}
	else
	{
		dc.Content = Util.Metatext("RGB->HLS convert failed");
	}
}