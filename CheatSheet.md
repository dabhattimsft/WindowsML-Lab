## TODO-1: Get/Initialize execution providers from the WinML

```csharp
public static async Task InitializeWinMLEPsAsync()
{
    // TODO-1: Get/Initialize execution providers from the WinML
    // After finishing this step, WinML will find all applicable EPs for your device
    // download the EP for your device, deploy it and register with ONNX Runtime.

    // Get the WinML EP catalog
    var catalog = ExecutionProviderCatalog.GetDefault();

    // Check if there's any new EPs to download, and if so, download them,
    // and then register all the EPs with the WinML copy of ONNX Runtime
    await catalog.EnsureAndRegisterCertifiedAsync();
}
```
## TODO-2: Compiling the model

```csharp
public static string CompileModelForExecutionProvider(OrtEpDevice executionProvider)
{ 
    // .... other code ...
    // ...................
    var sessionOptions = GetSessionOptions(executionProvider);

    // TODO-2: Create compilation options, set the input and output, and compile.
    // After finishing this step, a compiled model will be created at 'compiledModelPath'

    // Create compilation options from session options
    var compileOptions = new OrtModelCompilationOptions(sessionOptions);

    // Set input and output model paths
    compileOptions.SetInputModelPath(baseModelPath);
    compileOptions.SetOutputModelPath(compiledModelPath);

    // Compile the model
    compileOptions.CompileModel();
    
    // .... other code ...
    // ...................
}
```

## TODO-3: Loading the model
```csharp
public static InferenceSession LoadModel(string compiledModelPath, OrtEpDevice executionProvider)
{
    var sessionOptions = GetSessionOptions(executionProvider);

    // TODO-3: Return an inference session
    // Return an inference session
    return new InferenceSession(compiledModelPath, sessionOptions);
}
```

## TODO-4: Inferencing the model
```csharp
public static async Task<string> RunModelAsync(InferenceSession session, string imagePath, string compiledModelPath, OrtEpDevice executionProvider)
{
    // Prepare inputs
    var inputs = await ModelHelpers.BindInputs(imagePath, session);

    // TODO-4: Run the inference, format and return the results
    // Run inference
    using var results = session.Run(inputs);

    // Format the results
    return ModelHelpers.FormatResults(results, session);
}
```