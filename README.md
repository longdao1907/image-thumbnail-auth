# image-thumbnail-auth

This repository provides authentication services for the Image Thumbnail application ecosystem. It is developed in C# and designed to offer robust, secure authentication APIs and utilities, enabling other components in the Image Thumbnail suite to verify user identity and manage access.

## Features

- RESTful API endpoints for authentication
- User registration and login support
- Integration with Entity Framework Core for data persistence
- Configurable authentication schemes
- Error handling and logging utilities
- Docker support for containerized deployment

## Usage

Clone the repository and follow standard .NET build procedures:

```bash
git clone https://github.com/longdao1907/image-thumbnail-auth.git
cd image-thumbnail-auth
dotnet build
dotnet run
```

For Docker:

```bash
docker build -t image-thumbnail-auth .
docker run -p 5000:80 image-thumbnail-auth
```

## Configuration

Configure connection strings and authentication settings in `appsettings.json`.

## License

No license specified.

## Author

[longdao1907](https://github.com/longdao1907)

---

*This README is a template based on the repository context and may need updates to reflect actual features and usage.*
