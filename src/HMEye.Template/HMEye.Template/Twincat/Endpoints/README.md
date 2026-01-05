# PLC Data Endpoints

This API provides access to TwinCAT PLC variables.

## Authentication
All requests must include the API key in the `X-Api-Key` header.
- **Header**: `X-Api-Key: <Your-Secret-Key>`

## Service Base URL
`https://<host>:<port>/api/plc-data`

## Endpoints

### 1. Read Single Value
**GET** `/{type}/{address}`

Fetches the value of a single PLC variable.

- **Parameters**:
  - `type`: The **C# / .NET data type** to interpret the value as.
    - Valid values: `bool`, `byte`, `int`, `uint`, `short`, `ushort`, `long`, `ulong`, `float`, `double`, `string`.
    - **Note**: Ensure the C# type matches the underlying PLC type size (e.g., PLC `DINT` -> C# `int`, PLC `INT` -> C# `short`).

| C# Type | Common PLC Type |
| :--- | :--- |
| `bool` | BOOL |
| `byte` | BYTE |
| `short` | INT |
| `ushort` | UINT |
| `int` | DINT |
| `uint` | UDINT |
| `long` | LINT |
| `float` | REAL |
| `double` | LREAL |
| `string` | STRING / WSTRING |

  - `address`: PLC variable address (e.g., `MAIN.bStart`).

**Example**:
```bash
curl -X GET "https://localhost:5001/api/plc-data/int/MAIN.Counter" \
     -H "X-Api-Key: secret"
```

### 2. Write Single Value
**POST** `/{type}/{address}`

Writes a value to a single PLC variable.

- **Body**: JSON value matching the type.

**Example**:
```bash
curl -X POST "https://localhost:5001/api/plc-data/bool/MAIN.bStart" \
     -H "X-Api-Key: secret" \
     -H "Content-Type: application/json" \
     -d "true"
```

### 3. Bulk Read
**POST** `/bulk-read`

Reads multiple variables in a single request.

- **Body**: JSON object containing a list of items to read.

**Request Body Structure**:
```json
{
  "items": [
    { "address": "MAIN.Var1", "type": "int" },
    { "address": "MAIN.Var2", "type": "bool" }
  ]
}
```

**Response Structure**:
```json
{
  "MAIN.Var1": { "value": 123 },
  "MAIN.Var2": { "error": "Variable not found" }
}
```

**Example**:
```bash
curl -X POST "https://localhost:5001/api/plc-data/bulk-read" \
     -H "X-Api-Key: secret" \
     -H "Content-Type: application/json" \
     -d '{ "items": [
           { "address": "MAIN.Counter", "type": "int" },
           { "address": "MAIN.bStart", "type": "bool" }
         ] }'
```
