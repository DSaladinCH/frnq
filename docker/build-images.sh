#!/bin/bash
# Build Docker images for FRNQ application

set -e

API=false
UI=false
TAG="latest"
API_URL="http://localhost:8080"

# Parse arguments
while [[ $# -gt 0 ]]; do
	case $1 in
		--api)
			API=true
			shift
			;;
		--ui)
			UI=true
			shift
			;;
		--tag)
			TAG="$2"
			shift 2
			;;
		--api-url)
			API_URL="$2"
			shift 2
			;;
		*)
			echo "Unknown option: $1"
			echo "Usage: $0 [--api] [--ui] [--tag TAG] [--api-url URL]"
			exit 1
			;;
	esac
done

# If no specific service specified, build both
if [[ "$API" == false && "$UI" == false ]]; then
	API=true
	UI=true
fi

echo "Building FRNQ Docker Images"
echo "============================="
echo ""

# Change to script directory
cd "$(dirname "$0")"

if [[ "$API" == true ]]; then
	echo "Building API image (frnq-api:$TAG)..."
	docker build -t "frnq-api:$TAG" -f Dockerfile.api ..
	echo "✓ API image built successfully"
	echo ""
fi

if [[ "$UI" == true ]]; then
	echo "Building UI image (frnq-ui:$TAG)..."
	echo "API URL: $API_URL"
	docker build -t "frnq-ui:$TAG" --build-arg VITE_API_BASE_URL="$API_URL" -f Dockerfile.ui ..
	echo "✓ UI image built successfully"
	echo ""
fi

echo "============================="
echo "All images built successfully!"
echo ""
echo "Available images:"
docker images | grep "frnq-"
echo ""
echo "UI was built with API URL: $API_URL"
echo "To start services, run: docker-compose up -d"
