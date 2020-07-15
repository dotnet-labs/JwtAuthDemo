FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /app

COPY ./*.sln .
COPY ./JwtAuthDemo/*.csproj ./JwtAuthDemo/
COPY ./JwtAuthDemo.IntegrationTests/*.csproj ./JwtAuthDemo.IntegrationTests/
RUN dotnet restore

COPY . .

WORKDIR /app/JwtAuthDemo.IntegrationTests
RUN dotnet test --no-restore

WORKDIR /app/JwtAuthDemo
RUN dotnet publish -c Release -o /out --no-restore


FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS http://*:5000
ENTRYPOINT ["dotnet", "JwtAuthDemo.dll"]