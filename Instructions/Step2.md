## Step 2: Deploy the app

Click the Start Debugging button to deploy the app. We'll keep it open while we edit, and see changes appear live!

<img width="269" height="39" alt="image" src="https://github.com/user-attachments/assets/56aeb74f-8efc-420b-9753-3f4a83a041f9" />

The app should look like this when it launches.

<img width="400" alt="image" src="https://github.com/user-attachments/assets/153119fc-1faf-4a61-91b1-7655b34a4963" />

Notice that there are some execution providers that already appear. By default, the CPU and DirectML execution providers are present on all devices. You might have the device with NPU and We're going to use WinML to dynamically download the execution provider that works with your NPU, so that you can run the model on your NPU!