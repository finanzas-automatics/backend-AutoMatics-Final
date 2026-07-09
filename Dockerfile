FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiamos el archivo de proyecto y restauramos las dependencias
COPY ["Automatics.csproj", "./"]
RUN dotnet restore "Automatics.csproj"

# Copiamos el resto del código y construimos la aplicación
COPY . .
RUN dotnet publish "Automatics.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Construimos la imagen de producción
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponemos el puerto de Railway (Railway inyecta la variable de entorno PORT)
# Usamos un script de inicio para leer el $PORT al momento de ejecutar
CMD ["sh", "-c", "dotnet Automatics.dll --urls http://0.0.0.0:${PORT:-8080}"]
