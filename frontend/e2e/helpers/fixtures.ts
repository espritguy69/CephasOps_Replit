import { test as base, expect } from '@playwright/test';
import { TestDiagnostics, ApiTimingCollector } from './diagnostics';

type DiagnosticsFixtures = {
  diagnostics: TestDiagnostics;
  apiTiming: ApiTimingCollector;
};

export const test = base.extend<DiagnosticsFixtures>({
  diagnostics: async ({ page }, use, testInfo) => {
    const diag = new TestDiagnostics(page);
    await use(diag);
    if (testInfo.status !== testInfo.expectedStatus) {
      await diag.attachToTestInfo(testInfo);
      const summary = diag.getSummary();
      if (summary !== 'No diagnostic issues captured.') {
        console.error(`[DIAGNOSTICS] ${testInfo.title}: ${summary}`);
      }
    }
  },

  apiTiming: async ({ page }, use, testInfo) => {
    const timing = new ApiTimingCollector(page);
    await use(timing);
    await timing.attachToTestInfo(testInfo);
    const slow = timing.getSlowRequests();
    if (slow.length > 0) {
      console.warn(`[SLOW API] ${testInfo.title}: ${slow.length} request(s) exceeded 5s threshold`);
    }
  },
});

export { expect };
