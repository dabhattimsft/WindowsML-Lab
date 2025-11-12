using Microsoft.ML.OnnxRuntime;
using Microsoft.Windows.AI.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

namespace WinMLLabDemo
{
    internal static class ExecutionLogic
    {
        private static OrtEnv _ortEnv;
        private const string ModelName = "SqueezeNet";
        private const string ModelExtension = ".onnx";

        static ExecutionLogic()
        {
            EnvironmentCreationOptions envOptions = new()
            {
                logId = "WinMLLabDemo",
                logLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_WARNING
            };

            // Pass the options by reference to CreateInstanceWithOptions
            _ortEnv = OrtEnv.CreateInstanceWithOptions(ref envOptions);
        }

        public static IReadOnlyList<OrtEpDevice> LoadExecutionProviders()
        {
            // Get all the EPs available in the environment
            return _ortEnv.GetEpDevices();
        }

        public static async Task InitializeWinMLEPsAsync()
        {
            // TODO-1: Get/Initialize execution providers from the WinML
            // After finishing this step, WinML will find all applicable EPs for your device
            // download the EP for your device, deploy it and register with ONNX Runtime.

            var catalog = ExecutionProviderCatalog.GetDefault();

            await catalog.EnsureAndRegisterCertifiedAsync();
        }

        public static string CompileModelForExecutionProvider(string modelFolder, OrtEpDevice executionProvider)
        {
            string baseModelPath = IOPath.Combine(modelFolder, $"{ModelName}{ModelExtension}");
            string compiledModelPath = ModelHelpers.GetCompiledModelPath(modelFolder, executionProvider);

            try
            {
                var sessionOptions = GetSessionOptions(executionProvider);

                // TODO-2.2: Create compilation options, set the input and output, and compile.
                // After finishing this step, a compiled model will be created at 'compiledModelPath'

                var compileOptions = new OrtModelCompilationOptions(sessionOptions);

                compileOptions.SetInputModelPath(baseModelPath);
                compileOptions.SetOutputModelPath(compiledModelPath);

                // Compile the model
                compileOptions.CompileModel();
            }
            catch
            {
                throw new Exception($"Failed to create session with execution provider: {executionProvider.EpName}");
            }

            return compiledModelPath;
        }

        // WindowsML - Model Catalog
        public static async Task<string> DownloadModel(Action<double> progressCallback)
        {
            // Get the model catalog
            string catalogJsonPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "modelCatalog.json");
            var catalogUri = new Uri(catalogJsonPath);
            var modelCatalogSource = await ModelCatalogSource.CreateFromUriAsync(catalogUri);

            var modelCatalog = new ModelCatalog(new ModelCatalogSource[] { modelCatalogSource });

            // Find model
            var modelInfo = await modelCatalog.FindModelAsync(ModelName);

            // Download the model with progress reporting.
            // NOTE: ModelCatalog API handles caching, so if the model is already downloaded, it won't download it again.
            var getInstanceOp = modelInfo.GetInstanceAsync();
            getInstanceOp.Progress = (op, progress) =>
            {
                progressCallback?.Invoke(progress);
            };

            var result = await getInstanceOp.AsTask();

            if (result.Status != CatalogModelInstanceStatus.Available)
            {
                throw new Exception($"Failed to download model: {ModelName}");
            }

            // Return model path
            var modelInstance = result.GetInstance();
            return modelInstance.ModelPaths[0];
        }

        public static InferenceSession LoadModel(string compiledModelPath, OrtEpDevice executionProvider)
        {
            var sessionOptions = GetSessionOptions(executionProvider);

            // TODO-3: Return an inference session
            return new InferenceSession(compiledModelPath, sessionOptions);
        }

        public static async Task<string> RunModelAsync(InferenceSession session, string imagePath, string modelFolder, OrtEpDevice executionProvider)
        {
            // Prepare inputs
            var inputs = await ModelHelpers.BindInputs(imagePath, session);

            // TODO-4: Run the inference, format and return the results
            using var results = session.Run(inputs);

            return ModelHelpers.FormatResults(modelFolder, results, session);
        }

        private static SessionOptions GetSessionOptions(OrtEpDevice executionProvider)
        {
            // Create a session
            var sessionOptions = new SessionOptions();

            Dictionary<string, string> epOptions = new(StringComparer.OrdinalIgnoreCase);

            switch (executionProvider.EpName)
            {
                case "VitisAIExecutionProvider":
                    sessionOptions.AppendExecutionProvider(_ortEnv, [executionProvider], epOptions);
                    break;

                case "OpenVINOExecutionProvider":
                    // TODO-2.1: Configure threading for OpenVINO EP
                    sessionOptions.AppendExecutionProvider(_ortEnv, [executionProvider], epOptions);
                    break;

                case "QNNExecutionProvider":
                    // TODO-2.1: Configure performance mode for QNN EP
                    sessionOptions.AppendExecutionProvider(_ortEnv, [executionProvider], epOptions);
                    break;

                case "NvTensorRTRTXExecutionProvider":
                    // Configure performance mode for TensorRT RTX EP
                    sessionOptions.AppendExecutionProvider(_ortEnv, [executionProvider], epOptions);
                    break;

                default:
                    break;
            }

            return sessionOptions;
        }
    }
}
