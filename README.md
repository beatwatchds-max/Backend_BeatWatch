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
dotnet user-secrets set "RecaptchaSettings:SiteKey" "<clave-de-sitio-para-el-cliente-web>" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "RecaptchaSettings:SecretKey" "<secreto-del-servidor-recaptcha>" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "EmailSettings:FromAddress" "no-reply@tu-dominio.com" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "EmailSettings:SmtpHost" "smtp.tu-proveedor.com" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "EmailSettings:SmtpPort" "587" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "EmailSettings:SmtpUsername" "<usuario-smtp>" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "EmailSettings:SmtpPassword" "<contrasena-o-token-smtp>" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
dotnet user-secrets set "EmailSettings:PasswordResetUrl" "https://app.tu-dominio.com/restablecer-contrasena" --project BeatWatch_BackEnd/BeatWatch_BackEnd.csproj
```

En despliegues, usa las variables de entorno de `.env.example` desde un gestor de secretos. La aplicacion no iniciara si faltan secretos requeridos.

La clave de sitio de reCAPTCHA debe configurarse en el cliente web; el backend usa exclusivamente `RecaptchaSettings:SecretKey` para verificar los tokens.

## Recuperacion de contrasena

Configura SMTP y `EmailSettings:PasswordResetUrl` antes de activar el formulario web. `POST /api/autenticacion/recuperar-contrasena` recibe `{ "correo": "..." }` y siempre responde igual para no revelar cuentas registradas. El enlace contiene un token de un solo uso con vigencia de una hora. El formulario debe enviar `{ "token": "...", "contrasena": "..." }` a `POST /api/autenticacion/restablecer-contrasena`.
