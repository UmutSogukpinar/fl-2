FROM grafana/alloy:v1.10.2

COPY config/config.alloy /etc/alloy/config.alloy

EXPOSE 12345 4318

CMD ["run", "--server.http.listen-addr=0.0.0.0:12345", "/etc/alloy/config.alloy"]
