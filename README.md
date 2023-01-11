# studious-enigma

Web API with JWT Authentication and ASP.NET Core Identity

### Instruction
1. Clone repository:
```console
    git clone https://github.com/dotnewt/studious-enigma.git
```

2. Add Migrations
```console
    dotnet ef migrations add InitialCreate -c AuthDbContext -o Migrations/AuthMigrations
```
```console
    dotnet ef migrations add InitialCreate -c MemoryCryptContext -o Migrations/MemoryCryptMigrations
```

3. Update DB
```console
    dotnet ef database update -c AuthDbContext
```
```console
    dotnet ef database update -c MemoryCryptContext
```


4. Run project:
```console
    cd  studious-enigma
    dotnet run 
```
