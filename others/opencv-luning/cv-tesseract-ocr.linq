<Query Kind="Statements">
  <NuGetReference>OpenCvSharp4</NuGetReference>
  <NuGetReference>OpenCvSharp4.runtime.win</NuGetReference>
  <Namespace>OpenCvSharp.Text</Namespace>
  <Namespace>OpenCvSharp</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
</Query>

using var ocr = OCRTesseract.Create(Util.GetPassword("tesseract-datapath"), language: "chi_sim");
ocr.SetWhiteList("嘉兴宇民伍号投资合伙企业（有限合伙）");
using Mat mat = Cv2.ImDecode(GetClipboardImage(), ImreadModes.Color);
ocr.Run(mat, 
	out string outputText, 
	out Rect[] _, 
	out string[] _, 
	out float[] _);
	
outputText.Dump();

byte[] GetClipboardImage()
{
	using var ms = new MemoryStream();
	Clipboard.GetImage().Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
	return ms.ToArray();
}