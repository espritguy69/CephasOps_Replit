import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { MainLayout } from './components/layout/MainLayout';
import { JobsListPage } from './pages/jobs/JobsListPage';
import { JobDetailPage } from './pages/jobs/JobDetailPage';
import { LoginPage } from './pages/auth/LoginPage';
import { DashboardPage } from './pages/dashboard/DashboardPage';
import { EarningsPage } from './pages/earnings/EarningsPage';
import { OrdersTrackingPage } from './pages/orders/OrdersTrackingPage';
import { MaterialsScanPage } from './pages/materials/MaterialsScanPage';
import { MaterialsTrackingPage } from './pages/materials/MaterialsTrackingPage';
import { MaterialReturnsPage } from './pages/materials/MaterialReturnsPage';
import { ServiceInstallersPage } from './pages/service-installers/ServiceInstallersPage';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { SubconRoute } from './components/auth/SubconRoute';

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/*"
        element={
          <ProtectedRoute>
            <MainLayout>
              <Routes>
                <Route path="/" element={<Navigate to="/dashboard" replace />} />
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/jobs" element={<JobsListPage />} />
                <Route path="/jobs/:orderId" element={<JobDetailPage />} />
                <Route path="/orders" element={<OrdersTrackingPage />} />
                <Route path="/materials/scan" element={<MaterialsScanPage />} />
                <Route path="/materials/tracking" element={<MaterialsTrackingPage />} />
                <Route path="/materials/returns" element={<MaterialReturnsPage />} />
                <Route path="/service-installers" element={<ServiceInstallersPage />} />
                <Route 
                  path="/earnings" 
                  element={
                    <SubconRoute>
                      <EarningsPage />
                    </SubconRoute>
                  } 
                />
                <Route path="*" element={<Navigate to="/dashboard" replace />} />
              </Routes>
            </MainLayout>
          </ProtectedRoute>
        }
      />
    </Routes>
  );
}

export default App;

