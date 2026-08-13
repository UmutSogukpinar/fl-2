FROM prom/prometheus:v3.13.1

COPY config/prometheus-config.yaml /etc/prometheus/config.yml

CMD ["--config.file=/etc/prometheus/config.yml", "--web.enable-otlp-receiver"]
