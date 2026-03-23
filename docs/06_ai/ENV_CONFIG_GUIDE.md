
# ENV_CONFIG_GUIDE.md – Full Version

## 1. Backend `.env`
```
ASPNETCORE_ENVIRONMENT=Development
JWT_SECRET=supersecret
DB_CONNECTION=Host=localhost;Port=5432;Database=cephasops;Username=postgres;Password=postgres
STORAGE_PATH=/var/storage
EMAIL_PARSER_HOST=imap.gmail.com
EMAIL_PARSER_USER=cephas@serverfreak.com
EMAIL_PARSER_PASS=xxxxxx
```

## 2. Frontend `.env`
```
VITE_API_URL=http://localhost:5000
VITE_COMPANY_SWITCH=true
```

## 3. Local Docker
```
docker-compose up --build
```

## 4. Multi-company config
- Create company records in DB
- Assign users to companies
- Cursor AI must attach `X-Company-Id` header on every API call
