<Query Kind="Program">
  <Namespace>System.Runtime.InteropServices</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net.Http</Namespace>
</Query>

void Main()
{
	PdConfig.Version.Dump("version");

	using PdConfig config = new()
	{
		GLogEnabled = false, 
		CpuMathThreadCount = 5, 
		MkldnnEnabled = true, 
	};
	config.SetModel(@"C:\_\3rd\paddle\models\ppocr-v2\det\inference.pdmodel", @"C:\_\3rd\paddle\models\ppocr-v2\det\inference.pdiparams");
	config.Dump();
	using (PdPredictor predictor = config.CreatePredictor())
	{
		using PdTensor input = predictor.GetInputTensor(predictor.InputNames[0]);
		input.Shape = new[] { 1, 3, 256, 320};
		input.SetData(new float[32]);
		predictor.Run().Dump("run");
		
		using PdTensor output = predictor.GetOutputTensor(predictor.OutputNames[0]);
		output.GetData<float>().Sum().Dump("sum");
	}
}

public class PdTensor : IDisposable
{
	private IntPtr _ptr;

	public PdTensor(IntPtr predictorPointer)
	{
		if (predictorPointer == IntPtr.Zero)
		{
			throw new ArgumentNullException(nameof(predictorPointer));
		}
		_ptr = predictorPointer;
	}
	
	public string Name => Marshal.PtrToStringUTF8(PdInvoke.PD_TensorGetName(_ptr));
	public unsafe int[] Shape
	{
		get
		{
			using PdInvoke.PdIntArrayWrapper wrapper = new () { ptr = PdInvoke.PD_TensorGetShape(_ptr) };
			return wrapper.ToArray();
		}
		set
		{
			fixed (int* ptr = value)
			{
				PdInvoke.PD_TensorReshape(_ptr, value.Length, (IntPtr)ptr);
			}
		}
	}

	public unsafe T[] GetData<T>()
	{
		TypeCode code = Type.GetTypeCode(typeof(T));
		Action<IntPtr, IntPtr> copyAction = code switch
		{
			TypeCode.Single => PdInvoke.PD_TensorCopyToCpuFloat, 
			TypeCode.Int32 => PdInvoke.PD_TensorCopyToCpuInt32, 
			TypeCode.Int64 => PdInvoke.PD_TensorCopyToCpuInt64, 
			TypeCode.Byte => PdInvoke.PD_TensorCopyToCpuUint8,
			TypeCode.SByte => PdInvoke.PD_TensorCopyToCpuInt8,
			_ => throw new NotSupportedException($"GetData for {typeof(T).Name} is not supported.")
		};
		
		int[] shape = Shape;
		int size = 1;
		for (int i = 0; i < shape.Length; ++i)
		{
			size *= shape[i];
		}
		
		T[] result = new T[size];
		GCHandle handle = GCHandle.Alloc(result, GCHandleType.Pinned);
		copyAction(_ptr, handle.AddrOfPinnedObject());
		handle.Free();
		
		return result;
	}

	public unsafe void SetData(float[] data)
	{
		fixed (void* ptr = data)
		{
			PdInvoke.PD_TensorCopyFromCpuFloat(_ptr, (IntPtr)ptr);
		}
	}

	public unsafe void SetData(int[] data)
	{
		fixed (void* ptr = data)
		{
			PdInvoke.PD_TensorCopyFromCpuInt32(_ptr, (IntPtr)ptr);
		}
	}

	public unsafe void SetData(long[] data)
	{
		fixed (void* ptr = data)
		{
			PdInvoke.PD_TensorCopyFromCpuInt64(_ptr, (IntPtr)ptr);
		}
	}

	public unsafe void SetData(byte[] data)
	{
		fixed (void* ptr = data)
		{
			PdInvoke.PD_TensorCopyFromCpuUint8(_ptr, (IntPtr)ptr);
		}
	}

