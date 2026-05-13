# My Basecamp 2

## Live Demo

[https://basecamp2anar-production.up.railway.app/](https://basecamp2anar-production.up.railway.app/)

## Description

My Basecamp 2 is a project management and collaboration web application built with ASP.NET Core MVC, PostgreSQL, and ASP.NET Identity. Users can register, log in, view projects, contribute to discussions, and work with project attachments.

## Features

Authentication: Users can register, log in, log out, and view their profile page.

PostgreSQL Database: Project data, users, roles, project members, discussions, tasks, and attachments are stored persistently in PostgreSQL.

Role-Based Access Control: Admin and Member roles are managed with ASP.NET Identity tables.

Admin Dashboard: Admin users can view all registered users and promote or demote users between Admin and Member roles.

Admin Permissions: Admin users can create, update, and delete projects, tasks, members, messages, and attachments.

Member Permissions: Member users can read project data and contribute to discussions.

Attachment Management: Admin users can upload files, save file records in PostgreSQL, delete files, preview supported files, and download attachments.

Discussion Board: Project members can add messages inside project discussions.

Task Management: Admin users can create, update, complete, and delete project tasks.

Project Members: Admin users can add members to projects and manage project-level roles.

Responsive Layout: The navbar keeps DASHBOARD on the left and account links on the right. Profile and main cards are centered on the page.

## Main Routes

Dashboard:
```
GET /Dashboard/Index
```

Admin user management:
```
GET /admin/users
POST /admin/update-role/{id}
```

Project details:
```
GET /Project/Details/{id}
```

Attachment upload:
```
POST /Project/UploadAttachment
```

Attachment preview:
```
GET /Project/OpenAttachment/{id}
```

Attachment download:
```
GET /Project/DownloadAttachment/{id}
```

Attachment delete:
```
POST /Project/DeleteAttachment
```

## Installation

1. Clone the repository.

```
git clone <repository-link>
```

2. Open the solution.

```
Basecamp-Backend.slnx
```

3. Install the required NuGet packages.

```
Microsoft.AspNetCore.Identity.EntityFrameworkCore
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.Tools
Npgsql.EntityFrameworkCore.PostgreSQL
Npgsql.EntityFrameworkCore.PostgreSQL.Design
```

4. Configure PostgreSQL in `appsettings.json`.

```
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=BaseCampDB;Username=postgres;Password=yourpassword;Include Error Detail=true"
}
```

5. Apply database migrations.

```
Update-Database
```

6. Run the application.

```
dotnet run
```

## Roles

Admin:
```
Full CRUD permissions
Manage users
Promote users
Demote users
Upload attachments
Delete attachments
Delete messages
Delete projects
```

Member:
```
Read project data
View attachments
Download attachments
Contribute to discussions
```

## Team

Anar Mirzayev

<span><i>Made at <a href="https://qwasar.io">Qwasar SV -- Software Engineering School</a></i></span>
<img src="https://storage.googleapis.com/qwasar-public/qwasar-logo_50x50.png" width="20px" alt="Qwasar SV -- Software Engineering School's Logo" />
