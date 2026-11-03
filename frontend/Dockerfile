FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
# npm ci замість npm install (виправлення після аудиту): відтворювана збірка з package-lock.json
# без "розповзання" версій транзитивних залежностей між білдами. Потребує закомічений lock-файл
# (тепер він є в репозиторії) — npm ci навмисно провалиться, якщо package.json і lock розійшлись.
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
