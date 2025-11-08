using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntimeGenAI;
using Microsoft.Win32;
using Microsoft.Windows.AI.MachineLearning;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Xml.Linq;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using IOPath = System.IO.Path;

namespace WinMLLabDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Generator? _generator;
        private Tokenizer? _tokenizer;
        public ObservableCollection<OrtEpDevice> ExecutionProviders { get; set; }
        public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();

        private string selectedImagePath = string.Empty;
        private OrtEpDevice? selectedExecutionProvider = null;
        private string _modelFolderPath = null;

        public MainWindow()
        {
            InitializeComponent();

            ExecutionProviders = new ObservableCollection<OrtEpDevice>();
            ExecutionProvidersGrid.ItemsSource = ExecutionProviders;
            
            // Initialize Messages collection and bind to ChatMessages ItemsControl
            Messages = new ObservableCollection<ChatMessage>();
            ChatMessages.ItemsSource = Messages;
            
            // Set up EP selection event
            ExecutionProvidersGrid.SelectionChanged += ExecutionProvidersGrid_SelectionChanged;

            // Initialize with some sample data
            LoadExecutionProviders();
            WriteToConsole("WinML Demo Application initialized.");
        }

        private void LoadExecutionProviders()
        {
            ExecutionProviders.Clear();

            var eps = ExecutionLogic.LoadExecutionProviders();

            foreach (var ep in eps)
            {
                ExecutionProviders.Add(ep);
            }

            WriteToConsole("Loaded execution providers.");
        }

        private void ExecutionProvidersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExecutionProvidersGrid.SelectedItem is OrtEpDevice selectedEP)
            {
                selectedExecutionProvider = selectedEP;
                _generator = null;
                _tokenizer = null;
                SendButton.IsEnabled = false;
                WriteToConsole($"Selected execution provider: {selectedEP.EpName}");
            }
            else
            {
                selectedExecutionProvider = null;
                _generator = null;
                _tokenizer = null;
                SendButton.IsEnabled = false;
            }

            // Update button states
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (selectedExecutionProvider == null)
            {
                LoadModelButton.IsEnabled = false;
            }

            if (_modelFolderPath == null)
            {
                LoadModelButton.IsEnabled = false;
                return;
            }

            LoadModelButton.IsEnabled = Directory.Exists(_modelFolderPath);
        }

        private void RefreshEPButton_Click(object sender, RoutedEventArgs e)
        {
            LoadExecutionProviders();
            // Reset selection state
            selectedExecutionProvider = null;
            _generator = null;
            _tokenizer = null;
            SendButton.IsEnabled = false;
        }

        private async void InitializeWinMLEPsButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeWinMLEPsButton.IsEnabled = false;
            try
            {
                WriteToConsole("WinML: Downloading and registering EPs...");
                var now = DateTime.Now;

                // Download and register the Execution Providers for our device
                await ExecutionLogic.InitializeWinMLEPsAsync();

                var elapsed = DateTime.Now - now;
                WriteToConsole($"WinML: EPs downloaded and registered in {elapsed.TotalMilliseconds} ms.");
                LoadExecutionProviders();
            }
            catch (Exception ex)
            {
                WriteToConsole($"Error downloading execution providers: {ex.Message}");
            }
            finally
            {
                InitializeWinMLEPsButton.IsEnabled = true;
            }
        }


        private async void DownloadModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedExecutionProvider == null)
            {
                WriteToConsole("Please select an execution provider first.");
                return;
            }

            try
            {
                // Download model if needed
                DateTime modelDownloadStart = DateTime.Now;
                WriteToConsole($"Downloading model if not already downloaded...");
                _modelFolderPath = await ExecutionLogic.DownloadModel(progress =>
                {
                    WriteToConsole($"Model download progress: {progress}%");
                });
                var modelDownloadElapsed = DateTime.Now - modelDownloadStart;
                WriteToConsole($"Model downloaded successfully in {modelDownloadElapsed.TotalMilliseconds} ms.");

                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                WriteToConsole($"Error loading model: {ex.Message}");
            }
        }

        private async void LoadModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedExecutionProvider == null)
            {
                WriteToConsole("Please select an execution provider first.");
                return;
            }

            if (_modelFolderPath == null)
            {
                WriteToConsole("Model not downloaded yet. Please download the model first.");
                return;
            }

            try
            {
                // Load model
                DateTime start = DateTime.Now;
                WriteToConsole($"Loading model for execution provider: {selectedExecutionProvider.EpName}");
                (_tokenizer, _generator) = await Task.Run(() => ExecutionLogic.LoadModel(_modelFolderPath, selectedExecutionProvider));
                var elapsed = DateTime.Now - start;
                WriteToConsole($"Model loaded successfully in {elapsed.TotalMilliseconds} ms.");

                SendButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                WriteToConsole($"Error loading model: {ex.Message}");
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageInput.Text))
                return;

            if (_generator == null || _tokenizer == null)
            {
                Messages.Add(new ChatMessage("Please load the model first.", false));
                return;
            }

            // Get and store the user's message
            string userPrompt = MessageInput.Text;
            Messages.Add(new ChatMessage(userPrompt, true));

            // Clear input and scroll to bottom
            MessageInput.Clear();
            ChatScrollViewer.ScrollToBottom();

            try
            {
                // Create AI message first
                var modelMessage = new ChatMessage("", false);
                Messages.Add(modelMessage);

                await Task.Run(async () =>
                {
                    string response = await ExecutionLogic.GenerateModelResponseAsync(
                        _tokenizer, 
                        _generator, 
                        userPrompt,
                        token =>
                        {
                            // Update UI on the UI thread for each token generated
                            Dispatcher.Invoke(() =>
                            {
                                modelMessage.Message = token;
                                ChatScrollViewer.ScrollToBottom();
                            });
                        });

                    // Final update to model message with the complete response
                    modelMessage.Message = response;
                });
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage($"Error: {ex.Message}", false));
                WriteToConsole($"Error during chat: {ex.Message}");
            }
        }

        private void ClearConsoleButton_Click(object sender, RoutedEventArgs e)
        {
            ConsoleTextBlock.Text = string.Empty;
        }

        public void WriteToConsole(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] {message}\n";
            
            Dispatcher.Invoke(() =>
            {
                ConsoleTextBlock.Text += logEntry;
                
                // Auto-scroll to bottom
                if (ConsoleTextBlock.Parent is ScrollViewer scrollViewer)
                {
                    scrollViewer.ScrollToEnd();
                }
            });
        }
    }
}