using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntimeGenAI;
using Microsoft.VisualBasic;
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
        private const string ModelName = "phi-3-mini";
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

        public static string CompileModelForExecutionProvider(OrtEpDevice executionProvider)
        {
            string baseModelPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{ModelName}{ModelExtension}");
            string compiledModelPath = ModelHelpers.GetCompiledModelPath(executionProvider);

            try
            {
                var sessionOptions = GetSessionOptions(executionProvider);

                // TODO-2.2: Create compilation options, set the input and output, and compile.
                // After finishing this step, a compiled model will be created at 'compiledModelPath'

// TODOTODO
            }
            catch
            {
                throw new Exception($"Failed to create session with execution provider: {executionProvider.EpName}");
            }

            return compiledModelPath;
        }

        public static async Task<string> DownloadModel(Action<double> progressCallback)
        {
            // WindowsML-Lab-phi: Get the model catalog
            string catalogJsonPath = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "modelCatalog.json");
            var catalogUri = new Uri(catalogJsonPath);
            var modelCatalogSource = await ModelCatalogSource.CreateFromUriAsync(catalogUri);

            var modelCatalog = new ModelCatalog(new ModelCatalogSource[] { modelCatalogSource });

            // WindowsML-Lab-phi: Find "phi-3-mini" model
            var modelInfo = await modelCatalog.FindModelAsync("phi-3-mini");

            // WindowsML-Lab-phi: Download the model with progress reporting.
            // NOTE: ModelCatalog API handles caching, so if the model is already downloaded, it won't download it again.
            var getInstanceOp = modelInfo.GetInstanceAsync();
            getInstanceOp.Progress = (op, progress) =>
            {
                progressCallback?.Invoke(progress);
            };

            var result = await getInstanceOp.AsTask();

            // WindowsML-Lab-phi: Return model path
            var modelInstance = result.GetInstance();
            return modelInstance.ModelPaths[0];
        }

        public static async Task<(Tokenizer tokenizer, Generator generator)> LoadModel(string compiledModelPath, OrtEpDevice executionProvider)
        {
            var sessionOptions = GetSessionOptions(executionProvider);

            Config config = new Config(compiledModelPath);
            config.ClearProviders();
            config.AppendProvider(executionProvider.EpName);

            switch (executionProvider.EpName)
            {
                case "OpenVINOExecutionProvider":
                    config.SetProviderOption(executionProvider.EpName, "num_of_threads", "4");
                    break;

                case "QNNExecutionProvider":
                    config.SetProviderOption(executionProvider.EpName, "htp_performance_mode", "high_performance");
                    break;

                default:
                    break;
            }

            Model model = new Model(config);
            Tokenizer tokenizer = new Tokenizer(model);

            GeneratorParams generatorParams = new GeneratorParams(model);
            generatorParams.SetSearchOption("min_length", 50);
            generatorParams.SetSearchOption("max_length", 500);
            var generator = new Generator(model, generatorParams);

            return (tokenizer, generator);
        }

        public static async Task<string> GenerateModelResponseAsync(
            Tokenizer tokenizer,
            Generator generator,
            string userPrompt,
            Action<string> onTokenGenerated)
        {
            using var tokenizerStream = tokenizer.CreateStream();
            string messages = $@"[{{""role"":""system"",""content"":""You are a helpful AI assistant.""}},{{""role"":""user"",""content"":""{userPrompt}""}}]";

            var sequences = tokenizer.Encode(tokenizer.ApplyChatTemplate("", messages, "", true));
            generator.AppendTokenSequences(sequences);

            StringBuilder response = new StringBuilder();
            while (!generator.IsDone())
            {
                generator.GenerateNextToken();
                string token = tokenizerStream.Decode(generator.GetSequence(0)[^1]);
                response.Append(token);

                // Call the callback with the current response
                onTokenGenerated?.Invoke(response.ToString());
            }

            return response.ToString();
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
