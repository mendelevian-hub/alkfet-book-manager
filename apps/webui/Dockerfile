
FROM node:22 AS build
WORKDIR /app

COPY apps/webui/package*.json ./
RUN npm ci

COPY apps/webui/ ./
RUN npm run build

FROM nginx:alpine
COPY apps/webui/nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist/webui /usr/share/nginx/html

EXPOSE 80
