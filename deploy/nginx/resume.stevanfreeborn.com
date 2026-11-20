# Install on VPS at /etc/nginx/sites-available/resume.stevanfreeborn.com
# Then:
#   sudo ln -s /etc/nginx/sites-available/resume.stevanfreeborn.com /etc/nginx/sites-enabled/
#   sudo nginx -t && sudo systemctl reload nginx
#   sudo certbot --nginx -d resume.stevanfreeborn.com

server {
    listen 80;
    listen [::]:80;
    server_name resume.stevanfreeborn.com;

    location / {
        proxy_pass http://127.0.0.1:5100;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
