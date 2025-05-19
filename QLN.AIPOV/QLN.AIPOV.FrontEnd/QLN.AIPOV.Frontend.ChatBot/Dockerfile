# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["QLN.AIPOV.Frontend.ChatBot/QLN.AIPOV.Frontend.ChatBot.csproj", "QLN.AIPOV.Frontend.ChatBot/"]
RUN dotnet restore "QLN.AIPOV.Frontend.ChatBot/QLN.AIPOV.Frontend.ChatBot.csproj"
COPY . .
WORKDIR "/src/QLN.AIPOV.Frontend.ChatBot"
RUN dotnet build "QLN.AIPOV.Frontend.ChatBot.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "QLN.AIPOV.Frontend.ChatBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QLN.AIPOV.Frontend.ChatBot.dll"]
