# Etapa 1: compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app

# Etapa 2: imagen final, más liviana
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# Librería OpenMP que usa el entrenador Matrix Factorization de ML.NET en Linux.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgomp1 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "UniConnect.dll"]