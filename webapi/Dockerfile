ARG VERSION=5.0-alpine

FROM mcr.microsoft.com/dotnet/sdk:$VERSION AS build
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


FROM mcr.microsoft.com/dotnet/aspnet:$VERSION AS runtime
WORKDIR /app
COPY --from=build /out ./
ENTRYPOINT ["dotnet", "JwtAuthDemo.dll"]