# KeyValueStore

A small client-server key-value storage application built with .NET 10.

The project consists of two console applications:

- `KeyValueStore.Server` — stores data and handles TCP requests
- `KeyValueStore.Client` — connects to the server and sends commands

## Running the server

```bash
dotnet run --project KeyValueStore.Server
```

## Running the client

```bash
dotnet run --project KeyValueStore.Client -- localhost 5000
```

## Supported commands

```text
SET key value
GET key
UPDATE key value
DELETE key
EXISTS key
EXIT
```

The client communicates with the server over TCP. Data is stored in memory and is lost when the server stops.

## Status

This project is under development.