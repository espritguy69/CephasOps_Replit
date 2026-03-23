import React, { useState, useEffect } from 'react';
import { Plus, Trash2, Link2, ChevronDown, ChevronUp } from 'lucide-react';
import {
  getServiceProfileMappings,
  createServiceProfileMapping,
  deleteServiceProfileMapping,
  getServiceProfiles,
  type OrderCategoryServiceProfileDto,
  type ServiceProfileDto,
} from '../../api/serviceProfiles';
import { getOrderCategories } from '../../api/orderCategories';
import { LoadingSpinner, EmptyState, useToast, Button, Card, Modal, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const ServiceProfileMappingsPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [mappings, setMappings] = useState<OrderCategoryServiceProfileDto[]>([]);
  const [profiles, setProfiles] = useState<ServiceProfileDto[]>([]);
  const [orderCategories, setOrderCategories] = useState<{ id: string; name: string; code?: string }[]>([]);
  const [loading, setLoading] = useState(true);
  const [showGuide, setShowGuide] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [deleting, setDeleting] = useState<OrderCategoryServiceProfileDto | null>(null);
  const [form, setForm] = useState({ orderCategoryId: '', serviceProfileId: '' });

  const loadMappings = async () => {
    try {
      const data = await getServiceProfileMappings();
      setMappings(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load mappings');
    }
  };

  const loadProfiles = async () => {
    try {
      const data = await getServiceProfiles();
      setProfiles(Array.isArray(data) ? data : []);
    } catch {
      setProfiles([]);
    }
  };

  const loadOrderCategoriesList = async () => {
    try {
      const data = await getOrderCategories();
      setOrderCategories(
        (Array.isArray(data) ? data : []).map((c: { id: string; name: string; code?: string }) => ({
          id: c.id,
          name: c.name,
          code: c.code,
        }))
      );
    } catch {
      setOrderCategories([]);
    }
  };

  useEffect(() => {
    (async () => {
      setLoading(true);
      await Promise.all([loadMappings(), loadProfiles(), loadOrderCategoriesList()]);
      setLoading(false);
    })();
  }, []);

  const openCreate = () => {
    setForm({ orderCategoryId: '', serviceProfileId: '' });
    setShowModal(true);
  };

  const handleCreate = async () => {
    if (!form.orderCategoryId || !form.serviceProfileId) {
      showError('Select both Order Category and Service Profile.');
      return;
    }
    try {
      await createServiceProfileMapping({
        orderCategoryId: form.orderCategoryId,
        serviceProfileId: form.serviceProfileId,
      });
      showSuccess('Mapping created.');
      setShowModal(false);
      await loadMappings();
    } catch (err) {
      showError((err as Error).message || 'Create failed');
    }
  };

  const handleDelete = async (row: OrderCategoryServiceProfileDto) => {
    try {
      await deleteServiceProfileMapping(row.id);
      showSuccess('Mapping removed.');
      setDeleting(null);
      await loadMappings();
    } catch (err) {
      showError((err as Error).message || 'Delete failed');
    }
  };

  const mappedCategoryIds = new Set(mappings.map((m) => m.orderCategoryId));
  const unmappedCategories = orderCategories.filter((c) => !mappedCategoryIds.has(c.id));

  const columns: TableColumn<OrderCategoryServiceProfileDto>[] = [
    { key: 'orderCategoryCode', label: 'Order Category Code', render: (_, row) => row.orderCategoryCode ?? row.orderCategoryId },
    { key: 'orderCategoryName', label: 'Order Category' },
    { key: 'serviceProfileCode', label: 'Profile Code', render: (_, row) => row.serviceProfileCode ?? row.serviceProfileId },
    { key: 'serviceProfileName', label: 'Service Profile' },
    {
      key: 'actions',
      label: 'Actions',
      render: (_, row) => (
        <button onClick={() => setDeleting(row)} title="Remove mapping" className="text-red-600 hover:opacity-75">
          <Trash2 className="h-4 w-4" />
        </button>
      ),
    },
  ];

  if (loading) {
    return <LoadingSpinner message="Loading mappings..." fullPage />;
  }

  return (
    <PageShell
      title="Order Category → Service Profile Mappings"
      breadcrumbs={[
        { label: 'Settings', path: '/settings' },
        { label: 'GPON' },
        { label: 'Service Profile Mappings' },
      ]}
      actions={
        <Button onClick={openCreate} className="flex items-center gap-2" disabled={unmappedCategories.length === 0}>
          <Plus className="h-4 w-4" />
          Add mapping
        </Button>
      }
    >
      <div className="flex-1 p-2 max-w-5xl mx-auto space-y-2">
        <Card className="bg-muted/50 border-muted">
          <button
            onClick={() => setShowGuide(!showGuide)}
            className="w-full flex items-center justify-between px-3 py-2 text-left"
          >
            <div className="flex items-center gap-2">
              <Link2 className="h-4 w-4 text-primary" />
              <span className="font-medium text-sm">About mappings</span>
            </div>
            {showGuide ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
          </button>
          {showGuide && (
            <div className="px-3 pb-3 text-sm text-muted-foreground">
              <p>
                Each Order Category can be linked to at most one Service Profile. This links e.g. FTTH and FTTR to
                RESIDENTIAL_FIBER so that (in a future phase) pricing can target the profile. Payout behaviour is
                unchanged until engine integration.
              </p>
            </div>
          )}
        </Card>

        <Card>
          {mappings.length > 0 ? (
            <DataTable data={mappings} columns={columns} />
          ) : (
            <EmptyState
              title="No mappings"
              message="Create Service Profiles first, then add mappings here. Each Order Category can map to one profile."
              action={
                unmappedCategories.length > 0
                  ? { label: 'Add mapping', onClick: openCreate }
                  : undefined
              }
            />
          )}
        </Card>
      </div>

      <Modal
        open={showModal}
        onClose={() => setShowModal(false)}
        title="Add mapping"
        footer={
          <>
            <Button variant="outline" onClick={() => setShowModal(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreate} disabled={!form.orderCategoryId || !form.serviceProfileId}>
              Add
            </Button>
          </>
        }
      >
        <div className="space-y-3">
          <label className="block text-sm font-medium">
            Order Category
            <select
              className="mt-1 block w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              value={form.orderCategoryId}
              onChange={(e) => setForm((f) => ({ ...f, orderCategoryId: e.target.value }))}
            >
              <option value="">Select...</option>
              {unmappedCategories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.code ?? c.id} — {c.name}
                </option>
              ))}
            </select>
          </label>
          <label className="block text-sm font-medium">
            Service Profile
            <select
              className="mt-1 block w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              value={form.serviceProfileId}
              onChange={(e) => setForm((f) => ({ ...f, serviceProfileId: e.target.value }))}
            >
              <option value="">Select...</option>
              {profiles.filter((p) => p.isActive).map((p) => (
                <option key={p.id} value={p.id}>
                  {p.code} — {p.name}
                </option>
              ))}
            </select>
          </label>
        </div>
      </Modal>

      {deleting && (
        <Modal
          open={!!deleting}
          onClose={() => setDeleting(null)}
          title="Remove mapping"
          footer={
            <>
              <Button variant="outline" onClick={() => setDeleting(null)}>
                Cancel
              </Button>
              <Button variant="destructive" onClick={() => deleting && handleDelete(deleting)}>
                Remove
              </Button>
            </>
          }
        >
          <p>
            Remove mapping from &quot;{deleting.orderCategoryName ?? deleting.orderCategoryId}&quot; to &quot;
            {deleting.serviceProfileName ?? deleting.serviceProfileId}&quot;?
          </p>
        </Modal>
      )}
    </PageShell>
  );
};

export default ServiceProfileMappingsPage;
