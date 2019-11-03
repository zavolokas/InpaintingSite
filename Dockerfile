#
# This file can be used to create a docker container that will automatically execute
# the InpaintHTTP executeable on running it. 
#
# Building: docker build -t <your tag> .
# Example:  docker build -t inpainter/inpainter .
#
# Running: docker run -p 5000:80 -it --rm <your tag>
# Example: docker run -p 5000:80 -it inpainter/inpainter
#

# create a build container with the .NET Core
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env
WORKDIR /app

# copy some projects and build and publish the app
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o output

# Runtime image
# FROM mcr.microsoft.com/dotnet/core/aspnet:2.2
FROM mcr.microsoft.com/dotnet/core/runtime:2.2
# FROM microsoft/dotnet:2.1-aspnetcore-runtime

# Required to avoid Gdip initialization issues
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
     && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build-env /app/InpaintHTTP/output .
ENTRYPOINT ["dotnet", "InpaintHTTP.dll"]