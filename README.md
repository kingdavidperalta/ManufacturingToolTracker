# Manufacturing Tool Tracker

A web API built with C#, ASP.NET Core Web API, Entity Framework Core, and SQL Server to manage manufacturing tool inventory and track tool movements such as checkout, return, damage, and repair.

The project demonstrates practical backend development concepts, including RESTful API design, input validation, inventory business rules, automated testing with xUnit, optimistic concurrency control, and continuous integration using GitHub Actions.

## Table of Contents

- [About the Project](#about-the-project)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [System Workflow](#system-workflow)
- [Tool Movement Flowchart](#tool-movement-flowchart)
- [Database Models](#database-models)
- [Entity Relationship](#entity-relationship)
- [Business Rules](#business-rules)
- [API Endpoints](#api-endpoints)
- [Requirements](#requirements)
- [Getting Started - How to Run](#getting-started)
- [Running the Tests](#running-the-tests)
- [Concurrency Handling](#concurrency-handling)
- [Continuous Integration](#continuous-integration)
- [Screenshots](#screenshots)
- [Project Structure](#project-structure)
- [Future Improvements](#future-improvements)
- [License](#license)

## About the Project

Manufacturing environments use tools and equipment that need to be tracked to maintain inventory accuracy and accountability.

The Manufacturing Tool Tracker provides an API for recording tool movements and maintaining available quantities. Each movement records an action, quantity, the person who performed it, remarks, and the date and time of the transaction.

Instead of simply changing a tool's quantity, the application records movement history so that inventory changes can be reviewed later.

The project also addresses concurrency issues that can occur when multiple users attempt to update the same tool at nearly the same time.

## Features

- Tool inventory tracking
- Checkout, return, damage, and repair movements
- Input validation and inventory business rules
- Movement history retrieval
- SQL Server database persistence
- Optimistic concurrency control using SQL Server `rowversion`
- Automated tests using xUnit
- Continuous integration using GitHub Actions
- API exploration using Swagger / OpenAPI, when enabled

## Technology Stack

| Technology | Purpose |
|---|---|
| C# | Application programming language |
| .NET 10 | Application runtime and development platform |
| ASP.NET Core Web API | HTTP endpoints and request handling |
| Entity Framework Core | Database access and entity persistence |
| SQL Server | Relational database |
| xUnit | Automated testing framework |
| Git and GitHub | Source control and repository hosting |
| GitHub Actions | Automated build and test workflow |
| Swagger / OpenAPI | API exploration and documentation, when enabled |

## System Workflow

The application follows a request-to-database workflow.

```mermaid
flowchart TD
    A[Client or Swagger UI] --> B[ASP.NET Core Web API]
    B --> C[ToolMovementsController]
    C --> D[Validate Request]
    D --> E{Request Valid?}
    E -- No --> F[Return Error Response]
    E -- Yes --> G[Load Tool and Check Business Rules]
    G --> H{Movement Allowed?}
    H -- No --> F
    H -- Yes --> I[Update Tool and Create Movement]
    I --> J[Entity Framework Core]
    J --> K[(SQL Server)]
    K --> L[Return HTTP Response]
```

### Tool Movement Flowchart

The flowchart below shows how a movement request is validated and applied for each action (Checkout, Return, Damage, and Repair).

![Tool movement flowchart showing validation and the Checkout, Return, Damage, and Repair paths](ManufacturingToolTracker.Api/docs/images/flowchart/tool-movement-flowchart.png)

## Database Models

The application uses Entity Framework Core to work with its database entities.

### Tool

The `Tool` entity represents a tool tracked by the application.

| Property | Description |
|---|---|
| `Id` (PK) | Unique identifier for the tool. |
| `ToolCode` | Code used to identify the tool. |
| `Name` | Name or description of the tool. |
| `Quantity` | Current available quantity. |
| `Status` | Current status recorded for the tool. |
| `Version` | SQL Server rowversion used for concurrency detection. |

The `Tool` entity maintains tool information and the available quantity used when validating movement requests.

### ToolMovement

The `ToolMovement` entity represents a recorded action involving a tool.

| Property | Description |
|---|---|
| `Id` (PK) | Unique identifier for the movement record. |
| `ToolId` (FK) | Identifies the tool associated with the movement. References `Tool.Id`. |
| `Action` | Type of movement, such as Checkout, Return, Damage, or Repair. |
| `Quantity` | Quantity involved in the movement. |
| `PerformedBy` | Person recorded as performing the movement. |
| `Remarks` | Additional information about the movement, when provided. |
| `CreatedAt` | Date and time the movement was recorded. |

The `ToolMovement` entity maintains a history of inventory changes rather than relying only on the current quantity.

### Entity Relationship

![Entity relationship diagram: Tool.Id (PK) connects to ToolMovement.ToolId (FK)](ManufacturingToolTracker.Api/docs/images/entity-relationship/entity-relationship.png)

A tool can have multiple movement records. Each movement belongs to a specific tool through `ToolId`, which references `Tool.Id`.

## Business Rules

Each action follows the same pattern: validate the request, confirm the tool exists, check the action's quantity rule, then update the tool and record the movement.


### General Validation

The application rejects invalid action names and non-positive quantities. It also validates quantity limits and protects against integer overflow when increasing inventory.

A request that violates a business rule should not create a successful movement or leave inventory partially updated.

## API Endpoints

![Swagger UI showing the ToolMovements and Tools endpoints](ManufacturingToolTracker.Api/docs/images/api-endpoints.png)

### HTTP Response Codes

| Status Code | Meaning |
|---|---|
| `200 OK` | The request succeeded and data was retrieved. |
| `201 Created` | A resource or movement was created successfully. |
| `400 Bad Request` | The request is invalid or violates a business rule. |
| `404 Not Found` | The requested tool does not exist. |
| `409 Conflict` | A concurrent update was detected and the operation could not be completed. |

### Tools

Manage the tools tracked by the application.

#### GET All Tools

`GET /api/Tools` retrieves all tools.

![GET all tools](ManufacturingToolTracker.Api/docs/images/tools/get-api-tools.png)

#### POST Create Tool

`POST /api/Tools` creates a tool.

**Success**

![POST create tool](ManufacturingToolTracker.Api/docs/images/tools/post-api-tools.png)

**409 Conflict**

![POST create tool returning 409 Conflict](ManufacturingToolTracker.Api/docs/images/tools/post-api-tools-conflict.png)

#### GET Tool by ID

`GET /api/Tools/{id}` retrieves a single tool.

**Success**

![GET tool by ID](ManufacturingToolTracker.Api/docs/images/tools/get-api-tools-id.png)

**404 Not Found**

![GET tool by ID returning 404 Not Found](ManufacturingToolTracker.Api/docs/images/tools/get-api-tools-id-not-found.png)

#### PUT Update Tool

`PUT /api/Tools/{id}` updates a tool.

**Success**

![PUT update tool](ManufacturingToolTracker.Api/docs/images/tools/put-api-tools-id.png)

**409 Conflict**

![PUT update tool returning 409 Conflict](ManufacturingToolTracker.Api/docs/images/tools/put-api-tools-id-conflict.png)

#### DELETE Tool

`DELETE /api/Tools/{id}` deletes a tool.

**Success**

![DELETE tool](ManufacturingToolTracker.Api/docs/images/tools/delete-api-tools-id.png)

**404 Not Found**

![DELETE tool returning 404 Not Found](ManufacturingToolTracker.Api/docs/images/tools/delete-api-tools-id-not-found.png)

### Tool Movements

Record and review the movement history of a tool.

#### GET Movement History

`GET /api/Tools/{toolId}/Movements`

Retrieves movement history for the specified tool. The controller checks whether the tool exists and returns its recorded movements ordered by creation time, newest first.

**200 OK**: the tool exists. A tool with no recorded movements returns an empty array.

![GET movement history returning 200 with an empty array](ManufacturingToolTracker.Api/docs/images/tool-movements/get-api-tools-id-movements.png)

**404 Not Found**: the tool does not exist.

![GET movement history returning 404 Tool not found](ManufacturingToolTracker.Api/docs/images/tool-movements/get-api-tools-id-movements-not-found.png)

#### POST Create Movement

`POST /api/Tools/{toolId}/Movements`

Creates a movement after validating the request and applying the corresponding business rules.

Example request body:

```json
{
  "action": "Checkout",
  "quantity": 2,
  "performedBy": "John Doe",
  "remarks": "Issued for production use"
}
```

Use an existing tool ID and ensure the requested quantity is valid for the action.

##### Checkout

**201 Created**

![POST checkout returning 201 Created](ManufacturingToolTracker.Api/docs/images/tool-movements/post-api-tools-id-movements-checkout.png)

**400 Bad Request**: `Insufficient available quantity.`

![POST checkout returning 400 Bad Request](ManufacturingToolTracker.Api/docs/images/tool-movements/post-api-tools-id-movements-checkout-bad-request.png)

##### Return

**201 Created**

![POST return returning 201 Created](ManufacturingToolTracker.Api/docs/images/tool-movements/post-api-tools-id-movements-return.png)

**400 Bad Request**: `Return quantity cannot exceed the quantity not yet returned.`

![POST return returning 400 Bad Request](ManufacturingToolTracker.Api/docs/images/tool-movements/post-api-tools-id-movements-return-bad-request.png)

##### Damage

**201 Created**

![POST damage returning 201 Created](ManufacturingToolTracker.Api/docs/images/tool-movements/post-api-tools-id-movements-damage.png)

**400 Bad Request**: `Insufficient available quantity.`

![POST damage returning 400 Bad Request](ManufacturingToolTracker.Api/docs/images/tool-movements/post-api-tools-id-movements-damage-bad-request.png)

##### Repair

**201 Created**

![POST repair returning 201 Created](ManufacturingToolTracker.Api/docs/images/tool-movements/post-api-tools-id-movements-repair.png)

**400 Bad Request**: `Repair quantity cannot exceed the quantity damaged and not yet repaired.`

![POST repair returning 400 Bad Request](ManufacturingToolTracker.Api/docs/images/tool-movements/post-api-tools-id-movements-repair-bad-request.png)

## Requirements

- Visual Studio with .NET 10 development support
- .NET 10 SDK
- SQL Server
- Git
- A web browser for Swagger UI, when enabled
- Database permissions required to create or update the development database

## Getting Started

### 1. Clone the Repository

Choose one of the options below.

#### Option A: Visual Studio

1. Open Visual Studio and, on the start window, click **Clone a repository**.

   ![Visual Studio start window with Clone a repository highlighted](ManufacturingToolTracker.Api/docs/images/clone-repo/clone-repository-1.png)

2. Enter the repository URL `https://github.com/kingdavidperalta/ManufacturingToolTracker.git`, choose a local path, and click **Clone**.

   ![Clone a repository dialog](ManufacturingToolTracker.Api/docs/images/clone-repo/clone-repository-2.png)

3. When the clone finishes, open `ManufacturingToolTracker.slnx` in Visual Studio.

#### Option B: Command Line

```bash
git clone https://github.com/kingdavidperalta/ManufacturingToolTracker.git
```

Then open `ManufacturingToolTracker.slnx` in Visual Studio.

### 2. Configure the Database

Ensure SQL Server is running and accessible.

Configure the API connection string in `appsettings.json` to match your SQL Server instance.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ManufacturingToolTrackerDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

The connection string name must match the one used by the API's database configuration. The test project uses a separate `TestConnection` connection string.

Do not commit production passwords or other sensitive credentials to source control. Use user secrets or environment variables for sensitive configuration.

### 3. Apply Database Migrations

Apply the Entity Framework Core migrations for the API project to create or update the development database.

This can be done through Visual Studio's Package Manager Console or another supported EF Core migration workflow.

Ensure the correct database connection is selected before applying migrations.

### 4. Run the API

1. Open the solution in Visual Studio.
2. Set `ManufacturingToolTracker.Api` as the startup project.
3. Select the appropriate launch profile.
4. Start the application.
5. Open the Swagger URL displayed by the application if Swagger is enabled.

Use Swagger UI to inspect the endpoints and send test requests.

## Running the Tests

The solution includes an xUnit test project named `ManufacturingToolTracker.Tests`.

The tests exercise controller behavior and business rules using SQL Server. They are not all HTTP-level integration tests.

### Test Coverage

The test suite covers scenarios including:

- Database connectivity and migration setup
- Successful checkout and stock reduction
- Checkout requests exceeding available stock
- Zero and negative checkout quantities
- Successful returns and stock increases
- Returns exceeding the outstanding checked-out quantity
- Damage requests exceeding available stock
- Successful damage movements
- Successful repairs and stock increases
- Repairs exceeding the outstanding damaged quantity
- Requests for nonexistent tools
- Invalid movement actions
- Concurrency conflict handling

### Run Tests in Visual Studio

1. Open the solution.
2. Build the solution.
3. Open **Test Explorer** from the Visual Studio **Test** menu.
4. Select **Run All Tests**.
5. Review the results in Test Explorer.

Ensure that the test project's connection string points to a dedicated test database. The test helper applies migrations, so the configured SQL Server account must have the necessary permissions.

Do not point automated tests at a production database. Tests may create, modify, and remove records.

## Concurrency Handling

The application uses optimistic concurrency control to help prevent one user's changes from silently overwriting another user's update.

### The Problem

Imagine that a tool has 10 units available.

1. Employee A loads the tool and sees 10 units.
2. Employee B also loads the tool and sees 10 units.
3. Employee A checks out 3 units, leaving 7.
4. Employee B attempts to save a change based on the old version of the tool.

Without appropriate concurrency handling, Employee B's update could overwrite Employee A's change.

### The Solution: SQL Server Rowversion

The `Tool` entity includes a `Version` property configured as a SQL Server rowversion concurrency token.

When Entity Framework Core updates the tool, it checks whether the database row still has the version originally loaded by the application.

If another update has changed that row, the save can fail with `DbUpdateConcurrencyException`.

The controller catches this exception and returns `409 Conflict`, allowing the client to retrieve the latest state before retrying.

```mermaid
sequenceDiagram
    participant A as Employee A
    participant B as Employee B
    participant API as Web API
    participant DB as SQL Server

    A->>API: Load tool
    B->>API: Load tool
    API->>DB: Read quantity and version
    DB-->>API: Return current state
    API-->>A: Original tool state
    API-->>B: Original tool state
    A->>API: Submit movement
    API->>DB: Save using original version
    DB-->>API: Update succeeds
    B->>API: Submit movement using stale version
    API->>DB: Save using old version
    DB-->>API: Version mismatch
    API-->>B: 409 Conflict
```

Concurrency handling does not replace business-rule validation. The application must still verify stock quantities and outstanding movements before applying a change.

## Continuous Integration

GitHub Actions automatically builds the solution and runs the tests when changes are pushed to `master` or when a pull request targets `master`.

The workflow:

1. Checks out the repository.
2. Sets up the .NET 10 SDK.
3. Starts a SQL Server service container.
4. Restores project dependencies.
5. Builds the solution in Release configuration.
6. Runs the test suite.

The workflow uses a GitHub Actions repository secret named `MSSQL_SA_PASSWORD` for the SQL Server service password. The secret is stored in GitHub repository settings rather than written directly into the workflow.

The SQL Server container runs in GitHub's CI environment. Docker does not need to be installed locally to use this workflow.

### View Workflow Results

1. Open the repository on GitHub.
2. Select the **Actions** tab.
3. Open the latest `.NET Build and Test` workflow run.
4. Review the build and test results.

## Screenshots

### Swagger UI


## Project Structure

```text
ManufacturingToolTracker/
├── ManufacturingToolTracker.slnx
├── ManufacturingToolTracker.Api/
│   ├── Controllers/
│   │   └── ToolMovementsController.cs
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── Models/
│   │   ├── Tool.cs
│   │   ├── ToolMovement.cs
│   │   └── ToolMovementActions.cs
│   ├── DTOs/
│   │   ├── CreateToolMovementDto.cs
│   │   └── ToolMovementResponseDto.cs
│   ├── Migrations/
│   ├── appsettings.json
│   └── Program.cs
├── ManufacturingToolTracker.Tests/
│   ├── ToolMovementTests.cs
│   └── appsettings.json
└── .github/
    └── workflows/
        └── dotnet.yml
```

## Future Improvements

- Add authentication and authorization to identify users and control access.
- Add pagination and filtering to movement history.
- Add structured logging and centralized error handling.
- Add HTTP-level integration tests for the API.
- Add audit details for movement accountability.

## License

No license has been specified for this project.