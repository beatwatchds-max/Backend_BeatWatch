# BeatWatch_BackEnd
Repositorio para el proyecto BeatWatch apartado web

## Configuracion local

No agregues secretos al repositorio. Configura la conexion de MongoDB mediante secretos de usuario:

```bash
dotnet user-secrets set "MongoDbSettings:ConnectionString" "mongodb+srv://<username>:<password>@cluster0.mongodb.net/<dbname>" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "MongoDbSettings:DatabaseName" "BeatWatchDb_Dev" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
```

En despliegues, usa las variables de entorno `MongoDbSettings__ConnectionString` y `MongoDbSettings__DatabaseName` desde un gestor de secretos.
