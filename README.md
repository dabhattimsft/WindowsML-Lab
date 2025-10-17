# Windows ML Lab Demo

In this lab demo, we're going to be building an image classification app that can take in any image and locally identify what prominent features might be in the image, like the breed of a dog. We'll be using the ONNX Runtime that ships with WinML, along with an ONNX model we have, and using WinML to dynamically download the EPs for the device.

<img width="1412" height="961" alt="image" src="https://github.com/user-attachments/assets/18c8ff9f-82bb-41c1-8b12-14c3f5a49af3" />

## Step 1: Open the solution

Double click the WinMLLabDemo.sln file in the root directory to open the solution.

<img width="158" height="73" alt="image" src="https://github.com/user-attachments/assets/b2b1787e-e13d-4048-8fe5-0e761ae5e978" />

## Step 2: Deploy the app

Click the Start Debugging button to deploy the app. We'll keep it open while we edit, and see changes appear live!

<img width="269" height="39" alt="image" src="https://github.com/user-attachments/assets/56aeb74f-8efc-420b-9753-3f4a83a041f9" />

The app should look like this when it launches.

<img width="400" alt="image" src="https://github.com/user-attachments/assets/153119fc-1faf-4a61-91b1-7655b34a4963" />

## Step 3: Initialize WinML EPs

Switch back to the app and click the **Initialize WinML EPs** button, which will call WinML API! You should see either QNNExecutionProvider or OpenVINOExecutionProvider in the list.

<img width="359" height="116" alt="image" src="https://github.com/user-attachments/assets/7c6d7342-d261-4ed0-8683-873e2cf5445c" />

Or

<img width="702" height="180" alt="image" src="https://github.com/user-attachments/assets/1b86b9c7-91e0-4fc1-b235-e387354b07a3" />

