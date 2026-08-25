FROM rabbitmq:4.3.4

EXPOSE 5672
EXPOSE 15672
EXPOSE 15692

WORKDIR /var/lib/rabbitmq

COPY config/rabbitmq.conf /etc/rabbitmq/rabbitmq.conf
COPY definitions.json /etc/rabbitmq/definitions.json

RUN rabbitmq-plugins enable --offline rabbitmq_prometheus

CMD ["rabbitmq-server"]
