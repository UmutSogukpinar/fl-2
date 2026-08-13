FROM grafana/grafana:13.1.3

COPY --chown=472:0 grafana.ini /etc/grafana/grafana.ini
COPY --chown=472:0 provisioning /etc/grafana/provisioning

EXPOSE 3000
