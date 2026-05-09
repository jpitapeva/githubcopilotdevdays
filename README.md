# Todo List - .NET 10 + Vue 3

Aplicacao didatica de lista de tarefas com:

- Backend em ASP.NET Core Minimal API (.NET 10)
- Frontend em Vue 3 + Vite
- Persistencia em SQLite
- Autenticacao basica (usuario unico)
- Logs estruturados em backend e frontend
- Configuracao por `.env`
- Containerizacao com Docker e Docker Compose

## Estrutura

- `backend/TodoApi`: API minimal com CRUD e SQLite
- `backend/TodoApi.Tests`: testes de integracao xUnit
- `frontend/TodoWeb`: SPA Vue para gerenciar tarefas

## Credenciais padrao

- Usuario: `admin`
- Senha: `admin`

## Executar localmente (sem Docker)

### Backend

1. Copie e ajuste variaveis:
   - `backend/TodoApi/.env.example` -> `backend/TodoApi/.env`
2. Execute:
   - `dotnet run --project backend/TodoApi/TodoApi.csproj`

API disponivel em `http://localhost:5200`.

### Frontend

1. Instale e use Node LTS com nvm:
   - `nvm version`
   - se nao existir: `winget install CoreyButler.NVMforWindows`
   - `nvm install lts`
   - `nvm use lts`
2. Copie e ajuste variaveis:
   - `frontend/TodoWeb/.env.example` -> `frontend/TodoWeb/.env`
3. Instale dependencias e execute:
   - `cd frontend/TodoWeb`
   - `npm install`
   - `npm run dev`

Frontend disponivel em `http://localhost:5173`.

## Testes

- Backend (xUnit):
  - `dotnet test githubcopilotdevdays.sln`
- Frontend (Vitest):
  - `cd frontend/TodoWeb`
  - `npm run test`

## Docker

O projeto usa:

- `backend/TodoApi/Dockerfile` com .NET 10
- `frontend/TodoWeb/Dockerfile` com Node LTS oficial para build e Nginx para runtime
- `docker-compose.yml` para orquestracao

Subir a stack:

- `docker compose up --build`

Acessos:

- Frontend: `http://localhost:8080`
- Backend: `http://localhost:5200`

> Observacao: nao usamos nvm dentro de Dockerfile, apenas imagem oficial do Node.js, conforme boas praticas.
