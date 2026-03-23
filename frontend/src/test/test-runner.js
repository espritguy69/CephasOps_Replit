/**
 * Test Runner Script
 * 
 * This script runs all workflow-related tests and generates a test report
 * Run with: npm run test:workflow
 */

import { describe, it } from 'vitest';

// Import all test files
import './api/workflow.test.js';
import './api/workflowDefinitions.test.js';
import './components/workflow/WorkflowTransitionButton.test.jsx';
import './pages/workflow/WorkflowDefinitionsPage.test.jsx';
import './test/navigation.test.jsx';
import './pages/orders/OrderDetailPage.test.jsx';

describe('Workflow Test Suite', () => {
  it('should run all workflow tests', () => {
    // All tests are imported above and will run automatically
    expect(true).toBe(true);
  });
});
