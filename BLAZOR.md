# QLN Project – Development & API Standards

---

## 1. Main Development Branch

All updated development code resides in the main branch:

- **Branch Name:** `qln-dev`

---

## 2. Branch Naming Convention

Branches should follow this naming pattern:

```
<user-name>/<module-name>
```

**Examples:**
- `petchiappan/services`
- `grant/mock_testing`
- `Kishore/classified`
- `Koushiki/company`

---

## 3. Pull Request (PR) Guidelines

- **Target Branch:** `qln-dev`
- **Reviewer:** Grant Vine
- **Process:**
    - Develop and push feature branches following the naming convention.
    - Create a PR targeting the `qln-dev` branch.
    - Assign Grant Vine as the reviewer.
    - Merge occurs only after review and approval.

---

## Exception Handling

- All exceptions logged using a centralized `EventLogger`.
- Errors and exceptions consistently return structured `ProblemDetails` objects.

---

## Security and Authentication

- All API endpoints secured with JWT authentication and role-based authorization.
- Sensitive endpoints protected explicitly using `.RequireAuthorization()`.

---

## Dapr Commands

### 1. Run a Single Service Locally with Dapr Sidecar

```
dapr run --app-id <app-name> --app-port <your-app-port> dotnet run
```

**Example:**

```
dapr run --app-id qln-backend-api --app-port 5000 dotnet run
```
- `--app-id` – Unique name for your app (used for service discovery)
- `--app-port` – Your application’s listening port

### 2. Run a Service With Specified Dapr Components

```
dapr run --app-id <app-name> --app-port <your-app-port> --components-path ./components dotnet run
```
- If you have a custom components folder, specify the path with `--components-path`.

---

# API RESPONSE

## 1. Standard Response Structure

### Success Response
- Return only the relevant data (DTO or entity).
- Don’t wrap in a custom object unless required by frontend (like `{ success, data, message }`).
- **Example:**
    ```
    return TypedResults.Ok(companyProfileEntity);
    ```

### Error Response
- Use `ProblemDetails` (standard error object as per RFC 7807, supported by Swagger).
- Status codes must match error type:
    - 400 – BadRequest (validation, bad input)
    - 404 – NotFound (resource missing)
    - 409 – Conflict (duplicate or business rule issue)
    - 500 – InternalServerError (unexpected errors)
- **Example:**
    ```
    return TypedResults.Problem(
        title: "Internal Server Error",
        detail: "An unexpected error occurred.",
        statusCode: StatusCodes.Status500InternalServerError
    );
    ```

## 2. Minimal API Response Patterns

- **For Success:**
    ```
    return TypedResults.Ok(result); // result is your DTO/entity/list
    ```
- **For Validation Errors:**
    ```
    return TypedResults.BadRequest(new ProblemDetails {
        Title = "Invalid Data",
        Detail = "Phone number is required.",
        Status = StatusCodes.Status400BadRequest
    });
    ```
- **For Not Found:**
    ```
    return TypedResults.NotFound(new ProblemDetails {
        Title = "Not Found",
        Detail = $"No company found for id {id}",
        Status = StatusCodes.Status404NotFound
    });
    ```
- **For General/Internal Errors:**
    ```
    return TypedResults.Problem(
        title: "Internal Server Error",
        detail: "An unexpected error occurred.",
        statusCode: StatusCodes.Status500InternalServerError
    );
    ```

## 3. Exception Handling in Endpoints

- Wrap all logic in try-catch inside endpoint methods.
- Catch specific exceptions (like `KeyNotFoundException`, `InvalidDataException`).
- For unhandled exceptions, catch general `Exception` and log it.
- Log errors using your `EventLogger` service.

