FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

ARG VITE_APP_TITLE=Todo List
ARG VITE_API_BASE_URL=
ARG VITE_API_USERNAME=admin
ARG VITE_API_PASSWORD=admin
ARG VITE_LOG_LEVEL=info

ENV VITE_APP_TITLE=${VITE_APP_TITLE}
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_API_USERNAME=${VITE_API_USERNAME}
ENV VITE_API_PASSWORD=${VITE_API_PASSWORD}
ENV VITE_LOG_LEVEL=${VITE_LOG_LEVEL}

RUN npm run build

FROM nginx:1.27-alpine AS final
WORKDIR /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist ./

EXPOSE 80