	public unsafe void SetData(sbyte[] data)
	{
		fixed (void* ptr = data)
		{
			PdInvoke.PD_TensorCopyFromCpuInt8(_ptr, (IntPtr)ptr);
		}
	}

	public PdDataTypes DataType => (PdDataTypes)PdInvoke.PD_TensorGetDataType(_ptr);

	public void Dispose()
	{
		if (_ptr != IntPtr.Zero)
		{
			PdInvoke.PD_TensorDestroy(_ptr);
			_ptr = IntPtr.Zero;
		}
	}
}

public class PdPredictor : IDisposable
{
	private IntPtr _ptr;

	public PdPredictor(IntPtr predictorPointer)
	{
		if (predictorPointer == IntPtr.Zero)
		{
			throw new ArgumentNullException(nameof(predictorPointer));
		}
		_ptr = predictorPointer;
	}

	public PdPredictor Clone() => new PdPredictor(PdInvoke.PD_PredictorClone(_ptr));

	public string[] InputNames
	{
		get
		{
			using PdInvoke.PdStringArrayWrapper wrapper = new() { ptr = PdInvoke.PD_PredictorGetInputNames(_ptr) };
			return wrapper.ToArray();
		}
	}

	public string[] OutputNames
	{
		get
		{
			using PdInvoke.PdStringArrayWrapper wrapper = new() { ptr = PdInvoke.PD_PredictorGetOutputNames(_ptr) };
			return wrapper.ToArray();
		}
	}

	public PdTensor GetInputTensor(string name) => new PdTensor(PdInvoke.PD_PredictorGetInputHandle(_ptr, name));
	public PdTensor GetOutputTensor(string name) => new PdTensor(PdInvoke.PD_PredictorGetOutputHandle(_ptr, name));

	public int InputSize => (int)PdInvoke.PD_PredictorGetInputNum(_ptr);
	public int OutputSize => (int)PdInvoke.PD_PredictorGetOutputNum(_ptr);
	
	public bool Run() => PdInvoke.PD_PredictorRun(_ptr) != 0;

	public void Dispose()
	{
		if (_ptr != IntPtr.Zero)
		{
			PdInvoke.PD_PredictorDestroy(_ptr);
			_ptr = IntPtr.Zero;
		}
	}
}

public class PdConfig : IDisposable
{
	private IntPtr _ptr;

	public PdConfig()
	{
		_ptr = PdInvoke.PD_ConfigCreate();
	}

	public PdConfig(IntPtr configPointer)
	{
		_ptr = configPointer;
	}

