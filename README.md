# DotNet10Base

A reusable **.NET 10 Web API boilerplate** designed to provide a structured foundation for building maintainable backend applications.

DotNet10Base separates application logic, domain models, infrastructure, persistence, and API presentation into dedicated projects, making it easier to organize code and extend the solution as a project grows.

## ✨ Features

- **.NET 10**: Built on the latest .NET major release.
- **Layered architecture**: Separates core business logic from infrastructure and presentation.
- **Entity Framework Core**: Provides ORM and database access capabilities.
- **MySQL support**: Uses an Entity Framework Core MySQL provider.
- **Swagger / OpenAPI**: Supports API documentation and endpoint exploration.
- **Separate API projects**: Keeps administrative and frontend endpoints independent.
- **Shared components**: Centralizes common functionality in a dedicated project.
- **Static file project**: Provides a separate location for static file functionality.

## 🏗️ Project Structure

```text
DotNet10Base/
├── AdminWebApi/       # Administrative Web API
├── FrontendWebApi/    # Frontend-facing Web API
├── Application/       # Application logic and use cases
├── Domain/            # Core domain models and rules
├── Infrastructure/    # Infrastructure implementations
├── Persistence/       # Database access and persistence
├── Common/            # Shared components and utilities
├── StaticFiles/       # Static file functionality
└── DotNet10.slnx      # Solution file
```

### Architecture Overview

| Project | Responsibility |
|---|---|
| `Domain` | Core business models and domain rules |
| `Application` | Application-level logic and use cases |
| `Common` | Shared types and reusable utilities |
| `Infrastructure` | External service and infrastructure integrations |
| `Persistence` | Database configuration and persistence-related components |
| `AdminWebApi` | API endpoints for administrative operations |
| `FrontendWebApi` | API endpoints intended for frontend applications |
| `StaticFiles` | Static file-related functionality |

## 🛠️ Tech Stack

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- MySQL
- Swagger / OpenAPI

## 🚀 Getting Started

### Prerequisites

Make sure you have the following installed:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A MySQL-compatible database server
- An IDE such as [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)

### 1. Clone the repository

```bash
git clone https://github.com/SeyyedAliKamel/DotNet10Base.git
cd DotNet10Base
```

### 2. Configure the application

Configure the database connection and other required application settings using the configuration files or environment variables supported by the relevant API projects.

**Important:** Use your own database credentials and environment-specific settings. Do not commit passwords, API keys, or other secrets to source control.

### 3. Restore dependencies

```bash
dotnet restore DotNet10.slnx
```

### 4. Build the solution

```bash
dotnet build DotNet10.slnx
```

### 5. Run an API project

For the administrative API:

```bash
dotnet run --project AdminWebApi
```

For the frontend API:

```bash
dotnet run --project FrontendWebApi
```

Check the console output for the actual listening addresses and ports.

## 📖 API Documentation

The API projects include the Swashbuckle package for Swagger / OpenAPI integration.

When Swagger UI is enabled in the relevant project's configuration, open the Swagger endpoint provided by that application to explore its available endpoints and test requests.

## 🧩 Extending the Template

DotNet10Base can serve as a starting point for new backend projects.

A typical extension workflow is:

1. Define domain models and business rules in `Domain`.
2. Implement application-level operations in `Application`.
3. Add database and external service integrations in `Persistence` and `Infrastructure`.
4. Expose the required endpoints through the appropriate Web API project.
5. Place genuinely reusable functionality in `Common`.

Keep business rules independent of presentation and infrastructure concerns wherever practical.

## 🧩 TODO

There are several areas where contributions can help improve this project:

- **Naming consistency:** Standardize naming conventions across the codebase.
- **Code cleanup:** Remove unused code and redundant implementations.
- **Integration:** Improve integration between methods, helpers, and CRUD operations.
- **Common features:** Add reusable features such as user activity and statistics logging.
- **SEO:** Improve search engine optimization where applicable.
- **Developer experience:** Simplify setup, configuration, and development workflows.
- **Customization:** Expand personalization and configuration options.

## 🤝 Contributing

Contributions, suggestions, and bug reports are welcome.

1. Fork the repository.
2. Create a feature branch.
3. Commit your changes.
4. Open a pull request describing the changes.

## 📄 License

This project is licensed under the MIT License.

## 👤 Author

**Seyyed Ali Kamel**

- GitHub: [@SeyyedAliKamel](https://github.com/SeyyedAliKamel)
- Repository: [DotNet10Base](https://github.com/SeyyedAliKamel/DotNet10Base)
