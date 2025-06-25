# UniMart E-commerce Platform

A comprehensive e-commerce platform designed for university campuses, enabling students to buy and sell academic products with complete role-based access control.

---

## Key Features (2025 Update)
- **Three User Roles**: Admin, Merchant, and User with distinct functionalities
- **Product Management**: Complete CRUD operations with admin approval workflow
- **Product Reviews**: Rating and commenting system, including review display and star ratings
- **Localization**: English and Arabic language support, with translation files (`translations_en.json`, `language.js`)
- **Favorites & Wishlists**: Add/remove products from favorites
- **Order Management**: Cart, checkout, and order tracking
- **Admin Dashboard**: Analytics, merchant management, and system activity
- **Responsive Design**: Mobile-friendly interface using Bootstrap and custom CSS
- **Security**: Enhanced authentication and authorization

## Project Structure (Highlights)
- `Controllers/` — MVC controllers for all major features
- `Models/` — Entity and ViewModel classes
- `Views/` — Razor views for UI rendering (see `Views/Admin/`, `Views/Products/`, etc.)
- `Data/` — Entity Framework Core DbContext and migrations
- `wwwroot/` — Static files (CSS, JS, images)
- `Helpers/` — Extension methods and utilities
- `Services/` — Business logic and service classes
- `Resources/` — Localization and translation files
- `add_data_translate.py`, `extract_translations.py` — Scripts for managing translation keys in `.cshtml` files

## How to Run
1. Ensure you have .NET 8.0+ and SQL Server or SQLite installed.
2. Clone the repository and restore NuGet packages.
3. Update `appsettings.json` with your database connection string.
4. Run database migrations:
   ```powershell
   dotnet ef database update
   ```
5. Start the application:
   ```powershell
   dotnet run
   ```
6. Access the app at `https://localhost:5001` (or the configured port).

## Translation & Localization
- All user-facing text is managed via translation files (`translations_en.json`, etc.).
- Use the provided Python scripts to extract and add translation keys to `.cshtml` files.
- Front-end language switching is handled in `wwwroot/js/language.js`.

## Contribution
Pull requests are welcome! Please follow best practices for ASP.NET Core MVC and ensure all new features are covered by appropriate translations.

## License
© 2025 UniMart. All rights reserved.

---

# [Original README continues below]

## Features

- **Three User Roles**: Admin, Merchant, and User with distinct functionalities
- **Product Management**: Complete CRUD operations with admin approval workflow
- **Shopping Cart**: Full cart functionality with persistent storage
- **Product Reviews**: Rating and commenting system
- **Localization**: English and Arabic language support
- **Currency**: Egyptian Pound (EGP) display
- **Responsive Design**: Mobile-friendly interface
- **Security**: Enhanced authentication and authorization

## Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server (production)
- **ORM**: Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Frontend**: HTML5, CSS3, JavaScript, Bootstrap5
- **Localization**: ASP.NET Core Localization Services

## Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or Visual Studio Code
- SQL Server (for production) or SQLite (for development)

## Installation & Setup

### 1. Clone the Repository
```bash
git clone <repository-url>
cd UniMart-App
```

### 2. Install Dependencies
```bash
dotnet restore
```

### 3. Database Setup

#### For Development (SQLite)
The project is configured to use SQLite by default for development. The database will be created automatically when you run the application.

#### For Production (SQL Server)
1. Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=UniMart;Trusted_Connection=true;MultipleActiveResultSets=true;"
  }
}
```

2. Update `Program.cs` to use SQL Server:
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

### 4. Apply Database Migrations
```bash
dotnet ef database update
```

### 5. Run the Application
```bash
dotnet run
```

The application will be available at `https://localhost:5001` or `http://localhost:5000`.

## Default User Accounts

The application includes a database initializer that creates default accounts for testing:

### Admin Account
- **Email**: admin@unimart.com
- **Password**: Admin123!
- **Role**: Admin

### Merchant Account
- **Email**: merchant@unimart.com
- **Password**: Merchant123!
- **Role**: Merchant

### User Account
- **Email**: user@unimart.com
- **Password**: User123!
- **Role**: User

## Project Structure

