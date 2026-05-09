# TodoWeb

Aplicacao frontend em Vue 3 + Vite para a API Todo.

## Requisitos locais

- Node.js LTS (gerenciado com nvm)

### nvm no Windows

1. Verifique se existe:
	- `nvm version`
2. Se nao existir, instale `nvm-windows`:
	- `winget install CoreyButler.NVMforWindows`
3. Instale e selecione a versao LTS do Node:
	- `nvm install lts`
	- `nvm use lts`
4. Valide:
	- `node -v`
	- `npm -v`

## Executar localmente

1. Instalar dependencias:
	- `npm install`
2. Rodar em modo desenvolvimento:
	- `npm run dev`
3. Build de producao:
	- `npm run build`
4. Testes:
	- `npm run test`

## Variaveis de ambiente

Use `.env` (ou copie de `.env.example`):

- `VITE_APP_TITLE`
- `VITE_API_BASE_URL`
- `VITE_API_USERNAME`
- `VITE_API_PASSWORD`
- `VITE_LOG_LEVEL`
- `VITE_HOST`
- `VITE_PORT`

## Docker

O Dockerfile usa imagem oficial Node LTS para build e Nginx para servir arquivos estaticos.
Nao usa nvm dentro do container, conforme boas praticas.
