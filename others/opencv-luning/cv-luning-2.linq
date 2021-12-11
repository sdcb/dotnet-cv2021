<Query Kind="Statements">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp</Namespace>
</Query>

using Mat src = Cv2.ImRead(@"C:\Users\ZhouJie\Pictures\OpenCVDemo.png");
using Mat gray = src.CvtColor(ColorConversionCodes.RGB2GRAY);
var sw = Stopwatch.StartNew();
using Mat dest = gray.AdaptiveThreshold(255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 51, 25);
Util.Image(dest.ToBytes(), Util.ScaleMode.Unscaled).Dump();
sw.ElapsedMilliseconds.Dump();