	static PdConfig()
	{
		Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + @"C:\_\3rd\paddle\dll");
	}

	public static string Version => Marshal.PtrToStringUTF8(PdInvoke.PD_GetVersion());

	public bool GLogEnabled
	{
		get => PdInvoke.PD_ConfigGlogInfoDisabled() == 0;
		set
		{
			if (!value)
			{
				PdInvoke.PD_ConfigDisableGlogInfo(_ptr);
			}
			else if (!GLogEnabled)
			{
				Console.WriteLine($"Warn: Glog cannot re-enable after disabled.");
			}
		}
	}

	public bool IsMemoryModel => PdInvoke.PD_ConfigModelFromMemory(_ptr) != 0;
	public bool MkldnnEnabled
	{
		get => PdInvoke.PD_ConfigMkldnnEnabled(_ptr) != 0;
		set
		{
			if (value)
			{
				PdInvoke.PD_ConfigEnableMKLDNN(_ptr);
			}
			else if (MkldnnEnabled)
			{
				Console.WriteLine($"Warn: Mkldnn cannot disabled after enabled.");
			}
		}
	}
	
	private int _MkldnnCacheCapacity = 0;
	public int MkldnnCacheCapacity
	{
		get => _MkldnnCacheCapacity;
		set
		{
			_MkldnnCacheCapacity = value;
			PdInvoke.PD_ConfigSetMkldnnCacheCapacity(_ptr, value);
		}
	}

	//public string ModelDir
	//{
	//	get => Marshal.PtrToStringUTF8(PdInvoke.PD_ConfigGetModelDir(_ptr));
	//	set => PdInvoke.PD_ConfigSetModelDir(_ptr, value);
	//}

	public void SetModel(string programPath, string paramsPath)
	{
		if (programPath == null) throw new ArgumentNullException(nameof(programPath));
		if (paramsPath == null) throw new ArgumentNullException(nameof(paramsPath));
		if (!File.Exists(programPath)) throw new FileNotFoundException("programPath not found", programPath);
		if (!File.Exists(paramsPath)) throw new FileNotFoundException("paramsPath not found", paramsPath);
		PdInvoke.PD_ConfigSetModel(_ptr, programPath, paramsPath);
	}

	public string ProgramPath => Marshal.PtrToStringUTF8(PdInvoke.PD_ConfigGetProgFile(_ptr));
	public string ParamsPath => Marshal.PtrToStringUTF8(PdInvoke.PD_ConfigGetParamsFile(_ptr));

	public unsafe void SetMemoryModel(byte[] programBuffer, byte[] paramsBuffer)
	{
		fixed (byte* pprogram = programBuffer)
		fixed (byte* pparams = paramsBuffer)
		{
			PdInvoke.PD_ConfigSetModelBuffer(_ptr,
				(IntPtr)pprogram, programBuffer.Length,
				(IntPtr)pparams, paramsBuffer.Length);
		}
	}

	public int CpuMathThreadCount
	{
		get => PdInvoke.PD_ConfigGetCpuMathLibraryNumThreads(_ptr);
		set => PdInvoke.PD_ConfigSetCpuMathLibraryNumThreads(_ptr, value);
	}

	public PdPredictor CreatePredictor()
	{
		try 
		{
			return new PdPredictor(PdInvoke.PD_PredictorCreate(_ptr));
		}
		finally 
		{
			_ptr = IntPtr.Zero;
		}
	}

	public void Dispose()
	{
		if (_ptr != IntPtr.Zero)
		{
			PdInvoke.PD_ConfigDestroy(_ptr);
			_ptr = IntPtr.Zero;
		}
	}
}

public enum PdDataTypes
{
	Unknown = -1,
	Float32,
	Int32,
	Int64,
	UInt8,
	Int8,
}

public class PdInvoke
{
	private unsafe struct PdStringArray
	{
		public nint Size;
		public byte** Data;

		public string[] ToArray()
		{
			var result = new string[Size];
			for (int i = 0; i < Size; ++i)
			{
				result[i] = Marshal.PtrToStringUTF8((IntPtr)Data[i]);
			}
			return result;
		}
	}

	public unsafe ref struct PdStringArrayWrapper
	{
		public IntPtr ptr;

		public unsafe string[] ToArray()
		{
			return ((PdStringArray*)ptr)->ToArray();
		}

		public void Dispose()
		{
			PD_OneDimArrayCstrDestroy(ptr);
			ptr = IntPtr.Zero;
		}
	}

	private unsafe struct PdIntArray
	{
		public nint Size;
		public int* Data;

		public int[] ToArray()
		{
			var result = new int[Size];
			for (int i = 0; i < Size; ++i)
			{
				result[i] = Data[i];
			}
			return result;
		}

		public unsafe void Dispose()
		{
			fixed (PdIntArray* ptr = &this)
			{
				PD_OneDimArrayInt32Destroy((IntPtr)ptr);
			}
		}
	}

	public unsafe ref struct PdIntArrayWrapper
	{
		public IntPtr ptr;

		public unsafe int[] ToArray()
		{
			return ((PdIntArray*)ptr)->ToArray();
		}

		public void Dispose()
		{
			PD_OneDimArrayInt32Destroy(ptr);
			ptr = IntPtr.Zero;
		}
	}

	public const string PaddleInferenceCLib = @"C:\_\3rd\paddle\dll\paddle_inference_c.dll";