**Example:**
```
try
{
    var result = await service.GetCompanyById(id);
    if (result is null)
        throw new KeyNotFoundException($"Company with id '{id}' was not found.");

    return TypedResults.Ok(result);
}
catch (KeyNotFoundException ex)
{
    return TypedResults.NotFound(new ProblemDetails {
        Title = "Not Found",
        Detail = ex.Message,
        Status = StatusCodes.Status404NotFound
    });
}
catch (Exception ex)
{
    logger.LogError(ex, "Error in GetCompanyById");
    return TypedResults.Problem(
        title: "Internal Server Error",
        detail: "An unexpected error occurred.",
        statusCode: StatusCodes.Status500InternalServerError
    );
}
```

## 4. Swagger Documentation Integration

- Use `.Produces<T>()` for every endpoint to show possible responses in Swagger.
- **Example:**
    ```
    .Produces<CompanyProfileEntity>(StatusCodes.Status200OK)
    .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
    .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
    .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
    ```

## 5. Don’t Return Internal Details

- Never leak stack trace or internal exception details in production.
- Only return user-friendly messages via `ProblemDetails.Detail`.

---

## Sample Full Endpoint Pattern

```
group.MapPost("/create", async Task<Results<
    Ok<string>,
    BadRequest<ProblemDetails>,
    ProblemHttpResult>>
(
    [FromForm] CompanyProfileDto dto,
    [FromServices] ICompanyService service,
    CancellationToken cancellationToken = default) =>
{
    try
    {
        var entity = await service.CreateCompany(dto, cancellationToken);
        return TypedResults.Ok("Company Profile created successfully");
    }
    catch (InvalidDataException ex)
    {
        return TypedResults.BadRequest(new ProblemDetails {
            Title = "Invalid Data",
            Detail = ex.Message,
            Status = StatusCodes.Status400BadRequest
        });
    }
    catch (Exception)
    {
        return TypedResults.Problem(
               title: "Internal Server Error",
               detail: "An unexpected error occurred.",
               statusCode: StatusCodes.Status500InternalServerError
        );
    }
})
.WithName("CreateCompanyProfile")
.Produces<string>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
```

---

# Running Dapr-Enabled Microservices

- The `app-id` and `app-port` parameters for each Dapr-enabled microservice are specified within the `dapr.yaml` configuration file, located in each project’s directory. These values are not displayed directly in the source code.
- To verify or update these parameters, open the respective project’s directory and locate the `dapr.yaml` file. All Dapr-related configurations, including `app-id` and `app-port`, can be found and edited there.

## Example: Checking/Editing dapr.yaml

- For example, to check the `app-id` and `app-port` for QLN.Classified.MS, navigate to its project folder, open `dapr.yaml`, and review/modify the configuration as needed. These settings govern how Dapr identifies and communicates with your microservice during local or production runs.

## Where to Find app-id and app-port

- `app-id` and `app-port` for each microservice are configured in the `dapr.yaml` file, not in the application source code.
- To view or update:
    1. Open the relevant project folder.
    2. Locate `dapr.yaml`.
    3. Open and edit as needed.

---

## Example Commands to Run Each Project (Replace Resource Path as Needed)

Below are the commands to start each microservice with Dapr. Replace the paths as per your local solution structure.

### QLN.Classified.MS
```
dapr run --app-id qln-classified-ms --app-port 5276 --dapr-http-port 3600 --dapr-grpc-port 50003 --resources-path "C:\Users\Mujay.a\source\repos\QLN-V2\components" -- dotnet run --project QLN.Classified.MS
```

### QLN.Company.MS
```
dapr run --app-id qln-company-ms --app-port 5109 --dapr-http-port 3500 --dapr-grpc-port 5003 --resources-path "./components" -- dotnet run --project QLN.Company.MS
```

### QLN.Subscriptions.Actor
```
dapr run --app-id subscription-actor --app-port 5056 --dapr-http-port 3500 -- dotnet run --project "C:\Users\madhumitha.d.KRYPTOS\source\repos\subscriptionactor\QLN.Subscriptions.Actor"
```

Starting Dapr with id subscription-actor. HTTP Port: 3500. gRPC Port: 53108

---
