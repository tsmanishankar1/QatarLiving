 ## Setting Up VS Code on macOS for MAUI Workload
 
 This guide explains how to configure Visual Studio Code (VS Code) on macOS to run a .NET MAUI (Multi-platform App UI) workload. 
 It assumes that you have followed the tool installation guide in the existing [`README.md`](README.md) file.
 
 ### Prerequisites
 - macOS with a supported version for .NET MAUI development.
 - VS Code installed on your system.
 - .NET SDK installed (ensure it supports MAUI).
 - Access to the [`README.md`](README.md) file for tool installation instructions.
 
 ### Steps to Set Up
 
 1. **Install Required Tools**:
    - **Android Studio*(if not already installed):
      - Download and install Android Studio from the [official website](https://developer.android.com/studio).
      - Open Android Studio and install the required Android SDKs and tools.
      - Ensure that the Android Emulator is set up and functional.
    - **Xcode*(if not already installed):
      - Install Xcode from the Mac App Store.
      - Open Xcode and agree to the license agreement.
      - Install the required command-line tools by running `xcode-select --install` in the terminal.
 
 2. **Configure VS Code**:
    - Install the following VS Code extensions specifically designed for .NET MAUI development:
        - [MAUI](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui) for comprehensive MAUI support.

    - Ensure that the extensions are properly installed and activated by checking the Extensions view in VS Code.
    - Restart VS Code after installation to ensure all extensions are loaded correctly.
 
 3. **Verify Toolchain**:
    - Open a terminal and run the following commands to verify the installation:
      - `dotnet --list-sdks` to confirm the .NET SDK is installed.
      - `xcode-select -p` to confirm Xcode is installed and selected.
      - `adb devices` to confirm Android tools are installed and working.
 
4. **Set Up Existing MAUI Project**:
    - Navigate to the existing MAUI project folder:

      ```bash
      cd QLN.MAUI.App
      ```

    - Restore the required .NET workloads for the project **(elevated privileges are required)**:

      ```bash
      sudo dotnet workload restore
      ```

    - Open the project folder in VS Code:

      ```bash
      code .
      ```

     - Ensure all dependencies are restored and the project builds successfully by running the following commands for each platform **(NOTE: builds for iOS will fail if you are not on the latest MacOS version)**:

        ```bash
        # Restore dependencies
        dotnet restore

        # Build for Android
        dotnet build -f net9.0-android

        # Build for iOS (see NOTE above)
        dotnet build -f net9.0-ios
        ```

     - If you encounter build issues, verify that all required workloads are installed by running:

        ```bash
        dotnet workload list
        ```

     - Address any missing workloads by installing them:

        ```bash
        dotnet workload install <workload-id>
        ```

    Replace `<workload-id>` with the appropriate workload, such as `android` or `ios`, based on the error message.
 
 5. **Run and Debug**:
    - Use the VS Code debugger to run the application on an Android Emulator or iOS Simulator:
      - For Android: Ensure the emulator is running and select it as the target device.
      - For iOS: Ensure the iOS Simulator is running and select it as the target device.
    - Use the `Run and Debug` panel in VS Code to start the application.
 
 ### Notes
 - Ensure that all dependencies are installed as per the [`README.md`](README.md) file.
 - For iOS development, you must have a valid Apple Developer account to deploy apps to physical devices.
 - For Android development, ensure that the Android Emulator is configured with a compatible API level.
 