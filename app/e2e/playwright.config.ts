import { defineConfig, devices } from '@playwright/test';

const BASE_URL = 'http://localhost:4200';
const API_URL = 'http://localhost:5012';
const CI = process.env['CI'] !== undefined;

const ambiente = Object.fromEntries(
  Object.entries(process.env).filter(
    (entrada): entrada is [string, string] => entrada[1] !== undefined,
  ),
);

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,
  forbidOnly: CI,
  retries: CI ? 1 : 0,
  timeout: 90_000,
  expect: { timeout: 10_000 },
  reporter: CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: BASE_URL,
    locale: 'pt-BR',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'off',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: [
    {
      command: 'dotnet run --project ../backend/src/ColetaSeletiva.Api --no-launch-profile',
      url: `${API_URL}/health`,
      reuseExistingServer: false,
      timeout: 180_000,
      env: {
        ...ambiente,
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: API_URL,
        Sessao__OrigemPermitida: BASE_URL,
        LimitesDeTaxa__Login__PermitLimit: '1000',
        LimitesDeTaxa__Refresh__PermitLimit: '1000',
        LimitesDeTaxa__Cadastro__PermitLimit: '1000',
      },
    },
    {
      command: 'npm start -- --port 4200',
      cwd: '../frontend',
      url: BASE_URL,
      reuseExistingServer: false,
      timeout: 180_000,
      env: {
        ...ambiente,
        NG_CLI_ANALYTICS: 'false',
      },
    },
  ],
});
