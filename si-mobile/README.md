# SI Mobile (CephasOps Service Installer)

React Native + Expo app for field installers. See `IMPLEMENTATION_NOTES.md` and `GAPS_AND_REFERENCE.md` for design and API details.

## Audit in browser first (before Expo)

To evaluate the app in the browser before testing on device:

1. Run **`npm run web`** and use the tab that opens.
2. Follow **`docs/WEB_AUDIT_CHECKLIST.md`** – login, navigation, Home, Jobs, Job Detail, workflow, Earnings, Profile, Scan, and API/errors.
3. When the checklist passes, switch to **`npm start`** and Expo Go for device testing.

## Run on your laptop (browser)

1. **Start the app in web mode** (this opens the actual app in a browser):
   ```bash
   cd si-mobile
   npm run web
   ```
2. Use the **browser tab that opens automatically** (or the URL shown in the terminal, e.g. `http://localhost:8081`).

**Important:** Run `npm run web`, not just `npm start`. If you run `npm start` and then open `http://localhost:8081` (or `http://192.168.1.9:8081`) in the browser yourself, you will see raw JSON (the Expo manifest) instead of the app. Always use **`npm run web`** for laptop/browser use.

## Run on phone (Expo Go)

1. Start the backend API and note your laptop’s IP (e.g. `192.168.1.9`).
2. Set the API URL and start Expo:
   ```bash
   set EXPO_PUBLIC_API_BASE_URL=http://192.168.1.9:5000/api
   npm start
   ```
3. Scan the QR code with Expo Go (iOS/Android). Do **not** open the same URL in your laptop browser if you want the app UI—use `npm run web` for that.

## API

- Default API base: `http://localhost:5000/api`.
- Override with `EXPO_PUBLIC_API_BASE_URL` (see `.env.example`).
