FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar el archivo de proyecto y restaurar dependencias
COPY ["trabfinal.csproj", "./"]
RUN dotnet restore "trabfinal.csproj"

# Copiar el resto del código y compilar la aplicación
COPY . .
RUN dotnet publish "trabfinal.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Crear la imagen final usando el runtime de ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponer el puerto por defecto que provee Render
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "trabfinal.dll"]