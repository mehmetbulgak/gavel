FROM node:26-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN --mount=type=cache,id=npm,target=/root/.npm npm ci --ignore-scripts --no-audit --no-fund
COPY . .
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.28-alpine AS runtime
COPY nginx/ /etc/nginx/
COPY --from=build /src/dist /usr/share/nginx/html
EXPOSE 8080
