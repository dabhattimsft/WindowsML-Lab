# Introduction
## Local language model app
This is a fork of the demo app. This app utilizes (phi-3 model)[https://azure.microsoft.com/en-us/blog/introducing-phi-3-redefining-whats-possible-with-slms/?msockid=0012df25b14061c52557cc8eb5406fa2] to create a local chat bot.

This is complete app and doesn't have any TODOs. All the relevant logic is in ExecutionLogic.cs and look for **// WindowsML-Lab-phi** comments for more details.
This branch also uses ModelCatalog APIs of WindowsML to download models dynamically. You can [learn more about ModelCatalog APIs here](https://learn.microsoft.com/en-us/windows/ai/new-windows-ml/model-catalog/overview).

To run this app,
- Click on "Download Model" to download phi model. NOTE: This can take a while since model is large and we might be constrained with bandwidth here.
- Select Execution provider
- Click on "Load Model". This can take some time. 
  - Console will show log "Model loaded successfully" and once model is loaded "Send" button will be enabled.
- Type prompt in the text box and click "Send"



https://github.com/user-attachments/assets/2b50e798-b1b8-4955-bb97-f1c04f936b0c


https://github.com/user-attachments/assets/2a98f188-1a1a-45f7-8a52-41bdd661c3a3




### Download Model
```csharp
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

            // WindowsML-Lab-phi: Return model path
            var modelInstance = result.GetInstance();
            return modelInstance.ModelPaths[0];
        }
```

### Load Model
```csharp
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
```

### Running inference
```csharp
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
```

# References
[Windows ML Overview](https://learn.microsoft.com/en-us/windows/ai/new-windows-ml/overview)

[Windows ML API Reference](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.ai.machinelearning?view=windows-app-sdk-1.8)

[ONNX Runtime GenAI](https://onnxruntime.ai/docs/genai/)
