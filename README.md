# Spora Leuven website

## Technical documentation

GitHub Wiki-style technical documentation is available in [`/wiki/Home.md`](wiki/Home.md).

## Commands

All commands are run from the repository root:

| Command         | Action                                  |
| :-------------- | :-------------------------------------- |
| `npm install`   | Install dependencies                    |
| `npm run dev`   | Start the local Astro development server |
| `npm run build` | Build the production site into `dist/`  |
| `npm run preview` | Preview the production build locally  |

## Local calendar API

The calendar page requests `/api/calendar/2627/vlm/H2`. For local development before the updated API is deployed, install the .NET 8 runtime and Azure Functions Core Tools, start the API from `api/` with `func start`, then set `API_PROXY_TARGET=http://localhost:7071` before starting Astro. The dev server proxies `/api` to that address, keeping browser requests same-origin. Without this setting it proxies to `https://sporaleuven.be`, which will return 404 for the new league route until deployment.

`GET /api/teams/{season}/{league}/{competition}` returns an alphabetically sorted array of distinct home and away team names from the matching calendar, for example `/api/teams/2627/vlm/H2`.

`GET /api/ranking/{season}/{league}/{competition}` returns the overall ranking for the matching competition, for example `/api/ranking/2627/vlm/H2`.

## Visitor analytics

This site uses a centralized [Umami](https://umami.is/) script include in `/src/components/Analytics.astro`, which is mounted once from `/src/layouts/BaseLayout.astro` so every page is tracked consistently.

The public Umami configuration is centralized in `/src/consts.ts`, which reads:

- `UMAMI_WEBSITE_ID`: the configured Umami website identifier
- `PUBLIC_UMAMI_SCRIPT_URL` (optional): overrides the default script URL of `https://cloud.umami.is/script.js`

The deploy workflow forwards Umami-related values into the Astro build. The current site code consumes the optional script URL override and keeps the website identifier in source control.
