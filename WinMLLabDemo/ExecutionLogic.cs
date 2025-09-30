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
            // TODO: Initialize the OrtEnv instance
        }

        public static IReadOnlyList<OrtEpDevice> LoadExecutionProviders()
        {
            // Get all the EPs available in the environment
            return _ortEnv.GetEpDevices();
        }

        public static async Task InitializeWinMLEPsAsync()
        {
            // TODO: Get/Initialize execution providers from the WinML
        }

        public static string CompileModelForExecutionProvider(OrtEpDevice executionProvider)
        {
            // TODO: Compile Model
            return string.Empty;
        }

        public static InferenceSession LoadModel(string compiledModelPath, OrtEpDevice executionProvider)
        {
            var sessionOptions = GetSessionOptions(executionProvider);
            return new InferenceSession(compiledModelPath, sessionOptions);
        }

        public static async Task<string> RunModelAsync(InferenceSession session, string imagePath, string compiledModelPath, OrtEpDevice executionProvider)
        {
            // Prepare inputs
            var inputs = await ModelHelpers.BindInputs(imagePath, session);

            // Run inference
            using var results = session.Run(inputs);

            // Format the results
            return ModelHelpers.FormatResults(results, session);
        }

        private static SessionOptions GetSessionOptions(OrtEpDevice executionProvider)
        {
            // Create a session
            var sessionOptions = new SessionOptions();

            // TODO: Create Inference session

            return sessionOptions;
        }
    }
}
