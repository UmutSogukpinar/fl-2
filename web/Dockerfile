# =================================
# ========== Build Stage ==========
# =================================

FROM node:24-alpine AS build

WORKDIR /app

COPY FantasyLeague.Client/package*.json ./
RUN npm ci

COPY FantasyLeague.Client/ ./
RUN npm run build

FROM nginx:1.29-alpine AS final

COPY conf/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
