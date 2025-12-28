# syntax=docker/dockerfile:1

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env

WORKDIR /source

COPY FxNyaa/*.csproj FxNyaa/

ARG TARGETARCH

RUN dotnet restore FxNyaa/ -a $TARGETARCH

COPY . .

RUN set -xe; \
dotnet publish FxNyaa/ -c Release -a $TARGETARCH -o /app; \
chmod +x /app/FxNyaa

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build-env /app .

ENTRYPOINT [ "dotnet", "FxNyaa.dll" ]
# For configuration, i think you can just set the ASPNETCORE_ENVIRONMENT to "Docker", and add a volume with a file called appsettings.Docker.json :clueless: