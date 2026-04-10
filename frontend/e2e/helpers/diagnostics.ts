import type { Page, TestInfo } from '@playwright/test';

export interface DiagnosticEntry {
  type: 'console-error' | 'network-failure' | 'api-error' | 'exception';
  message: string;
  url?: string;
  status?: number;
  timestamp: number;
}

export class TestDiagnostics {
  private entries: DiagnosticEntry[] = [];
  private page: Page;

  constructor(page: Page) {
    this.page = page;
    this.attach();
  }

  private attach(): void {
    this.page.on('console', (msg) => {
      if (msg.type() === 'error') {
        this.entries.push({
          type: 'console-error',
          message: msg.text(),
          timestamp: Date.now(),
        });
      }
    });

    this.page.on('pageerror', (err) => {
      this.entries.push({
        type: 'exception',
        message: err.message,
        timestamp: Date.now(),
      });
    });

    this.page.on('response', (response) => {
      const status = response.status();
      if (status >= 500) {
        this.entries.push({
          type: 'api-error',
          message: `${response.request().method()} ${response.url()} → ${status}`,
          url: response.url(),
          status,
          timestamp: Date.now(),
        });
      }
    });

    this.page.on('requestfailed', (request) => {
      this.entries.push({
        type: 'network-failure',
        message: `${request.method()} ${request.url()} — ${request.failure()?.errorText ?? 'unknown'}`,
        url: request.url(),
        timestamp: Date.now(),
      });
    });
  }

  getEntries(): DiagnosticEntry[] {
    return [...this.entries];
  }

  hasErrors(): boolean {
    return this.entries.length > 0;
  }

  getSummary(): string {
    if (this.entries.length === 0) return 'No diagnostic issues captured.';
    const grouped = {
      'console-error': 0,
      'network-failure': 0,
      'api-error': 0,
      'exception': 0,
    };
    for (const e of this.entries) {
      grouped[e.type]++;
    }
    const parts: string[] = [];
    if (grouped.exception > 0) parts.push(`${grouped.exception} unhandled exception(s)`);
    if (grouped['api-error'] > 0) parts.push(`${grouped['api-error']} API 5xx error(s)`);
    if (grouped['network-failure'] > 0) parts.push(`${grouped['network-failure']} network failure(s)`);
    if (grouped['console-error'] > 0) parts.push(`${grouped['console-error']} console error(s)`);
    return `ROOT CAUSE SUMMARY: ${parts.join(', ')}`;
  }

  getDetailedReport(): string {
    if (this.entries.length === 0) return '';
    const lines = [this.getSummary(), '', '--- Diagnostic Entries ---'];
    for (const e of this.entries) {
      lines.push(`[${e.type}] ${e.message}${e.url ? ` (${e.url})` : ''}`);
    }
    return lines.join('\n');
  }

  async attachToTestInfo(testInfo: TestInfo): Promise<void> {
    if (this.entries.length === 0) return;
    const report = this.getDetailedReport();
    await testInfo.attach('diagnostics-report', {
      body: report,
      contentType: 'text/plain',
    });
  }

  clear(): void {
    this.entries.length = 0;
  }
}

export interface TimingEntry {
  url: string;
  method: string;
  durationMs: number;
  status: number;
}

export class ApiTimingCollector {
  private timings: TimingEntry[] = [];
  private pendingRequests = new Map<string, number>();

  constructor(page: Page) {
    page.on('request', (request) => {
      const url = request.url();
      if (url.includes('/api/')) {
        this.pendingRequests.set(`${request.method()}-${url}`, Date.now());
      }
    });

    page.on('response', (response) => {
      const key = `${response.request().method()}-${response.url()}`;
      const startTime = this.pendingRequests.get(key);
      if (startTime !== undefined) {
        this.timings.push({
          url: response.url(),
          method: response.request().method(),
          durationMs: Date.now() - startTime,
          status: response.status(),
        });
        this.pendingRequests.delete(key);
      }
    });
  }

  getTimings(): TimingEntry[] {
    return [...this.timings];
  }

  getSlowRequests(thresholdMs = 5000): TimingEntry[] {
    return this.timings.filter(t => t.durationMs > thresholdMs);
  }

  getFailedRequests(): TimingEntry[] {
    return this.timings.filter(t => t.status >= 500);
  }

  getAverageDuration(): number {
    if (this.timings.length === 0) return 0;
    return this.timings.reduce((sum, t) => sum + t.durationMs, 0) / this.timings.length;
  }

  async attachToTestInfo(testInfo: TestInfo): Promise<void> {
    const slow = this.getSlowRequests();
    const failed = this.getFailedRequests();
    if (slow.length === 0 && failed.length === 0) return;

    const lines: string[] = ['--- API Timing Report ---'];
    if (slow.length > 0) {
      lines.push(`\nSlow requests (>5s): ${slow.length}`);
      for (const s of slow) {
        lines.push(`  ${s.method} ${s.url} → ${s.durationMs}ms (${s.status})`);
      }
    }
    if (failed.length > 0) {
      lines.push(`\nFailed requests (5xx): ${failed.length}`);
      for (const f of failed) {
        lines.push(`  ${f.method} ${f.url} → ${f.status} (${f.durationMs}ms)`);
      }
    }
    lines.push(`\nAverage API response: ${Math.round(this.getAverageDuration())}ms`);
    lines.push(`Total API calls tracked: ${this.timings.length}`);

    await testInfo.attach('api-timing-report', {
      body: lines.join('\n'),
      contentType: 'text/plain',
    });
  }
}
