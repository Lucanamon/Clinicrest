# syntax=docker/dockerfile:1.6
# Angular production build + nginx static serve
ARG NODE_IMAGE=node:20-alpine
ARG NGINX_IMAGE=nginx:1.27-alpine

FROM ${NODE_IMAGE} AS build
WORKDIR /app

# Monorepo: root lockfile + workspace package manifests
COPY package.json package-lock.json ./
COPY apps/web/package.json apps/web/
RUN --mount=type=cache,id=npm-clinicrest,target=/root/.npm \
    npm ci

COPY apps/web/ ./apps/web/
# Static browser bundle (no Node SSR server in container)
RUN npm run build -w web -- --configuration production --output-mode static

# Normalize Angular output (browser/ vs flat); workspace root is /app, app lives in apps/web/
RUN mkdir -p /opt/web-dist \
    && if [ -d "apps/web/dist/web/browser" ]; then cp -r apps/web/dist/web/browser/. /opt/web-dist/; \
    elif [ -d "apps/web/dist/web" ]; then cp -r apps/web/dist/web/. /opt/web-dist/; \
    else echo "Angular production output not found under apps/web/dist/"; exit 1; fi

FROM ${NGINX_IMAGE} AS runtime
WORKDIR /usr/share/nginx/html

LABEL org.opencontainers.image.title="Clinicrest Web" \
      org.opencontainers.image.description="Angular SPA + nginx"

COPY apps/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /opt/web-dist/ ./

# nginx:alpine runs as root by default; for read-only static files this is common.
# Hardening: add read-only rootfs in compose if desired.

EXPOSE 80
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://127.0.0.1/ || exit 1