```
UniMart-App/
├── Controllers/           # MVC Controllers
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── MerchantController.cs
│   ├── ProductsController.cs
│   └── ...
├── Data/                 # Database Context and Migrations
│   ├── ApplicationDbContext.cs
│   ├── DbInitializer.cs
│   └── Migrations/
├── Models/               # Data Models
│   ├── ApplicationUser.cs
│   ├── Product.cs
│   ├── Faculty.cs
│   └── ...
├── Views/                # Razor Views
│   ├── Account/
│   ├── Admin/
│   ├── Merchant/
│   ├── Products/
│   └── Shared/
├── Services/             # Business Logic Services
│   └── LocalizationService.cs
├── Resources/            # Localization Resources
│   ├── LocalizationService.en-US.resx
│   └── LocalizationService.ar-EG.resx
├── wwwroot/             # Static Files
│   ├── css/
│   ├── js/
│   └── images/
├── appsettings.json     # Configuration
└── Program.cs           # Application Entry Point
```

## Configuration

### Database Configuration
- **Development**: SQLite database (`UniMart.db`)
- **Production**: SQL Server (configure connection string)

### Authentication Settings
- **Password Requirements**: Minimum 6 characters, requires digit and non-alphanumeric
- **Cookie Settings**: Secure, HttpOnly, SameSite protection
- **Session Timeout**: 1 hour with sliding expiration

### Localization Settings
- **Supported Languages**: English (en-US), Arabic (ar-EG)
- **Default Language**: English
- **Currency**: Egyptian Pound (EGP)

## Usage

### For Admins
1. Login with admin credentials
2. Navigate to Admin Dashboard
3. Manage products (approve/reject merchant submissions)
4. Manage faculties and academic years
5. Monitor system activity

### For Merchants
1. Register as a merchant or login
2. Access Merchant Dashboard
3. Create and manage products
4. View sales analytics
5. Track product approval status

### For Users
1. Register as a user or login
2. Browse approved products
3. Add products to cart
4. Rate and review products
5. Manage profile and preferences

## API Endpoints

### Authentication
- `POST /Account/Login` - User login
- `POST /Account/Register` - User registration
- `POST /Account/Logout` - User logout

### Products
- `GET /Products` - List all approved products
- `GET /Products/Details/{id}` - Product details
- `GET /Products/Search` - Search products
- `GET /Products/Category/{id}` - Products by category

### Admin (Requires Admin Role)
- `GET /Admin/Dashboard` - Admin dashboard
- `POST /Admin/ApproveProduct/{id}` - Approve product
- `POST /Admin/RejectProduct/{id}` - Reject product

### Merchant (Requires Merchant Role)
- `GET /Merchant/Dashboard` - Merchant dashboard
- `POST /Merchant/CreateProduct` - Create new product
- `PUT /Merchant/EditProduct/{id}` - Edit product

## Development

### Adding New Features
1. Create or modify models in `Models/` directory
2. Update database context if needed
3. Create migration: `dotnet ef migrations add MigrationName`
4. Apply migration: `dotnet ef database update`
5. Implement controllers and views

### Localization
1. Add new keys to resource files in `Resources/` directory
2. Use `@Localizer["Key"]` in Razor views
3. Use `_localizer["Key"]` in controllers

### Testing
1. Run the application: `dotnet run`
2. Test all three user roles
3. Verify database operations
4. Test localization features

## Deployment

### Development Deployment
```bash
dotnet publish -c Release -o ./publish
```

### Production Deployment
1. Update connection strings for production database
2. Configure HTTPS certificates
3. Set environment variables
4. Deploy to hosting platform (Azure, AWS, etc.)

## Troubleshooting

### Common Issues

#### Database Connection Issues
- Verify connection string in `appsettings.json`
- Ensure database server is running
- Check firewall settings

#### Migration Issues
- Delete `Migrations/` folder and recreate: `dotnet ef migrations add InitialCreate`
- Reset database: `dotnet ef database drop` then `dotnet ef database update`

#### Authentication Issues
- Clear browser cookies
- Check antiforgery token configuration
- Verify HTTPS/HTTP settings

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support and questions, please contact the development team or create an issue in the repository.

## Version History

- **v1.0.0** - Initial release with complete role-based functionality
- **v1.1.0** - Added localization support and currency conversion
- **v1.2.0** - Enhanced security and database integration

