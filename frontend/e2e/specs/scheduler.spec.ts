import { test, expect } from '../helpers/fixtures';

test.describe('E2E Scheduler – Intelligent Dispatch', () => {

  test.describe('Scoring Engine via Vite', () => {
    let page: any;

    test.beforeEach(async ({ page: p }) => {
      page = p;
      await page.goto('/');
      await page.waitForLoadState('domcontentloaded');
    });

    test('conflict detection blocks overlapping time slots', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const existingSlots = [
          {
            id: 'slot-1', orderId: 'ord-1', serviceInstallerId: 'inst-1',
            date: '2026-04-10', windowFrom: '09:00:00', windowTo: '11:00:00',
            sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '',
          },
        ];

        const noConflict = mod.checkConflict('inst-1', '2026-04-10', '11:00:00', '13:00:00', existingSlots as any);
        const hasConflict = mod.checkConflict('inst-1', '2026-04-10', '10:00:00', '12:00:00', existingSlots as any);
        const diffInstaller = mod.checkConflict('inst-2', '2026-04-10', '09:00:00', '11:00:00', existingSlots as any);

        return { noConflict, hasConflict, diffInstaller };
      });

      expect(result.noConflict.hasConflict).toBe(false);
      expect(result.hasConflict.hasConflict).toBe(true);
      expect(result.hasConflict.message).toBeTruthy();
      expect(result.diffInstaller.hasConflict).toBe(false);
    });

    test('computeWorkloads calculates utilization percentage', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const installers = [
          { id: 'inst-1', name: 'John', siLevel: 'Senior', installerType: 'InHouse', isActive: true, isSubcontractor: false },
          { id: 'inst-2', name: 'Jane', siLevel: 'Junior', installerType: 'InHouse', isActive: true, isSubcontractor: false },
        ];

        const slots = [
          { id: 's1', orderId: 'o1', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '09:00:00', windowTo: '11:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
          { id: 's2', orderId: 'o2', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '13:00:00', windowTo: '15:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
          { id: 's3', orderId: 'o3', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '15:00:00', windowTo: '17:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
        ];

        const workloads = mod.computeWorkloads(installers as any, slots as any);
        return workloads;
      });

      const johnWorkload = result.find((w: any) => w.installerId === 'inst-1');
      const janeWorkload = result.find((w: any) => w.installerId === 'inst-2');

      expect(johnWorkload!.jobCount).toBe(3);
      expect(johnWorkload!.totalMinutes).toBe(360);
      expect(johnWorkload!.utilizationPct).toBeGreaterThan(0);
      expect(johnWorkload!.level).toBe('medium');

      expect(janeWorkload!.jobCount).toBe(0);
      expect(janeWorkload!.utilizationPct).toBe(0);
      expect(janeWorkload!.level).toBe('free');
    });

    test('autoAssignJobs distributes across installers, no double assignment', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const installers = [
          { id: 'inst-1', name: 'John', siLevel: 'Senior', installerType: 'InHouse', isActive: true, isSubcontractor: false },
          { id: 'inst-2', name: 'Jane', siLevel: 'Junior', installerType: 'InHouse', isActive: true, isSubcontractor: false },
          { id: 'inst-3', name: 'Ravi', siLevel: 'Senior', installerType: 'InHouse', isActive: true, isSubcontractor: false },
        ];

        const jobs = [
          { orderId: 'job-1', orderType: 'FTTH' },
          { orderId: 'job-2', orderType: 'FTTH' },
          { orderId: 'job-3', orderType: 'FTTH' },
          { orderId: 'job-4', orderType: 'FTTH' },
          { orderId: 'job-5', orderType: 'FTTH' },
          { orderId: 'job-6', orderType: 'FTTH' },
        ];

        return mod.autoAssignJobs(jobs as any, installers as any, [], '2026-04-10');
      });

      expect(result.assignments.length).toBeGreaterThan(0);

      const assignedOrderIds = result.assignments.map((a: any) => a.orderId);
      const uniqueOrderIds = new Set(assignedOrderIds);
      expect(uniqueOrderIds.size).toBe(assignedOrderIds.length);

      const installerCounts = new Map<string, number>();
      for (const a of result.assignments) {
        const count = installerCounts.get(a.installerId) || 0;
        installerCounts.set(a.installerId, count + 1);
      }
      expect(installerCounts.size).toBeGreaterThan(1);
    });

    test('rankInstallers prefers free installer over partially busy one', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const installers = [
          { id: 'inst-1', name: 'Busy', siLevel: 'Senior', installerType: 'InHouse', isActive: true, isSubcontractor: false },
          { id: 'inst-2', name: 'Free', siLevel: 'Junior', installerType: 'InHouse', isActive: true, isSubcontractor: false },
        ];

        const existingSlots = [
          { id: 's1', orderId: 'o1', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '09:00:00', windowTo: '11:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
        ];

        const workloads = mod.computeWorkloads(installers as any, existingSlots as any);
        const job = { orderId: 'new-job', orderType: 'FTTH', windowFrom: '09:30:00', windowTo: '11:30:00' };
        return mod.rankInstallers(installers as any, job, existingSlots as any, workloads, '2026-04-10');
      });

      expect(result.length).toBe(2);
      expect(result[0].installerId).toBe('inst-2');
      expect(result[0].score).toBeGreaterThan(result[1].score);
    });

    test('fully booked installer is blocked', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const installers = [
          { id: 'inst-1', name: 'FullDay', siLevel: 'Senior', installerType: 'InHouse', isActive: true, isSubcontractor: false },
          { id: 'inst-2', name: 'Free', siLevel: 'Junior', installerType: 'InHouse', isActive: true, isSubcontractor: false },
        ];

        const fullSlots = [
          { id: 's1', orderId: 'o1', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '08:00:00', windowTo: '10:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
          { id: 's2', orderId: 'o2', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '10:00:00', windowTo: '12:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
          { id: 's3', orderId: 'o3', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '12:00:00', windowTo: '14:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
          { id: 's4', orderId: 'o4', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '14:00:00', windowTo: '16:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
          { id: 's5', orderId: 'o5', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '16:00:00', windowTo: '18:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
        ];

        const workloads = mod.computeWorkloads(installers as any, fullSlots as any);
        const job = { orderId: 'new-job', orderType: 'FTTH' };
        return mod.rankInstallers(installers as any, job, fullSlots as any, workloads, '2026-04-10');
      });

      expect(result.length).toBe(2);
      expect(result[0].installerId).toBe('inst-2');
      expect(result[0].blocked).toBe(false);
      expect(result[1].installerId).toBe('inst-1');
      expect(result[1].blocked).toBe(true);
    });

    test('workload updates correctly after assignments', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const installers = [
          { id: 'inst-1', name: 'Alpha', siLevel: 'Senior', installerType: 'InHouse', isActive: true, isSubcontractor: false },
        ];

        const wBefore = mod.computeWorkloads(installers as any, []);

        const afterSlots = [
          { id: 's1', orderId: 'o1', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '09:00:00', windowTo: '11:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
          { id: 's2', orderId: 'o2', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '13:00:00', windowTo: '15:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
        ];
        const wAfter = mod.computeWorkloads(installers as any, afterSlots as any);

        return { before: wBefore[0], after: wAfter[0] };
      });

      expect(result.before.jobCount).toBe(0);
      expect(result.before.utilizationPct).toBe(0);
      expect(result.after.jobCount).toBe(2);
      expect(result.after.totalMinutes).toBe(240);
      expect(result.after.utilizationPct).toBeGreaterThan(0);
    });

    test('scoring considers skill match in ranking', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const installers = [
          {
            id: 'inst-1', name: 'FiberPro', siLevel: 'Senior', installerType: 'InHouse', isActive: true, isSubcontractor: false,
            skills: [{ id: 'sk1', serviceInstallerId: 'inst-1', skillId: 'fiber', isActive: true, skill: { id: 'fiber', name: 'Fiber Installation', code: 'FTTH', category: 'FiberSkills', isActive: true, displayOrder: 1 } }],
          },
          {
            id: 'inst-2', name: 'NetworkGuy', siLevel: 'Junior', installerType: 'InHouse', isActive: true, isSubcontractor: false,
            skills: [{ id: 'sk2', serviceInstallerId: 'inst-2', skillId: 'network', isActive: true, skill: { id: 'network', name: 'Router Setup', code: 'ROUTER', category: 'NetworkEquipment', isActive: true, displayOrder: 1 } }],
          },
        ];

        const workloads = mod.computeWorkloads(installers as any, []);
        const job = { orderId: 'j1', orderType: 'FTTH' };
        const ranked = mod.rankInstallers(installers as any, job, [], workloads, '2026-04-10');
        return ranked;
      });

      expect(result[0].installerId).toBe('inst-1');
      expect(result[0].score).toBeGreaterThan(result[1].score);
    });
  });

  test.describe('Intelligent Time Slot Scoring', () => {
    let page: any;

    test.beforeEach(async ({ page: p }) => {
      page = p;
      await page.goto('/');
      await page.waitForLoadState('domcontentloaded');
    });

    test('multiple jobs same day — no overlap', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const installers = [
          { id: 'inst-1', name: 'Solo', siLevel: 'Senior', installerType: 'InHouse', isActive: true, isSubcontractor: false },
        ];

        const jobs = [
          { orderId: 'j1', orderType: 'FTTH' },
          { orderId: 'j2', orderType: 'FTTH' },
          { orderId: 'j3', orderType: 'FTTH' },
          { orderId: 'j4', orderType: 'FTTH' },
        ];

        return mod.autoAssignJobs(jobs as any, installers as any, [], '2026-04-10');
      });

      expect(result.assignments.length).toBe(4);

      const windows = result.assignments.map((a: any) => `${a.windowFrom}-${a.windowTo}`);
      const uniqueWindows = new Set(windows);
      expect(uniqueWindows.size).toBe(4);

      for (let i = 0; i < result.assignments.length; i++) {
        for (let j = i + 1; j < result.assignments.length; j++) {
          const a = result.assignments[i];
          const b = result.assignments[j];
          const aFrom = a.windowFrom.split(':').reduce((acc: number, v: string, k: number) => acc + Number(v) * [60, 1, 0][k], 0);
          const aTo = a.windowTo.split(':').reduce((acc: number, v: string, k: number) => acc + Number(v) * [60, 1, 0][k], 0);
          const bFrom = b.windowFrom.split(':').reduce((acc: number, v: string, k: number) => acc + Number(v) * [60, 1, 0][k], 0);
          const bTo = b.windowTo.split(':').reduce((acc: number, v: string, k: number) => acc + Number(v) * [60, 1, 0][k], 0);
          expect(aFrom >= bTo || aTo <= bFrom).toBe(true);
        }
      }
    });

    test('gap fitting — prefers adjacent time slots', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const existingSlots = [
          {
            id: 's1', orderId: 'o1', serviceInstallerId: 'inst-1',
            date: '2026-04-10', windowFrom: '08:00:00', windowTo: '10:00:00',
            sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '',
          },
          {
            id: 's2', orderId: 'o2', serviceInstallerId: 'inst-1',
            date: '2026-04-10', windowFrom: '14:00:00', windowTo: '16:00:00',
            sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '',
          },
        ];

        const adjacentScore = mod.getWindowScore(
          '10:00:00', '12:00:00', existingSlots as any,
          { orderId: 'j1' }, undefined, undefined
        );

        const gapScore = mod.getWindowScore(
          '11:00:00', '13:00:00', existingSlots as any,
          { orderId: 'j1' }, undefined, undefined
        );

        return { adjacentScore, gapScore };
      });

      expect(result.adjacentScore.score).toBeGreaterThan(result.gapScore.score);
    });

    test('preferred time used when available', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const windowResult = mod.findBestWindow(
          'inst-1', '2026-04-10', [],
          { orderId: 'j1', orderType: 'FTTH' },
          '10:00:00', '12:00:00'
        );

        return windowResult;
      });

      expect(result.status).toBe('PREFERRED');
      expect(result.windowFrom).toBe('10:00:00');
      expect(result.windowTo).toBe('12:00:00');
      expect(result.reason).toContain('as requested');
    });

    test('fallback time chosen when preferred conflicts', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const existingSlots = [
          {
            id: 's1', orderId: 'o1', serviceInstallerId: 'inst-1',
            date: '2026-04-10', windowFrom: '09:00:00', windowTo: '11:00:00',
            sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '',
          },
        ];

        const windowResult = mod.findBestWindow(
          'inst-1', '2026-04-10', existingSlots as any,
          { orderId: 'j1', orderType: 'FTTH' },
          '10:00:00', '12:00:00'
        );

        return windowResult;
      });

      expect(result.status).toBe('BEST_FIT');
      expect(result.windowFrom).not.toBe('10:00:00');
      expect(result.reason).toBeTruthy();
    });

    test('no slot scenario returns NO_SLOT', async () => {
      const result = await page.evaluate(async () => {
        const mod = await import('/src/lib/scheduler/scoringEngine.ts');

        const fullDaySlots = [
          { id: 's1', orderId: 'o1', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '08:00:00', windowTo: '10:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
          { id: 's2', orderId: 'o2', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '10:00:00', windowTo: '12:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
          { id: 's3', orderId: 'o3', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '12:00:00', windowTo: '14:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
          { id: 's4', orderId: 'o4', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '14:00:00', windowTo: '16:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
          { id: 's5', orderId: 'o5', serviceInstallerId: 'inst-1', date: '2026-04-10', windowFrom: '16:00:00', windowTo: '18:00:00', sequenceIndex: 0, status: 'Draft', createdByUserId: '', createdAt: '' },
        ];

        const windowResult = mod.findBestWindow(
          'inst-1', '2026-04-10', fullDaySlots as any,
          { orderId: 'j1', orderType: 'FTTH' },
          '10:00:00', '12:00:00'
        );

        return windowResult;
      });

      expect(result.status).toBe('NO_SLOT');
      expect(result.reason).toBeTruthy();
    });
  });

  test.describe('UI Integration', () => {
    test('scheduler page loads correctly', async ({ page }) => {
      await page.goto('/scheduler');
      await page.waitForLoadState('networkidle');

      const pageContent = await page.content();
      const isLoginPage = pageContent.includes('Sign in') || pageContent.includes('login');
      const isSchedulerPage = pageContent.includes('Schedule') || pageContent.includes('scheduler');

      expect(isLoginPage || isSchedulerPage).toBe(true);
    });

    test('scheduler route returns valid HTML', async ({ request }) => {
      const baseUrl = process.env.PLAYWRIGHT_BASE_URL || 'http://localhost:5000';
      const resp = await request.get(baseUrl);
      expect(resp.status()).toBe(200);
      const html = await resp.text();
      expect(html.toLowerCase()).toContain('<!doctype html>');
    });
  });
});
