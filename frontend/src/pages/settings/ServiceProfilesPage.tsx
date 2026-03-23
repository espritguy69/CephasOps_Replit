import React, { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Layers, ChevronDown, ChevronUp } from 'lucide-react';
import {
  getServiceProfiles,
  createServiceProfile,
  updateServiceProfile,
  deleteServiceProfile,
  type ServiceProfileDto,
  type CreateServiceProfileRequest,
  type UpdateServiceProfileRequest,
} from '../../api/serviceProfiles';
import { LoadingSpinner, EmptyState, useToast, Button, Card, TextInput, Modal, DataTable } from '../../components/ui';
import { PageShell } from '../../components/layout';

interface TableColumn<T> {
  key: string;
  label: string;
  render?: (value: unknown, row: T) => React.ReactNode;
}

const ServiceProfilesPage: React.FC = () => {
  const { showSuccess, showError } = useToast();
  const [profiles, setProfiles] = useState<ServiceProfileDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showGuide, setShowGuide] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState<ServiceProfileDto | null>(null);
  const [deleting, setDeleting] = useState<ServiceProfileDto | null>(null);
  const [form, setForm] = useState({
    code: '',
    name: '',
    description: '',
    displayOrder: 0,
    isActive: true,
  });

  const load = async () => {
    try {
      setLoading(true);
      const data = await getServiceProfiles();
      setProfiles(Array.isArray(data) ? data : []);
    } catch (err) {
      showError((err as Error).message || 'Failed to load service profiles');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const resetForm = () => {
    setForm({
      code: '',
      name: '',
      description: '',
      displayOrder: 0,
      isActive: true,
    });
    setEditing(null);
  };

  const openCreate = () => {
    resetForm();
    setShowModal(true);
  };

  const openEdit = (row: ServiceProfileDto) => {
    setEditing(row);
    setForm({
      code: row.code,
      name: row.name,
      description: row.description ?? '',
      displayOrder: row.displayOrder,
      isActive: row.isActive,
    });
    setShowModal(true);
  };

  const handleSave = async () => {
    try {
      if (editing) {
        const payload: UpdateServiceProfileRequest = {
          code: form.code.trim(),
          name: form.name.trim(),
          description: form.description.trim() || undefined,
          displayOrder: form.displayOrder,
          isActive: form.isActive,
        };
        await updateServiceProfile(editing.id, payload);
        showSuccess('Service profile updated.');
      } else {
        const payload: CreateServiceProfileRequest = {
          code: form.code.trim(),
          name: form.name.trim(),
          description: form.description.trim() || undefined,
          displayOrder: form.displayOrder,
          isActive: form.isActive,
        };
        await createServiceProfile(payload);
        showSuccess('Service profile created.');
      }
      setShowModal(false);
      resetForm();
      await load();
    } catch (err) {
      showError((err as Error).message || 'Save failed');
    }
  };

  const handleDelete = async (row: ServiceProfileDto) => {
    if (!window.confirm(`Delete service profile "${row.name}"?`)) return;
    try {
      await deleteServiceProfile(row.id);
      showSuccess('Service profile deleted.');
      setDeleting(null);
      await load();
    } catch (err) {
      showError((err as Error).message || 'Delete failed');
    }
  };

  const columns: TableColumn<ServiceProfileDto>[] = [
    { key: 'displayOrder', label: 'Order' },
    { key: 'code', label: 'Code' },
    { key: 'name', label: 'Name' },
    {
      key: 'isActive',
      label: 'Status',
      render: (value) => (
        <span
          className={`px-2 py-1 rounded-full text-xs font-medium ${
            value ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200' : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
          }`}
        >
          {value ? 'Active' : 'Inactive'}
        </span>
      ),
    },
    {
      key: 'actions',
      label: 'Actions',
      render: (_, row) => (
        <div className="flex items-center gap-2">
          <button onClick={() => openEdit(row)} title="Edit" className="text-blue-600 hover:opacity-75">
            <Edit className="h-4 w-4" />
          </button>
          <button onClick={() => setDeleting(row)} title="Delete" className="text-red-600 hover:opacity-75">
            <Trash2 className="h-4 w-4" />
          </button>
        </div>
      ),
    },
  ];

  if (loading) {
    return <LoadingSpinner message="Loading service profiles..." fullPage />;
  }

  return (
    <PageShell
      title="Service Profiles"
      breadcrumbs={[{ label: 'Settings', path: '/settings' }, { label: 'GPON' }, { label: 'Service Profiles' }]}
      actions={
        <Button onClick={openCreate} className="flex items-center gap-2">
          <Plus className="h-4 w-4" />
          Add profile
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
              <Layers className="h-4 w-4 text-primary" />
              <span className="font-medium text-sm">What are Service Profiles?</span>
            </div>
            {showGuide ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
          </button>
          {showGuide && (
            <div className="px-3 pb-3 text-sm text-muted-foreground">
              <p className="mb-2">
                Service Profiles group Order Categories (e.g. FTTH, FTTR, FTTO) into service families for pricing.
                Examples: <strong>RESIDENTIAL_FIBER</strong> (FTTH + FTTR), <strong>BUSINESS_FIBER</strong> (FTTO),
                <strong>MAINTENANCE</strong> (assurance categories). In a future phase, Base Work Rates can target a
                profile instead of duplicating rates per category. For now, configure profiles and map Order Categories
                in the next tab; payout behaviour is unchanged until engine integration.
              </p>
            </div>
          )}
        </Card>

        <Card>
          {profiles.length > 0 ? (
            <DataTable data={profiles} columns={columns} />
          ) : (
            <EmptyState
              title="No service profiles"
              message="Add a profile (e.g. RESIDENTIAL_FIBER, BUSINESS_FIBER) and then map Order Categories to it."
              action={{ label: 'Add profile', onClick: openCreate }}
            />
          )}
        </Card>
      </div>

      <Modal
        open={showModal}
        onClose={() => {
          setShowModal(false);
          resetForm();
        }}
        title={editing ? 'Edit Service Profile' : 'New Service Profile'}
        footer={
          <>
            <Button variant="outline" onClick={() => setShowModal(false)}>
              Cancel
            </Button>
            <Button onClick={handleSave}>Save</Button>
          </>
        }
      >
        <div className="space-y-3">
          <TextInput
            label="Code"
            value={form.code}
            onChange={(e) => setForm((f) => ({ ...f, code: e.target.value }))}
            placeholder="e.g. RESIDENTIAL_FIBER"
          />
          <TextInput
            label="Name"
            value={form.name}
            onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
            placeholder="Residential Fiber"
          />
          <TextInput
            label="Description"
            value={form.description}
            onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
            placeholder="Optional"
          />
          <TextInput
            label="Display order"
            type="number"
            value={String(form.displayOrder)}
            onChange={(e) => setForm((f) => ({ ...f, displayOrder: parseInt(e.target.value, 10) || 0 }))}
          />
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
            />
            <span className="text-sm">Active</span>
          </label>
        </div>
      </Modal>

      {deleting && (
        <Modal
          open={!!deleting}
          onClose={() => setDeleting(null)}
          title="Delete Service Profile"
          footer={
            <>
              <Button variant="outline" onClick={() => setDeleting(null)}>
                Cancel
              </Button>
              <Button variant="destructive" onClick={() => deleting && handleDelete(deleting)}>
                Delete
              </Button>
            </>
          }
        >
          <p>
            Delete &quot;{deleting.name}&quot;? Any Order Category mappings to this profile will need to be removed or
            reassigned first.
          </p>
        </Modal>
      )}
    </PageShell>
  );
};

export default ServiceProfilesPage;
