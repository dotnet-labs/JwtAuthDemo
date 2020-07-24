# JWT Auth Demo

This repository demos an ASP.NET Core web API application using JWT auth, and an integration testing project for a set of actions including login, logout, refresh token, impersonation, authentication, and authorization.

## Usage

1. Run in Visual Studio or in VS Code

   ```cmd
   dotnet watch run
   ```

1. Run with Docker: restore NuGet packages, run tests, publish web API app, and build a Docker image.

   ```Docker
   docker build -t jwtauthdemo_api .
   ```

   recommend to use the `docker-compose.yml` file in the parent directory to launch the app.
