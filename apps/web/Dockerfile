FROM node:20-alpine AS build
WORKDIR /app

COPY apps/web/package*.json ./
RUN npm install

COPY apps/web/ ./
RUN npm run build -- --configuration production --output-mode static

# Normalize Angular build output location for final image copy.
RUN mkdir -p /opt/web-dist \
    && if [ -d "dist/web/browser" ]; then cp -r dist/web/browser/. /opt/web-dist/; \
    elif [ -d "dist/web" ]; then cp -r dist/web/. /opt/web-dist/; \
    else echo "Angular production output not found under dist/"; exit 1; fi

FROM nginx:1.27-alpine AS runtime
WORKDIR /usr/share/nginx/html

COPY apps/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /opt/web-dist/ ./

EXPOSE 80
