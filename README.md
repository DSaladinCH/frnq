# 📊 FRNQ - Financial Portfolio Tracker

<div align="center">

**A modern, full-stack investment portfolio tracking application**

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SvelteKit](https://img.shields.io/badge/SvelteKit-5.0-FF3E00?logo=svelte)](https://kit.svelte.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791?logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)

</div>

---

## ✨ Features

### 💼 Investment Management
- **Track Multiple Asset Types**: Buy, Sell, and Dividend transactions
- **Real-time Price Updates**: Integration with Yahoo Finance for live market data
- **Historical Data**: View and analyze your investment history over time
- **Bulk Import**: Import multiple investments via CSV with intelligent column mapping
- **Flexible Filtering**: Filter by date range, quote, group, or investment type

### 📈 Portfolio Analytics
- **Position Tracking**: Monitor your current holdings and their performance
- **Interactive Charts**: Visualize portfolio value over time with customizable periods (1W, 1M, 3M, YTD, All Time)
- **Performance Metrics**: Track gains, losses, dividends, and total returns
- **Multi-Currency Support**: Handle investments across different currencies

### 🏷️ Organization
- **Quote Groups**: Organize your investments into custom groups
- **Custom Quote Names**: Personalize quote display names
- **Smart Search**: Quickly find investments with intelligent filtering

### 🔐 Security & Authentication
- **JWT Authentication**: Secure token-based authentication
- **OIDC Integration**: Support for external identity providers (OAuth2/OIDC)
- **External Account Linking**: Link multiple external accounts to a single user
- **User Management**: Comprehensive user profile and settings

### 🎨 User Experience
- **Responsive Design**: Beautiful interface that works on desktop, tablet, and mobile
- **Dark/Light Mode**: Customizable theme preferences
- **Date Format Preferences**: Localized date formatting
- **Infinite Scroll**: Smooth browsing experience for large investment lists
- **Real-time Notifications**: Toast notifications for user actions

---

## 🏗️ Architecture

### Backend (API)
- **Framework**: .NET 9.0 with ASP.NET Core
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT Bearer tokens with refresh token support
- **API Documentation**: OpenAPI/Swagger integration
- **Real-time Data**: Yahoo Finance provider for market quotes

### Frontend (UI)
- **Framework**: SvelteKit 5 with TypeScript
- **Styling**: Tailwind CSS 4
- **Charts**: Chart.js for data visualization
- **Build Tool**: Vite
- **State Management**: Svelte stores with reactive patterns

### Infrastructure
- **Containerization**: Docker & Docker Compose
- **Database**: PostgreSQL 16
- **Reverse Proxy Ready**: Configured for production deployment

---

## 🚀 Quick Start

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [PostgreSQL 16](https://www.postgresql.org/) or [Docker](https://www.docker.com/)

### Option 1: Docker (Recommended)

```bash
# Pull pre-built images from GitHub Container Registry
docker pull ghcr.io/dsaladinch/frnq-api:latest
docker pull ghcr.io/dsaladinch/frnq-ui:latest

# Navigate to docker directory
cd docker

# Start all services
docker-compose up -d
```

**Access the application:**
- Frontend: http://localhost:3000
- Backend API: http://localhost:8080
- Database: localhost:5432

### Option 2: Manual Setup

#### 1. Setup Database
```bash
# Using Docker
docker run --name frnq-postgres -e POSTGRES_PASSWORD=your-password -p 5432:5432 -d postgres:16

# Or install PostgreSQL locally
```

#### 2. Configure Backend
```bash
cd backend

# Update connection string in appsettings.json
# Update JWT secret key

# Run migrations
dotnet ef database update

# Start the API
dotnet run
```

Backend runs on: https://localhost:7000

#### 3. Configure Frontend
```bash
cd frontend

# Install dependencies
npm install

# Create .env file with API URL
echo "VITE_API_BASE_URL=http://localhost:8080" > .env

# Start development server
npm run dev
```

Frontend runs on: http://localhost:5173

---

## 📋 Configuration

### Backend Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DatabaseConnection": "Server=localhost;Port=5432;Database=frnq;User Id=postgres;Password=your-password;"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "https://yourdomain.com"]
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key",
    "Issuer": "frnq-api",
    "Audience": "frnq-client",
    "AccessTokenExpiryInMinutes": "15"
  },
  "Features": {
    "SignupEnabled": true
  },
  "ApiBaseUrl": "https://localhost:7000",
  "FrontendUrl": "http://localhost:5173"
}
```

#### Key Configuration Options

- **`ConnectionStrings:DatabaseConnection`**: PostgreSQL connection string
- **`Cors:AllowedOrigins`**: Array of allowed frontend origins for CORS (supports multiple domains for production)
- **`JwtSettings:SecretKey`**: Secret key for JWT token signing (min 256 bits, keep secure!)
- **`JwtSettings:AccessTokenExpiryInMinutes`**: Access token expiration time
- **`Features:SignupEnabled`**: Enable/disable user registration
- **`ApiBaseUrl`**: Base URL of the API (used for OAuth redirects)
- **`FrontendUrl`**: Frontend URL (used for OAuth redirects)

### Frontend Configuration

**Local Development:** Create a `.env` file in the frontend directory:

```bash
# frontend/.env
VITE_API_BASE_URL=http://localhost:8080
```

**Production (Docker):** Configuration is handled via environment variables in `docker-compose.yml`. No additional configuration needed.

---

## 🐳 Docker Deployment

### Manage Services
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Stop and remove all data
docker-compose down -v
```

