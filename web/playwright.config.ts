import { defineConfig, devices } from "@playwright/test";

/**
 * E2E against a running stack: the API (with its seeded users) behind this Next app.
 *   E2E_BASE_URL       Next URL (default http://localhost:3000)
 *   E2E_ADMIN_EMAIL / E2E_ADMIN_PASSWORD, E2E_SUPERADMIN_EMAIL / E2E_SUPERADMIN_PASSWORD
 *                      default to the Development seed of the API (appsettings.Development.json).
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? "github" : "list",
  use: {
    baseURL: process.env.E2E_BASE_URL ?? "http://localhost:3000",
    locale: "es-ES",
    timezoneId: "Europe/Madrid",
    trace: "retain-on-failure",
  },
  projects: [
    { name: "movil", use: { ...devices["Pixel 7"] } },
    { name: "escritorio", use: { ...devices["Desktop Chrome"], viewport: { width: 1280, height: 800 } } },
  ],
});
