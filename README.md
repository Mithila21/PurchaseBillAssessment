# Purchase Bill Management System

Full Stack Developer Assessment - Enhanzer

## Project Overview

A two-page Purchase Bill Management System developed using
Angular, ASP.NET Core Web API, and SQL Server.

The application authenticates users through the Enhanzer API,
stores user locations in SQL Server, and allows authenticated
users to create and save Purchase Bills.

## Technologies Used

### Frontend
- Angular 22
- TypeScript
- HTML and CSS
- Angular Reactive Forms
- Angular Signals
- Angular HttpClient

### Backend
- ASP.NET Core Web API (.NET 8)
- C#
- ADO.NET (Microsoft.Data.SqlClient)
- Cookie Authentication
- Antiforgery Token Validation

### Database
- Microsoft SQL Server 2022
- SQL Server Developer Edition for local development

## Features

### Login
- Email and password validation
- Authentication through Enhanzer's external API
- Stores returned user locations in SQL Server
- Cookie-based login session
- Protected application routes
- Logout functionality

### Purchase Bill
- Item autocomplete
- Batch dropdown populated from SQL Server
- Standard Cost and Standard Price fields
- Quantity and Discount inputs
- Automatic Total Cost and Total Selling calculations
- Add multiple items to a table
- Dynamic Item Summary
- Save Purchase Bills through ASP.NET Core Web API
- Server-side validation and calculations

## Project Structure

PurchaseBillAssessment/
  frontend/       Angular application
  backend/        ASP.NET Core Web API
  database/       SQL Server database script
  README.md       Project documentation

## Prerequisites

- Node.js compatible with Angular 22
- Angular CLI 22
- .NET 8 SDK
- SQL Server 2022 or compatible SQL Server instance
- VS Code or another suitable IDE

## Database Setup

1. Start your SQL Server instance.
2. Open database/create_database.sql.
3. Execute the script on SQL Server.
4. Verify that PurchaseBillDB has been created.

The application uses these tables:

- Location_Details
- Purchase_Bill_Items

## Backend Setup

1. Open a terminal inside the backend folder.

2. Restore dependencies:

   dotnet restore

3. Configure the DefaultConnection connection string
   in appsettings.json for your SQL Server instance.

4. Store the database password using .NET User Secrets:

   dotnet user-secrets set "Database:Password"

   If your CLI requires a value argument, provide it
   securely without committing it to Git.

5. Start the backend:

   dotnet run

Note the API address and port printed in the terminal.

## Frontend Setup

1. Open a terminal inside the frontend folder.

2. Install dependencies:

   npm install

3. Update src/proxy.conf.json so its target matches
   the running ASP.NET Core backend address.

4. Start Angular:

   npm start

5. Open http://localhost:4200

The Angular development proxy is configured in angular.json.

## Application Workflow

1. Open the Login page.
2. Enter valid Enhanzer authentication credentials.
3. The backend verifies the credentials through the external API.
4. Returned user locations are saved in SQL Server.
5. Navigate to the protected Purchase Bill page.
6. Select an item and a batch.
7. Enter cost, price, quantity, and discount.
8. Click Add Item to update the items table and summary.
9. Click Save Bill to submit the bill to the backend.
10. The backend validates and saves the bill in SQL Server.
11. Click Logout to end the session.

## API Endpoints

POST /api/auth/login       Authenticate user
GET  /api/auth/me          Check current session
POST /api/auth/logout      Logout
GET  /api/auth/csrf        Obtain antiforgery token
GET  /api/locations        Retrieve saved locations
POST /api/purchasebills    Save Purchase Bill

## Security

- Authentication cookies are HttpOnly.
- Protected API endpoints require authentication.
- CSRF protection is used for state-changing requests.
- SQL operations use parameterized queries.
- Database passwords are excluded from source control.

The HTTP development environment is intended for local
testing. Production deployment requires HTTPS and
appropriate production configuration.

## Notes

- Login requires valid credentials for the Enhanzer staging API.
- The external authentication API must be accessible.
- On Apple Silicon Macs, the SQL Server Docker image
  requires x64 emulation, which is not officially supported.
  A supported SQL Server instance is recommended for evaluation.
- Database passwords and authentication credentials are
  not included in this repository.
