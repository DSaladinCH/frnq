# FRNQ Docker Setup

This directory contains Docker configuration files for the FRNQ application stack.

## Prerequisites

- Docker Engine 20.10+
- Docker Compose v2.0+

## Quick Start

### Build Docker Images

Before running the services, you need to build the Docker images:

```powershell
# From the docker directory

# Build API image
docker build -t frnq-api:latest -f Dockerfile.api ..

# Build UI image
docker build -t frnq-ui:latest -f Dockerfile.ui ..

# Or use the build script (recommended)
.\build-images.ps1

# Build with custom API URL
.\build-images.ps1 -ApiUrl "http://your-api-url:8080"
```

### Run All Services

```bash
docker-compose up -d
```

This will start:
- PostgreSQL database on port 5432
- Backend API (.NET) on port 8080
- Frontend UI (SvelteKit) on port 3000

### Access the Application

- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:8080
- **PostgreSQL**: localhost:5432

## Commands

### Start Services
```bash
docker-compose up -d
```

### Stop Services
```bash
docker-compose down
```

### Stop and Remove Volumes (Clean Database)
```bash
docker-compose down -v
```

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f ui
docker-compose logs -f postgres
```

### Rebuild Images
```bash
# From the docker directory

# Rebuild API image
docker build -t frnq-api:latest -f Dockerfile.api ..

# Rebuild UI image (specify API URL at build time)
docker build -t frnq-ui:latest --build-arg VITE_API_BASE_URL=http://localhost:8080 -f Dockerfile.ui ..

# Or use the build scripts
# PowerShell
.\build-images.ps1 -ApiUrl "http://localhost:8080"

# Bash
./build-images.sh --api-url "http://localhost:8080"

# Then restart services
docker-compose up -d
```

### Execute Commands in Containers
```bash
# Access API container
docker-compose exec api sh

# Access UI container
docker-compose exec ui sh

# Access PostgreSQL
docker-compose exec postgres psql -U frnq -d frnq
```

## Configuration

### Environment Variables

You can override environment variables by creating a `.env` file in the docker directory:

```env
# Database
POSTGRES_USER=frnq
POSTGRES_PASSWORD=your_secure_password
POSTGRES_DB=frnq

# API
JWT_SECRET_KEY=your-super-secret-key-here
ASPNETCORE_ENVIRONMENT=Production

# UI
NODE_ENV=production
PUBLIC_API_URL=http://api:8080
```

### Ports

Default ports can be changed in `docker-compose.yml`:

- `postgres`: 5432:5432
- `api`: 8080:8080
- `ui`: 3000:3000

## Database Migrations

After the first startup, you may need to run database migrations:

```bash
# Access the API container
docker-compose exec api sh

# Run migrations
dotnet ef database update
```

## Troubleshooting

### Check Service Health
```bash
docker-compose ps
```

### Service Not Starting
```bash
# Check logs
docker-compose logs [service-name]

# Restart service
docker-compose restart [service-name]
```

### Database Connection Issues
```bash
# Verify database is healthy
docker-compose exec postgres pg_isready -U frnq

# Check database logs
docker-compose logs postgres
```

### Clean Rebuild
```bash
# Stop everything and remove volumes
docker-compose down -v

# Remove images
docker-compose rm -f

# Rebuild from scratch
docker-compose up -d --build
```

## Production Considerations

Before deploying to production:

1. **Security**:
   - Change default passwords in environment variables
   - Use Docker secrets for sensitive data
   - Set strong JWT secret key
   - Configure HTTPS/TLS

2. **Performance**:
   - Adjust PostgreSQL configuration for your workload
   - Configure proper resource limits
   - Set up log rotation

3. **Persistence**:
   - Backup PostgreSQL data volume regularly
   - Use external volume for production data

4. **Monitoring**:
   - Add health check endpoints
   - Configure logging aggregation
   - Set up monitoring and alerts

## Development Mode

For development with hot-reload:

```bash
# Use docker-compose.dev.yml (if created)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
```

## Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│             │     │             │     │             │
│  Frontend   │────▶│   Backend   │────▶│  PostgreSQL │
│  (SvelteKit)│     │   (.NET 9)  │     │             │
│   :3000     │     │    :8080    │     │    :5432    │
└─────────────┘     └─────────────┘     └─────────────┘
```

All services communicate through the `frnq-network` Docker network.