	[DllImport(PaddleInferenceCLib)]
	public static extern IntPtr PD_GetVersion();

	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_ConfigCreate();
	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_ConfigDestroy(IntPtr config);
	[DllImport(PaddleInferenceCLib)] public static extern byte PD_ConfigGlogInfoDisabled();
	[DllImport(PaddleInferenceCLib)] public static extern void PD_ConfigDisableGlogInfo(IntPtr config);
	[DllImport(PaddleInferenceCLib)] public static extern byte PD_ConfigModelFromMemory(IntPtr config);
	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_ConfigGetModelDir(IntPtr config);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_ConfigSetModelDir(IntPtr config, string modelDir);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_ConfigSetModel(IntPtr config, string modelPath, string paramsPath);
	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_ConfigGetProgFile(IntPtr config);
	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_ConfigGetParamsFile(IntPtr config);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_ConfigSetModelBuffer(IntPtr config, IntPtr programBuffer, nint programBufferSize, IntPtr paramsBuffer, nint paramsBufferSize);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_ConfigSetCpuMathLibraryNumThreads(IntPtr config, int threadCount);
	[DllImport(PaddleInferenceCLib)] public static extern int PD_ConfigGetCpuMathLibraryNumThreads(IntPtr config);
	[DllImport(PaddleInferenceCLib)] public static extern byte PD_ConfigMkldnnEnabled(IntPtr config);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_ConfigEnableMKLDNN(IntPtr config);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_ConfigSetMkldnnCacheCapacity(IntPtr config, int capacity);
	

	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_PredictorCreate(IntPtr config);
	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_PredictorClone(IntPtr predictor);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_PredictorDestroy(IntPtr predictor);
	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_PredictorGetInputNames(IntPtr predictor);
	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_PredictorGetOutputNames(IntPtr predictor);
	[DllImport(PaddleInferenceCLib)] public static extern nint PD_PredictorGetInputNum(IntPtr predictor);
	[DllImport(PaddleInferenceCLib)] public static extern nint PD_PredictorGetOutputNum(IntPtr predictor);
	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_PredictorGetInputHandle(IntPtr predictor, string name);
	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_PredictorGetOutputHandle(IntPtr predictor, string name);
	[DllImport(PaddleInferenceCLib)] public static extern byte PD_PredictorRun(IntPtr predictor);

	
	[DllImport(PaddleInferenceCLib)] public static extern void PD_OneDimArrayInt32Destroy(IntPtr array);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_OneDimArrayCstrDestroy(IntPtr array);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_OneDimArraySizeDestroy(IntPtr array);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TwoDimArraySizeDestroy(IntPtr array);
	

	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorDestroy(IntPtr tensor);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorReshape(IntPtr tensor, nint size, IntPtr shape);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorCopyFromCpuFloat(IntPtr tensor, IntPtr data);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorCopyFromCpuInt64(IntPtr tensor, IntPtr data);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorCopyFromCpuInt32(IntPtr tensor, IntPtr data);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorCopyFromCpuUint8(IntPtr tensor, IntPtr data);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorCopyFromCpuInt8(IntPtr tensor, IntPtr data);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorCopyToCpuFloat(IntPtr tensor, IntPtr data);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorCopyToCpuInt64(IntPtr tensor, IntPtr data);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorCopyToCpuInt32(IntPtr tensor, IntPtr data);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorCopyToCpuUint8(IntPtr tensor, IntPtr data);
	[DllImport(PaddleInferenceCLib)] public static extern void PD_TensorCopyToCpuInt8(IntPtr tensor, IntPtr data);	
	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_TensorGetShape(IntPtr tensor);
	[DllImport(PaddleInferenceCLib)] public static extern IntPtr PD_TensorGetName(IntPtr tensor);
	[DllImport(PaddleInferenceCLib)] public static extern int PD_TensorGetDataType(IntPtr tensor);
}