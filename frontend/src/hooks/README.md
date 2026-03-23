# React Query Hooks

This directory contains React Query hooks for data fetching and mutations.

## Pattern

Each feature should have its own hook file (e.g., `useOrders.js`, `useUsers.js`) that exports:

### Query Hooks (for fetching data)

```javascript
import { useQuery } from '@tanstack/react-query';
import { getOrders } from '../api/orders';

export const useOrders = (filters = {}, options = {}) => {
  return useQuery({
    queryKey: ['orders', filters], // Unique cache key
    queryFn: () => getOrders(filters), // API function
    ...options, // Additional React Query options
  });
};
```

**Usage:**
```javascript
const { data: orders, isLoading, error, refetch } = useOrders({ status: 'Active' });
```

### Mutation Hooks (for creating/updating/deleting)

```javascript
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createOrder } from '../api/orders';

export const useCreateOrder = () => {
  const queryClient = useQueryClient();
  const { showSuccess, showError } = useToast();

  return useMutation({
    mutationFn: (orderData) => createOrder(orderData),
    onSuccess: (data) => {
      // Invalidate related queries to trigger refetch
      queryClient.invalidateQueries({ queryKey: ['orders'] });
      showSuccess('Order created successfully');
      return data;
    },
    onError: (error) => {
      showError(error.message || 'Failed to create order');
    },
  });
};
```

**Usage:**
```javascript
const createOrder = useCreateOrder();

// In handler
createOrder.mutate(orderData);
// or with async/await
await createOrder.mutateAsync(orderData);
```

## Key Benefits

1. **Automatic caching** - Data is cached and shared across components
2. **Background refetching** - Data stays fresh automatically
3. **Loading states** - Built-in `isLoading`, `isFetching`, `isError` states
4. **Optimistic updates** - Update UI before server confirms
5. **Query invalidation** - Automatically refetch when data changes
6. **Request deduplication** - Multiple components requesting same data = one request

## Migration Guide

### Before (useState + useEffect):

```javascript
const [orders, setOrders] = useState([]);
const [loading, setLoading] = useState(true);

useEffect(() => {
  const loadData = async () => {
    try {
      setLoading(true);
      const data = await getOrders(filters);
      setOrders(data);
    } catch (err) {
      showError(err.message);
    } finally {
      setLoading(false);
    }
  };
  loadData();
}, [filters]);
```

### After (React Query):

```javascript
const { data: orders, isLoading, error } = useOrders(filters);

if (error) {
  showError(error.message);
}
```

## Query Keys

Query keys should be hierarchical arrays:
- `['orders']` - All orders
- `['orders', { status: 'Active' }]` - Filtered orders
- `['orders', orderId]` - Single order
- `['users', userId, 'permissions']` - Nested data

This allows precise cache invalidation:
- `queryClient.invalidateQueries({ queryKey: ['orders'] })` - Invalidates all order queries
- `queryClient.invalidateQueries({ queryKey: ['orders', orderId] })` - Invalidates specific order

## Best Practices

1. **Keep hooks focused** - One hook per API endpoint/resource
2. **Use query keys consistently** - Follow the same pattern across all hooks
3. **Handle errors in hooks** - Show toasts, log errors appropriately
4. **Invalidate related queries** - After mutations, invalidate affected queries
5. **Use enabled option** - Prevent queries from running until prerequisites are met
6. **Leverage staleTime** - Configure per-query staleTime for frequently accessed data

