import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Users, Search, User, Phone, Mail, Briefcase, CheckCircle, XCircle, TrendingUp } from 'lucide-react';
import { Card, TextInput, Skeleton, EmptyState } from '../../components/ui';
import { useQuery } from '@tanstack/react-query';
import { getAllServiceInstallers } from '../../api/service-installers';
import { getAllOrders } from '../../api/orders';
import { useAuth } from '../../contexts/AuthContext';

export function ServiceInstallersPage() {
  const { user, isAdmin } = useAuth();
  const [searchTerm, setSearchTerm] = useState('');
  const [filterActive, setFilterActive] = useState<boolean | null>(null);

  // Only show this page to admins
  if (!isAdmin) {
    return (
      <div className="p-4">
        <EmptyState
          title="Access Denied"
          description="This page is only available to administrators"
        />
      </div>
    );
  }

  // Fetch all service installers
  const { data: serviceInstallers, isLoading: isLoadingSIs } = useQuery({
    queryKey: ['serviceInstallers', filterActive],
    queryFn: () => getAllServiceInstallers({ isActive: filterActive ?? undefined }),
    enabled: !!user?.id && isAdmin,
  });

  // Fetch all orders to calculate SI stats
  const { data: orders } = useQuery({
    queryKey: ['allOrdersForSIStats'],
    queryFn: () => getAllOrders({}),
    enabled: !!user?.id && isAdmin,
  });

  // Filter service installers
  const filteredSIs = serviceInstallers?.filter((si: any) => {
    if (!searchTerm) return true;
    const searchLower = searchTerm.toLowerCase();
    return (
      si.name?.toLowerCase().includes(searchLower) ||
      si.employeeId?.toLowerCase().includes(searchLower) ||
      si.phone?.toLowerCase().includes(searchLower) ||
      si.email?.toLowerCase().includes(searchLower) ||
      si.siLevel?.toLowerCase().includes(searchLower) ||
      si.departmentName?.toLowerCase().includes(searchLower)
    );
  }) || [];

  // Calculate stats for each SI
  const siStats = filteredSIs.map((si: any) => {
    const siOrders = orders?.filter((o: any) => 
      o.assignedSiId === si.id || o.assignedSiName === si.name
    ) || [];
    
    const totalJobs = siOrders.length;
    const completedJobs = siOrders.filter((o: any) => 
      o.status === 'Completed' || o.status === 'OrderCompleted'
    ).length;
    const pendingJobs = siOrders.filter((o: any) => 
      o.status === 'Pending' || o.status === 'Assigned' || o.status === 'OnTheWay'
    ).length;
    const inProgressJobs = siOrders.filter((o: any) => 
      o.status === 'MetCustomer' || o.status === 'Installing'
    ).length;

    return {
      ...si,
      totalJobs,
      completedJobs,
      pendingJobs,
      inProgressJobs,
      completionRate: totalJobs > 0 ? (completedJobs / totalJobs) * 100 : 0,
    };
  });

  if (isLoadingSIs) {
    return (
      <div className="p-4 space-y-4">
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-48" />
        </div>
        <Card className="p-4">
          <div className="space-y-4">
            <Skeleton className="h-10 w-full" />
            <div className="flex gap-2">
              <Skeleton className="h-9 w-12 rounded-md" />
              <Skeleton className="h-9 w-14 rounded-md" />
              <Skeleton className="h-9 w-16 rounded-md" />
            </div>
          </div>
        </Card>
        {[1, 2, 3].map((i) => (
          <Card key={i} className="p-4">
            <div className="flex justify-between items-start mb-3">
              <div className="flex-1 space-y-2">
                <Skeleton className="h-6 w-40" />
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-32" />
              </div>
            </div>
            <div className="flex gap-4 mb-3">
              <Skeleton className="h-4 w-28" />
              <Skeleton className="h-4 w-36" />
            </div>
            <div className="grid grid-cols-2 gap-2 pt-3 border-t border-border">
              <div><Skeleton className="h-3 w-16 mb-1" /><Skeleton className="h-5 w-8" /></div>
              <div><Skeleton className="h-3 w-16 mb-1" /><Skeleton className="h-5 w-8" /></div>
              <div><Skeleton className="h-3 w-16 mb-1" /><Skeleton className="h-5 w-8" /></div>
              <div><Skeleton className="h-3 w-16 mb-1" /><Skeleton className="h-5 w-8" /></div>
            </div>
            <div className="mt-3 pt-3 border-t border-border">
              <Skeleton className="h-4 w-24" />
            </div>
          </Card>
        ))}
      </div>
    );
  }

  return (
    <div className="p-4 space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-bold text-foreground flex items-center gap-2">
          <Users className="h-6 w-6" />
          Service Installers
        </h2>
      </div>

      {/* Filters */}
      <Card className="p-4">
        <div className="space-y-4">
          <div className="flex gap-2">
            <div className="flex-1 relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <TextInput
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Search by name, employee ID, phone, email..."
                className="pl-10"
              />
            </div>
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => setFilterActive(null)}
              className={`px-4 py-2 text-sm rounded-md ${
                filterActive === null
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted text-muted-foreground'
              }`}
            >
              All
            </button>
            <button
              onClick={() => setFilterActive(true)}
              className={`px-4 py-2 text-sm rounded-md ${
                filterActive === true
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted text-muted-foreground'
              }`}
            >
              Active
            </button>
            <button
              onClick={() => setFilterActive(false)}
              className={`px-4 py-2 text-sm rounded-md ${
                filterActive === false
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted text-muted-foreground'
              }`}
            >
              Inactive
            </button>
          </div>
        </div>
      </Card>

      {/* Service Installers List */}
      {filteredSIs.length === 0 ? (
        <EmptyState
          title="No service installers found"
          description={searchTerm ? "Try adjusting your search" : "No service installers available"}
        />
      ) : (
        <div className="space-y-2">
          {siStats.map((si: any) => (
            <Card key={si.id} className="p-4">
              <div className="flex justify-between items-start mb-3">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1">
                    <h3 className="text-lg font-semibold">{si.name}</h3>
                    {si.isActive ? (
                      <CheckCircle className="h-4 w-4 text-green-600" />
                    ) : (
                      <XCircle className="h-4 w-4 text-red-600" />
                    )}
                  </div>
                  {si.employeeId && (
                    <p className="text-sm text-muted-foreground">ID: {si.employeeId}</p>
                  )}
                  {si.siLevel && (
                    <p className="text-sm text-muted-foreground">Level: {si.siLevel}</p>
                  )}
                  {si.departmentName && (
                    <p className="text-sm text-muted-foreground">Department: {si.departmentName}</p>
                  )}
                  {si.isSubcontractor && (
                    <span className="inline-block mt-1 px-2 py-1 text-xs bg-blue-100 text-blue-800 rounded-full">
                      Subcontractor
                    </span>
                  )}
                </div>
              </div>

              {/* Contact Info */}
              <div className="flex flex-wrap gap-4 mb-3 text-sm">
                {si.phone && (
                  <div className="flex items-center gap-1 text-muted-foreground">
                    <Phone className="h-4 w-4" />
                    <span>{si.phone}</span>
                  </div>
                )}
                {si.email && (
                  <div className="flex items-center gap-1 text-muted-foreground">
                    <Mail className="h-4 w-4" />
                    <span>{si.email}</span>
                  </div>
                )}
              </div>

              {/* Job Statistics */}
              <div className="grid grid-cols-2 gap-2 pt-3 border-t border-border">
                <div>
                  <p className="text-xs text-muted-foreground">Total Jobs</p>
                  <p className="text-lg font-semibold">{si.totalJobs}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Completed</p>
                  <p className="text-lg font-semibold text-green-600">{si.completedJobs}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Pending</p>
                  <p className="text-lg font-semibold text-yellow-600">{si.pendingJobs}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">In Progress</p>
                  <p className="text-lg font-semibold text-blue-600">{si.inProgressJobs}</p>
                </div>
              </div>

              {si.totalJobs > 0 && (
                <div className="mt-2 pt-2 border-t border-border">
                  <div className="flex items-center justify-between">
                    <span className="text-xs text-muted-foreground">Completion Rate</span>
                    <span className="text-sm font-semibold flex items-center gap-1">
                      <TrendingUp className="h-4 w-4" />
                      {si.completionRate.toFixed(1)}%
                    </span>
                  </div>
                  <div className="mt-1 w-full bg-muted rounded-full h-2">
                    <div
                      className="bg-primary h-2 rounded-full transition-all"
                      style={{ width: `${si.completionRate}%` }}
                    />
                  </div>
                </div>
              )}

              {/* View Orders Link */}
              <div className="mt-3 pt-3 border-t border-border">
                <Link
                  to={`/orders?assignedSiId=${si.id}`}
                  className="text-sm text-primary hover:underline flex items-center gap-1"
                >
                  <Briefcase className="h-4 w-4" />
                  View {si.totalJobs} {si.totalJobs === 1 ? 'Order' : 'Orders'}
                </Link>
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Summary Stats */}
      {filteredSIs.length > 0 && (
        <Card className="p-4">
          <h3 className="font-semibold mb-3">Summary</h3>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <p className="text-sm text-muted-foreground">Total SIs</p>
              <p className="text-2xl font-bold">{filteredSIs.length}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Active SIs</p>
              <p className="text-2xl font-bold text-green-600">
                {filteredSIs.filter((si: any) => si.isActive).length}
              </p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Total Jobs</p>
              <p className="text-2xl font-bold">
                {siStats.reduce((sum, si) => sum + si.totalJobs, 0)}
              </p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Completed Jobs</p>
              <p className="text-2xl font-bold text-green-600">
                {siStats.reduce((sum, si) => sum + si.completedJobs, 0)}
              </p>
            </div>
          </div>
        </Card>
      )}
    </div>
  );
}

