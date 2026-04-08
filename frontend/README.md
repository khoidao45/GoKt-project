# Gokt Frontend MVP

Frontend MVP for the Gokt ride-hailing backend.

## Included Flows

- Login with JWT access token
- Fetch current user profile
- Estimate ride price
- Create ride request
- View active ride and trip snapshot
- View trip history and notifications

## Environment

Copy `.env.example` to `.env` and set API base URL.

Example:

```
VITE_API_BASE_URL=http://localhost:8080/api/v1
```

## Run

Install and run dev server:

```
npm install
npm run dev
```

Build for production:

```
npm run build
```

## Notes

- Backend must be running and reachable from your browser.
- The app stores access token in localStorage for MVP speed.
- Refresh token remains server-managed via HttpOnly cookie.
