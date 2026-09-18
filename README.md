# KeyValueStore

A small client-server in-memory key-value store built with .NET 10.

## Projects

- `KeyValueStore.Server` — TCP server and in-memory storage
- `KeyValueStore.Client` — interactive TCP client
- `KeyValueStore.Server.Tests` — automated server tests

## Run

Start the server on port 5000:

```sh
dotnet run --project KeyValueStore.Server
```

Start the server on a custom port:

```sh
dotnet run --project KeyValueStore.Server -- 6000
```

Connect the client:

```sh
dotnet run --project KeyValueStore.Client -- localhost 5000
```

## Commands

| Command | Description |
| --- | --- |
| `SET key value` | Create or replace a value |
| `SET key value EX seconds` | Create or replace a value with expiration |
| `GET key` | Return a value |
| `UPDATE key value` | Update an existing value |
| `UPDATE key value EX seconds` | Update a value and expiration |
| `DELETE key` | Delete a key |
| `EXISTS key` | Check whether a key exists |
| `EXPIRE key seconds` | Set or replace expiration |
| `TTL key` | Return remaining time to live |
| `PERSIST key` | Remove expiration and make the key persistent |
| `EXIT` | Exit the client |

## TTL result codes

The `TTL` result is returned in the response `value` field.

| Code | Meaning |
| ---: | --- |
| Positive number | Remaining seconds before expiration |
| `-1` | The key exists without expiration |
| `-2` | The key does not exist or has expired |

Example TTL responses:

```json
{"success":true,"message":"TTL returned","value":"297"}
{"success":true,"message":"TTL returned","value":"-1"}
{"success":true,"message":"TTL returned","value":"-2"}
```

TTL values are rounded down to whole seconds. An active key with less than one second remaining returns `1`.

## Response format

Responses use JSON:

| Field | Description |
| --- | --- |
| `success` | Whether the command succeeded |
| `message` | Operation result |
| `value` | Key value or TTL result |
| `exists` | Result of the `EXISTS` command |

Example error response:

```json
{"success":false,"message":"Key not found"}
```

## TCP protocol

Requests and responses are UTF-8 JSON lines exchanged over TCP. Supported operations are `set`, `get`, `update`, `delete`, `exists`, `expire`, `ttl`, and `persist`.

## Limitations

- Keys and values cannot contain spaces
- Keys cannot be empty
- Values cannot be empty for `SET` and `UPDATE`
- TTL values must be positive integers
- All data is stored in memory and is lost when the server stops

## Testing

Run all tests from the solution directory:

```sh
dotnet test
```

The test project is `KeyValueStore.Server.Tests`.
