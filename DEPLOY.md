# Deployment Guide

This document outlines the steps to build and deploy the QLN-V2 project using Azure DevOps pipelines.

## Prerequisites

1. **Azure DevOps Account**  
   Ensure you have access to an Azure DevOps organization.

2. **Azure Resources**  
   - Azure Container Registry (ACR)
   - Azure Container Apps
   - Azure Resource Group

3. **Service Connection**  
   Create a service connection in Azure DevOps to authenticate with your Azure subscription.

## Backend API Deployment

The backend API is deployed using the [backendapi-azure-pipelines.yml](http://_vscodecontentref_/3) pipeline.

### Pipeline Configuration

1. **Trigger**  
   The pipeline triggers on changes to the `qln-dev` branch or files in the [QLN.Backend.API](http://_vscodecontentref_/4) and [QLN.Common](http://_vscodecontentref_/5) directories.

2. **Variables**  
   - `acrRegistry`: Azure Container Registry name.
   - `appName`: Name of the backend API project.
   - `appShortName`: Short name for the backend API.

3. **Steps**  
   - Build the Docker image for the backend API.
   - Push the image to Azure Container Registry.
   - Deploy the image to Azure Container Apps.

### Running the Pipeline

1. Commit changes to the `qln-dev` branch.
2. Navigate to Azure DevOps and run the pipeline manually if needed.

---

## Blazor Frontend Deployment

The Blazor frontend is deployed using the [blazorbase-azure-pipelines.yml](http://_vscodecontentref_/6) pipeline.

### Pipeline Configuration

1. **Trigger**  
   The pipeline triggers on changes to the `qln-dev` branch or files in the [QLN.Blazor.Base](http://_vscodecontentref_/7), [QLN.Common](http://_vscodecontentref_/8), and [QLN.Web.Shared](http://_vscodecontentref_/9) directories.

2. **Variables**  
   - `acrRegistry`: Azure Container Registry name.

3. **Steps**  
   - Build the Docker image for the Blazor frontend.
   - Push the image to Azure Container Registry.
   - Deploy the image to Azure Container Apps.

### Running the Pipeline

1. Commit changes to the `qln-dev` branch.
2. Navigate to Azure DevOps and run the pipeline manually if needed.

---

## Azure DevOps Pipeline Files

- **Backend API Pipeline**: [backendapi-azure-pipelines.yml](http://_vscodecontentref_/10)
- **Blazor Frontend Pipeline**: [blazorbase-azure-pipelines.yml](http://_vscodecontentref_/11)

## Notes

- Ensure the `acrUsername` and `acrPassword` variables in the pipeline files are securely stored in Azure DevOps variable groups or secrets.
- Update the `dockerfilePath` and `imageToBuild` paths in the pipeline files if the project structure changes.