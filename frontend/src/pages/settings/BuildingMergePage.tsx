import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Merge, ArrowRight, AlertTriangle, Loader2 } from 'lucide-react';
import { getBuildings, getMergeCandidates, getMergePreview, mergeBuildings } from '../../api/buildings';
import { Button, Card, LoadingSpinner, useToast } from '../../components/ui';
import { PageShell } from '../../components/layout';
import type { Building } from '../../types/buildings';

const BuildingMergePage: React.FC = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();
  const [sourceBuildingId, setSourceBuildingId] = useState<string>('');
  const [targetBuildingId, setTargetBuildingId] = useState<string>('');

  const { data: buildings = [], isLoading: buildingsLoading } = useQuery({
    queryKey: ['buildings'],
    queryFn: () => getBuildings({ isActive: true }),
  });

  const { data: candidates = [], isLoading: candidatesLoading } = useQuery({
    queryKey: ['buildings', 'merge-candidates', sourceBuildingId],
    queryFn: () => getMergeCandidates(sourceBuildingId),
    enabled: !!sourceBuildingId,
  });

  const { data: preview, isLoading: previewLoading } = useQuery({
    queryKey: ['buildings', 'merge-preview', sourceBuildingId, targetBuildingId],
    queryFn: () => getMergePreview(sourceBuildingId, targetBuildingId),
    enabled: !!sourceBuildingId && !!targetBuildingId && sourceBuildingId !== targetBuildingId,
  });

  const mergeMutation = useMutation({
    mutationFn: mergeBuildings,
    onSuccess: (result) => {
      showSuccess(result.message);
      setSourceBuildingId('');
      setTargetBuildingId('');
      queryClient.invalidateQueries({ queryKey: ['buildings'] });
    },
    onError: (err: Error) => showError(err.message),
  });


  return (
    <PageShell
      title="Building Merge"
      description="Merge duplicate buildings: reassign orders and parsed drafts to the target building, then soft-delete the source."
    >
      <div className="max-w-3xl space-y-6">
        <Card className="p-6">
          <h3 className="text-lg font-semibold mb-4">1. Select source building (to be merged away)</h3>
          <select
            className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            value={sourceBuildingId}
            onChange={(e) => {
              setSourceBuildingId(e.target.value);
              setTargetBuildingId('');
            }}
          >
            <option value="">— Select building —</option>
            {buildings.map((b) => (
              <option key={b.id} value={b.id}>
                {b.name} {b.city ? `(${b.city})` : ''}
              </option>
            ))}
          </select>
          {buildingsLoading && <LoadingSpinner className="mt-2" />}
        </Card>

        {sourceBuildingId && (
          <Card className="p-6">
            <h3 className="text-lg font-semibold mb-4">2. Select target building (to keep)</h3>
            {candidates.length === 0 && !candidatesLoading && (
              <p className="text-muted-foreground text-sm">No similar buildings found. You can still choose any building as target below.</p>
            )}
            <select
              className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm mt-2"
              value={targetBuildingId}
              onChange={(e) => setTargetBuildingId(e.target.value)}
            >
              <option value="">— Select target —</option>
              {(candidates.length > 0
                ? candidates
                : buildings.map((b) => ({ id: b.id, name: b.name, city: b.city ?? '', state: b.state ?? '', ordersCount: 0 }))
              )
                .filter((c) => c.id !== sourceBuildingId)
                .map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name} {c.city ? `(${c.city})` : ''} {c.ordersCount != null && c.ordersCount > 0 ? `— ${c.ordersCount} orders` : ''}
                  </option>
                ))}
            </select>
            {candidatesLoading && <LoadingSpinner className="mt-2" />}
          </Card>
        )}

        {sourceBuildingId && targetBuildingId && sourceBuildingId !== targetBuildingId && (
          <Card className="p-6">
            <h3 className="text-lg font-semibold mb-4">3. Preview & merge</h3>
            {previewLoading && <LoadingSpinner />}
            {preview && !previewLoading && (
              <div className="space-y-4">
                <div className="flex items-center gap-2 text-sm">
                  <span className="font-medium">{preview.sourceBuildingName}</span>
                  <ArrowRight className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">{preview.targetBuildingName}</span>
                </div>
                <ul className="text-sm text-muted-foreground list-disc list-inside">
                  <li>{preview.ordersToReassignCount} order(s) will be reassigned</li>
                  <li>{preview.parsedDraftsToReassignCount} parsed draft(s) will be reassigned</li>
                  <li>Source building will be soft-deleted (hidden from lists)</li>
                </ul>
                <div className="flex items-center gap-2 p-3 rounded-lg bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800">
                  <AlertTriangle className="h-5 w-5 text-amber-600 flex-shrink-0" />
                  <p className="text-sm">This action cannot be undone. The source building will be marked deleted.</p>
                </div>
                <Button
                  onClick={() => mergeMutation.mutate({ sourceBuildingId, targetBuildingId })}
                  disabled={mergeMutation.isPending}
                >
                  {mergeMutation.isPending ? (
                    <>
                      <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                      Merging...
                    </>
                  ) : (
                    <>
                      <Merge className="h-4 w-4 mr-2" />
                      Merge buildings
                    </>
                  )}
                </Button>
              </div>
            )}
          </Card>
        )}
      </div>
    </PageShell>
  );
};

export default BuildingMergePage;
