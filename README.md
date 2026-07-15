# BeatWatch_BackEnd
Repositorio para el proyecto BeatWatch apartado web

## Configuracion local

No agregues secretos al repositorio. Configura MongoDB, JWT y reCAPTCHA mediante secretos de usuario:

```bash
dotnet user-secrets set "MongoDbSettings:ConnectionString" "mongodb+srv://<username>:<password>@cluster0.mongodb.net/<dbname>" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "MongoDbSettings:DatabaseName" "BeatWatchDb_Dev" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "JwtSettings:Issuer" "https://api.beatwatch.example" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "JwtSettings:Audience" "beatwatch-web" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "JwtSettings:SigningKey" "<al-menos-32-caracteres-aleatorios>" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "RecaptchaSettings:SecretKey" "<secreto-del-servidor-recaptcha>" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
```

En despliegues, usa las variables de entorno de `.env.example` desde un gestor de secretos. La aplicacion no iniciara si faltan secretos requeridos.
