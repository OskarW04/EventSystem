# ETAP 1: Budowanie (używamy pełnego SDK .NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopiujemy pliki projektów najpierw (optymalizacja cache'a Dockera)
COPY ["EventSystem.API/EventSystem.API.csproj", "EventSystem.API/"]
COPY ["EventSystem.Core/EventSystem.Core.csproj", "EventSystem.Core/"]

# Przywracamy pakiety
RUN dotnet restore "EventSystem.API/EventSystem.API.csproj"

# Kopiujemy całą resztę kodu
COPY . .
WORKDIR "/src/EventSystem.API"

# Budujemy wersję produkcyjną (Release)
RUN dotnet publish "EventSystem.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ETAP 2: Uruchamianie (używamy lekkiego obrazu tylko z procesem uruchomieniowym)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Domyślny port dla nowoczesnych aplikacji .NET
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Kopiujemy zbudowane pliki z Etapu 1
COPY --from=build /app/publish .

# Punkt wejścia aplikacji
ENTRYPOINT ["dotnet", "EventSystem.API.dll"]