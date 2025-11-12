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

        // WindowsML-Lab-phi: download model
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

            if (result.Status != CatalogModelInstanceStatus.Available)
            {
                throw new Exception($"Failed to download model: {ModelName}, Error: {result.ExtendedError}");
            }

            // WindowsML-Lab-phi: Return model path
            var modelInstance = result.GetInstance();
            return modelInstance.ModelPaths[0];
        }

        // WindowsML-Lab-phi: load model
        public static async Task<(Tokenizer tokenizer, Generator generator)> LoadModel(string modelFolder, OrtEpDevice executionProvider)
        {
            // WindowsML-Lab-phi: Configure ONNX Runtime GenAI model settings:
            Config config = new Config(modelFolder);
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

                case "NvTensorRTRTXExecutionProvider":
                    config.SetProviderOption(executionProvider.EpName, "enable_cuda_graph", "true");
                    break;

                case "VitisAIExecutionProvider":
                    config.SetProviderOption(executionProvider.EpName, "log_level", "info");
                    break;

                default:
                    break;
            }

            // WindowsML-Lab-phi: Initialize ONNX Runtime GenAI components for text generation:
            // WindowsML-Lab-phi:  Model - Loads the ONNX model with the specified configuration and execution provider
            Model model = new Model(config);
            // WindowsML-Lab-phi: Tokenizer - Handles text-to-token conversion and vice versa for model input/output
            Tokenizer tokenizer = new Tokenizer(model);

            // WindowsML-Lab-phi: GeneratorParams - Configures generation constraints (min 50, max 500 tokens)
            GeneratorParams generatorParams = new GeneratorParams(model);
            generatorParams.SetSearchOption("min_length", 50);
            generatorParams.SetSearchOption("max_length", 500);

            // - Generator: Performs the actual text generation using the model and parameters
            var generator = new Generator(model, generatorParams);

            return (tokenizer, generator);
        }

        // WindowsML-Lab-phi: generate response
        public static async Task<string> GenerateModelResponseAsync(
            Tokenizer tokenizer,
            Generator generator,
            string userPrompt,
            Action<string> onTokenGenerated)
        {
            // WindowsML-Lab-phi: Give instructions to the model.
            // WindowsML-Lab-phi: Create a tokenizer stream for efficient token-by-token decoding during generation
            using var tokenizerStream = tokenizer.CreateStream();
            string messages = $@"[{{""role"":""system"",""content"":""You are a helpful AI assistant.""}},{{""role"":""user"",""content"":""{userPrompt}""}}]";

             // WindowsML-Lab-phi: Apply the model's chat template and encode the formatted text into token sequences
            var sequences = tokenizer.Encode(tokenizer.ApplyChatTemplate("", messages, "", true));

            // WindowsML-Lab-phi: Feed the tokenized input to the generator as initial context
            generator.AppendTokenSequences(sequences);

            // WindowsML-Lab-phi: Generate response from the model.
            StringBuilder response = new StringBuilder();
            while (!generator.IsDone())
            {
                // WindowsML-Lab-phi: Generate next token and decode.
                generator.GenerateNextToken();
                string token = tokenizerStream.Decode(generator.GetSequence(0)[^1]);
                response.Append(token);

                onTokenGenerated?.Invoke(response.ToString());
            }

            return response.ToString();
        }
    }
}
