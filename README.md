### Basecamp Project Management System

### Description
This project is a project management and collaboration tool. It allows users to register, log in, create new projects, and share them with friends. Key features include a discussion section for team communication and full CRUD (Create, Read, Update, Delete) functionality for managing project tasks.
### Link
```
https://basecamp2anar-production.up.railway.app/
```
### Features
Authentication: Secure Login and Registration system.

Project Management: Create and share projects with others.

Discussions: Real-time messaging/discussion boards within projects.

Task Management: Add, and delete tasks within specific projects.

### Installation & Setup

To get the project running locally, follow these steps:

1. Clone the Repository
Clone the project to your local machine and open the directory:

```
git clone <repository-link>
```

2. Open the Solution
Open the "Basecamp-Backend.slnx".

3. Install Required Packages
Ensure the following NuGet packages are installed for the backend to function correctly:

Microsoft.AspNetCore.Identity.EntityFrameworkCore

Microsoft.EntityFrameworkCore

Microsoft.EntityFrameworkCore.Tools

Npgsql.EntityFrameworkCore.PostgreSQL

Npgsql.EntityFrameworkCore.PostgreSQL.Design

4. Database Configuration
Update the connection string in your appsettings.json file to match your local PostgreSQL credentials:

```
"ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=BaseCampDB;Username=postgres;Password=yourpassword;Include Error Detail=true"
},
```

5. Database Migrations
To create the necessary tables in your database:

Go to Tools -> NuGet Package Manager -> Package Manager Console.

In the terminal that opens, run the following command:

```
Update-Database
```

### The Core Team

Anar Mirzayev

<span><i>Made at <a href="https://qwasar.io">Qwasar SV -- Software Engineering School</a></i></span>
<img src="https://storage.googleapis.com/qwasar-public/qwasar-logo_50x50.png" width="20px" alt="Qwasar SV -- Software Engineering School's Logo" />
