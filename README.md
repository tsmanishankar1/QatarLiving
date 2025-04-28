# QLN-V2 Project

This repository contains a Blazor-based frontend and a backend API built using .NET Minimal APIs. The project is structured as a .NET solution and is designed to run on macOS with Visual Studio Code.

## Prerequisites

Before you begin, ensure you have the following installed on your MacBook:

1. **.NET SDK 8.0**  
   Download and install the latest .NET SDK from [Microsoft's .NET website](https://dotnet.microsoft.com/download).

2. **Visual Studio Code**  
   Download and install Visual Studio Code from [here](https://code.visualstudio.com/).

3. **Docker**  
   Install Docker Desktop for macOS from [Docker's website](https://www.docker.com/products/docker-desktop).

## Required VS Code Extensions

Install the following extensions in Visual Studio Code:

1. **C# Dev Kit** (Required)  
   Provides an enhanced development experience for C# projects, including Blazor and .NET APIs.  
   [C# Dev Kit Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)

2. **Azure Extension Pack** (Optional for deployments)  
   Includes tools for managing Azure resources and deployments.  
   [Azure Extension Pack](https://marketplace.visualstudio.com/items?itemName=ms-vscode.vscode-node-azure-pack)

3. **Docker**  
   For working with Docker containers.  
   [Docker Extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker)

4. **Dapr** (Optional for future use)  
   Provides support for Dapr development and debugging.  
   [Dapr Extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-dapr)

5. **PostgreSQL** (Optional for database management)  
   For managing PostgreSQL databases.  
   [PostgreSQL Extension](https://marketplace.visualstudio.com/items?itemName=ckolkman.vscode-postgres)

6. **REST Client (Optional)**  
   For testing API endpoints.  
   [REST Client Extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)

## Getting Started

1. **Clone the Repository**  
   ```bash
   git clone https://QLNext@dev.azure.com/QLNext/QLN-V2/_git/QLN-V2
   cd QLN-V2
   ```

2. **Restore Dependencies**  
   Run the following command to restore NuGet packages:  
   ```bash
   dotnet restore
   ```

3. **Run the Backend API**  
   Navigate to the backend API directory and run the project:  
   ```bash
   cd QLN.Backend.API
   dotnet run
   ```

4. **Run the Blazor Frontend**  
   Navigate to the Blazor project directory and run the project:  
   ```bash
   cd QLN.Blazor.Base
   dotnet run
   ```

5. **Access the Applications**  
   - Backend API: `http://localhost:5200` (or the port specified in `launchSettings.json`)
   - Blazor Frontend: `http://localhost:5047` (or the port specified in `launchSettings.json`)

## Environment Configuration

- Update the `appsettings.json` file in the `QLN.Backend.API` directory to configure the PostgreSQL database connection.

### Example PostgreSQL Connection String

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=QLNDB;Username=your_username;Password=your_password"
}
```

## Docker Support

To build and run the project using Docker, use the provided `Dockerfile` in both the `QLN.Backend.API` and `QLN.Blazor.Base` directories.

```bash
# Build and run the backend API
docker build -t qln-backend-api -f QLN.Backend.API/Dockerfile .
docker run -p 5200:8080 qln-backend-api

# Build and run the Blazor frontend
docker build -t qln-blazor-base -f QLN.Blazor.Base/Dockerfile .
docker run -p 5047:8080 qln-blazor-base
```