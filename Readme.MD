# Communicatio Project

Sample infrastructure with a few components.
A Producer of events. A Consumer of events. And RedPanda as a broker (replacement for Kafka).

## Tech stack

C#, .Net8, Docker, Kafka client, RedPanda

## Goal

The main goal is to understand modern concepts and techniques, plus to get a workable sample.
What was surprising, it was easy and fast to develop and test the solution in a fully containerized infrastructure that runs locally.

## How to run Docker infrastructure

```cmd
docker-compose build --no-cache && docker-compose up -d --force-recreate
```

## How to stop infrastructure

```cmd
docker-compose -f docker-compose.yml down
```

## How to stop infrastructure and delete all data

```cmd
docker-compose -f docker-compose.yml down -v
```

## TODO

- Update Readme
- Add diagram
- Add OpenTelemetry
- Add tests
- Add Grafana (and Prometeus?)
- Add database for each service
- Add REST endpoints (independent microservices?)
- Use meaningful events and topics
- Etc.
