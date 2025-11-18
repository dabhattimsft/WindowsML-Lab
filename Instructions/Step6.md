## Step 6: Implement compiling the model

For these hardware-specific EPs, models need to be compiled against the EP before you can use the model. 

Back in our **ExecutionLogic.cs** file, locate the `CompileModelForExecutionProvider` method. 

Within the `// TODO` in `CompileModelForExecutionProvider`, you'll first need to create a new `compileOptions` via `new OrtModelCompilationOptions(sessionOptions)`, passing in the sessionOptions that are specific to the EP we've selected.

Then, you'll need to use `SetInputModelPath` and `SetOutputModelPath` to indicate the source and target model paths.

Finally, you'll call `CompileModel` to produce the compiled model.

Your final code within `CompileModelForExecutionProvider` should look something like this...

```csharp
var sessionOptions = GetSessionOptions(executionProvider);

// Create compilation options from session options
var compileOptions = new OrtModelCompilationOptions(sessionOptions);

// Set input and output model paths
compileOptions.SetInputModelPath(baseModelPath);
compileOptions.SetOutputModelPath(compiledModelPath);

// Compile the model
compileOptions.CompileModel();
```

Save your changes (`Ctrl+S`) and then press the **Hot Reload** button (or `Alt+F10`).

Then, switch back to the app, select the **QNNExecutionProvider**/**OpenVINOExecutionProvider**/**NvTensorRtRtxExecutionProvider**/**VitisAIExecutionProvider** EP, and click the **Compile Model** button. This will take ~15 seconds, but in the console output you should eventually see that it outputs a compiled model path!

<img width="400" alt="image" src="https://github.com/user-attachments/assets/71c02862-09d6-4891-a55e-0476a1603a15" />

The model is now ready to load on the NPU! Note that our app implements caching logic, so that if the model is already compiled on disk, it will use the already-compiled version, so users only have to experience the initial compile once.