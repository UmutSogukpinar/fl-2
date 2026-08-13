FROM grafana/loki:3.5.5

EXPOSE 3100

COPY config/loki-config.yaml /etc/loki/loki-config.yaml

CMD ["-config.file=/etc/loki/loki-config.yaml"]