For detailed Docker instructions, see [docker/README.md](docker/README.md).

---

## 📦 Project Structure

```
frnq/
├── backend/                      # .NET 8 API
│   ├── Auth/                    # Authentication & user management
│   ├── Investment/              # Investment tracking logic
│   ├── Position/                # Portfolio position calculations
│   ├── Quote/                   # Market data & providers
│   ├── Group/                   # Quote grouping
│   ├── Migrations/              # EF Core database migrations
│   └── Program.cs               # Application entry point
│
├── frontend/                     # SvelteKit application
│   ├── src/
│   │   ├── lib/
│   │   │   ├── components/      # Reusable UI components
│   │   │   ├── services/        # API service layer
│   │   │   ├── stores/          # State management
│   │   │   ├── Models/          # TypeScript models
│   │   │   └── utils/           # Helper functions
│   │   └── routes/              # SvelteKit routes
│   ├── static/                  # Static assets
│   └── package.json
│
└── docker/                       # Docker configuration
    ├── docker-compose.yml       # Multi-service orchestration
    ├── Dockerfile.api           # Backend container
    └── Dockerfile.ui            # Frontend container
```

---

## 🔌 API Endpoints

### Authentication
- `POST /api/auth/signup` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - User logout
- `GET /api/auth/user` - Get current user info
- `PUT /api/auth/user` - Update user profile

### Investments
- `GET /api/investments` - List investments (paginated, filterable)
- `POST /api/investments` - Create investment
- `POST /api/investments/bulk` - Bulk create investments
- `PUT /api/investments/{id}` - Update investment
- `DELETE /api/investments/{id}` - Delete investment

### Positions
- `GET /api/positions` - Get portfolio positions
- `GET /api/positions/snapshots` - Get historical portfolio snapshots

### Quotes
- `GET /api/quotes` - List available quotes
- `GET /api/quotes/{id}` - Get quote details
- `PUT /api/quotes/{id}` - Update quote (custom name)
- `POST /api/quotes/refresh` - Refresh quote prices

### Groups
- `GET /api/groups` - List quote groups
- `POST /api/groups` - Create group
- `PUT /api/groups/{id}` - Update group
- `DELETE /api/groups/{id}` - Delete group

---

## 🛠️ Development

### Backend Development
```bash
cd backend

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests (if available)
dotnet test

# Run with hot reload
dotnet watch run
```

### Frontend Development
```bash
cd frontend

# Install dependencies
npm install

# Development server
npm run dev

# Type checking
npm run check

# Linting
npm run lint

# Format code
npm run format

# Build for production
npm run build

# Preview production build
npm run preview
```

### Database Migrations
```bash
cd backend

# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigration
```

---

## 🔐 Security Considerations

1. **Change Default Secrets**: Update JWT secret keys before production deployment
2. **Database Credentials**: Use strong passwords and secure connection strings
3. **HTTPS**: Always use HTTPS in production
4. **CORS**: Configure appropriate CORS policies
5. **Environment Variables**: Store sensitive configuration in environment variables
6. **Rate Limiting**: Implement rate limiting for API endpoints (recommended)
7. **Input Validation**: All inputs are validated on both client and server

---

## 🌐 External Service Integration

### Yahoo Finance Provider
FRNQ uses Yahoo Finance for real-time and historical market data. The system:
- Automatically fetches quotes when new symbols are added
- Updates historical prices on-demand
- Caches data to minimize API calls

### OIDC/OAuth2 Providers
Configure external authentication providers in `appsettings.json`:
```json
"OidcProviders": {
  "google": {
    "Enabled": true,
    "DisplayName": "Google",
    "AuthorizationEndpoint": "https://accounts.google.com/o/oauth2/v2/auth",
    "TokenEndpoint": "https://oauth2.googleapis.com/token",
    "UserInfoEndpoint": "https://www.googleapis.com/oauth2/v3/userinfo",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

---

## 🎯 Roadmap

- [ ] Advanced analytics and benchmarking
- [ ] Export/Import portfolio data
- [ ] Multi-language support

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👨‍💻 Author

**DSaladin**

- GitHub: [@DSaladinCH](https://github.com/DSaladinCH)

---

## 🙏 Acknowledgments

- [SvelteKit](https://kit.svelte.dev/) for the amazing frontend framework
- [.NET](https://dotnet.microsoft.com/) for the robust backend platform
- [Tailwind CSS](https://tailwindcss.com/) for the utility-first styling
- [Chart.js](https://www.chartjs.org/) for beautiful charts

---

<div align="center">

**⭐ Star this repository if you find it helpful!**

Made with ❤️ by [DSaladin](https://github.com/DSaladinCH)

</div>
