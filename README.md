# Inventory Management in ASP.NET Core

## Overview

This sample shows how to build a product inventory web application with ASP.NET Core 3.1 and Entity Framework Core 3.1 that stores its data in SQL Server. The `ProductsController` class exposes create, read, update, and delete operations over a product catalog, and the `InventoryContext` Entity Framework Core database context persists each change. Razor views render the product listing together with the Create, Edit, Details, and Delete screens, so the sample can be used as a starting point for a server-rendered inventory application.

## Key Features

- Lists every product in a Bootstrap table on the landing route, with each row pulled from SQL Server through an Entity Framework Core `DbSet` (the table-tracked collection of products).
- Provides full create, read, update, and delete operations for product records through the `Index`, `Details`, `Create`, `Edit`, and `Delete` actions of the `ProductsController` class.
- Creates a new product through a form that posts to the `Create` action, adds it with the Entity Framework Core `Add` method, and redirects back to the product list when the submitted values are valid.
- Edits an existing product that is looked up by its primary key with the `FindAsync` method, applies the submitted changes with the `Update` method, and saves them to the database.
- Deletes a product after a confirmation screen, calls the Entity Framework Core `Remove` method on the products `DbSet`, and returns to the product list.
- Shows a read-only details page for a single product selected by its `ProductId` value, and routes every request through the default `{controller=Products}/{action=Index}/{id?}` pattern so the product listing is the landing page.

## Prerequisites

- Visual Studio 2022 or Visual Studio Code
- .NET SDK compatible with the project's target framework
- SQL Server 2017 or later / SQL Server Express / SQL Server LocalDB

## How to Run the Project

### Visual Studio 2022

1. Clone or download this repository.
2. Open the solution file:

   `InventoryManagement/InventoryManagement.sln`

3. Update the SQL Server connection string under `ConnectionStrings:InventoryDatabase` in `InventoryManagement/appsettings.json` with the server name, database name, and authentication details for a database that you are authorized to access.
4. Restore the NuGet packages by rebuilding the solution.
5. Set the startup project to:

   `InventoryManagement`

6. Build the solution.
7. Run the application using `Ctrl+F5`.
8. Open the application URL displayed by Visual Studio after launch.
9. Verify that the product listing is displayed as the landing page, and that the Create New, Edit, Details, and Delete actions are available for each row.

### Visual Studio Code

1. Clone or download this repository.
2. Open the repository folder in Visual Studio Code.
3. Open the integrated terminal.
4. Navigate to the project directory:

```bash
cd InventoryManagement
```

5. Update the SQL Server connection string under `ConnectionStrings:InventoryDatabase` in `appsettings.json` with the server name, database name, and authentication details for a database that you are authorized to access.
6. Restore the NuGet packages:

```bash
dotnet restore
```

7. Build the project:

```bash
dotnet build
```

8. Run the project:

```bash
dotnet run
```

9. Open the local URL displayed in the terminal after the application starts.
10. Verify that the product listing is displayed as the landing page, and that the Create New, Edit, Details, and Delete actions are available for each row.

## Project Structure

- `InventoryManagement/Controllers/ProductsController.cs` — controller that defines the create, read, update, and delete actions for products.
- `InventoryManagement/Controllers/HomeController.cs` — controller that serves the home, privacy, and error pages.
- `InventoryManagement/Models/Products.cs` — entity class that models a product with name, category, color, unit price, and available quantity fields.
- `InventoryManagement/Models/InventoryContext.cs` — Entity Framework Core database context that exposes the products table and configures its key, required fields, column lengths, and unit price column type.
- `InventoryManagement/Views/Products/Index.cshtml` — Razor view that renders the product listing table with create, edit, details, and delete links.
- `InventoryManagement/Views/Products/Create.cshtml` — Razor view with the form used to add a new product.
- `InventoryManagement/Views/Products/Edit.cshtml` — Razor view with the form used to update an existing product.
- `InventoryManagement/Views/Products/Details.cshtml` — Razor view that shows the read-only details of a single product.
- `InventoryManagement/Views/Products/Delete.cshtml` — Razor view that asks for confirmation before deleting a product.

## Support and Feedback

- To report an issue specific to this sample, open a GitHub issue in this repository.
- Official documentation: [ASP.NET Core documentation](https://learn.microsoft.com/aspnet/core/)
