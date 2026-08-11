FROM grafana/loki:2.9.4

EXPOSE 3100

COPY loki-config.yaml /etc/loki/loki-config.yaml

CMD ["-config.file=/etc/loki/loki-config.yaml"]
