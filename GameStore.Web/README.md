# GameStore.Web

Angular frontend for the GameStore API.

## Run

1. Install dependencies:

```bash
npm install
```

2. Start the API from `GameStore.Api`:

```bash
dotnet run
```

3. Start Angular from `GameStore.Web`:

```bash
npm start
```

Angular uses `proxy.conf.json` to forward `/api/*` to `http://localhost:5059`.
