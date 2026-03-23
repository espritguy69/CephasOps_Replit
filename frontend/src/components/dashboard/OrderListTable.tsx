import React from 'react';
import { Edit, Trash2, Eye } from 'lucide-react';

interface Order {
  id: string | number;
  orderNumber: string;
  customer: string;
  status: string;
  amount: string;
  date: string;
}

interface OrderListTableProps {
  orders?: Order[];
  onView?: (order: Order) => void;
  onEdit?: (order: Order) => void;
  onDelete?: (order: Order) => void;
}

const OrderListTable: React.FC<OrderListTableProps> = ({ orders = [], onView, onEdit, onDelete }) => {
  // Sample data if none provided
  const tableData: Order[] = orders.length > 0 ? orders : [
    { id: 1, orderNumber: 'ORD-001', customer: 'John Doe', status: 'Pending', amount: 'RM 150.00', date: '2024-01-15' },
    { id: 2, orderNumber: 'ORD-002', customer: 'Jane Smith', status: 'Completed', amount: 'RM 200.00', date: '2024-01-14' },
    { id: 3, orderNumber: 'ORD-003', customer: 'Bob Johnson', status: 'In Progress', amount: 'RM 175.00', date: '2024-01-13' },
    { id: 4, orderNumber: 'ORD-004', customer: 'Alice Brown', status: 'Pending', amount: 'RM 300.00', date: '2024-01-12' },
    { id: 5, orderNumber: 'ORD-005', customer: 'Charlie Wilson', status: 'Completed', amount: 'RM 125.00', date: '2024-01-11' },
  ];

  const getStatusBadge = (status: string): React.ReactNode => {
    const statusColors: Record<string, string> = {
      'Pending': 'bg-yellow-100 text-yellow-800',
      'Completed': 'bg-green-100 text-green-800',
      'In Progress': 'bg-blue-100 text-blue-800',
      'Cancelled': 'bg-red-100 text-red-800',
    };
    const colorClass = statusColors[status] || 'bg-gray-100 text-gray-800';
    
    return (
      <span className={`px-2 py-1 rounded-full text-xs font-medium ${colorClass}`}>
        {status}
      </span>
    );
  };

  return (
    <div className="rounded-xl bg-layout-card shadow-card p-5">
      <div className="mb-4">
        <h3 className="text-sm font-semibold text-slate-900">Recent Orders</h3>
        <p className="text-xs text-slate-600 mt-0.5">Latest order activity</p>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full border-collapse">
          <thead>
            <tr className="border-b border-slate-300">
              <th className="text-left py-3 px-4 text-xs font-semibold text-slate-800">Order #</th>
              <th className="text-left py-3 px-4 text-xs font-semibold text-slate-800">Customer</th>
              <th className="text-left py-3 px-4 text-xs font-semibold text-slate-800">Status</th>
              <th className="text-left py-3 px-4 text-xs font-semibold text-slate-800">Amount</th>
              <th className="text-left py-3 px-4 text-xs font-semibold text-slate-800">Date</th>
              <th className="text-right py-3 px-4 text-xs font-semibold text-slate-800">Actions</th>
            </tr>
          </thead>
          <tbody>
            {tableData.map((order) => (
              <tr
                key={order.id}
                className="border-b border-slate-200 hover:bg-slate-50 transition-colors h-11"
              >
                <td className="py-2.5 px-4 text-xs text-slate-800 font-medium">{order.orderNumber}</td>
                <td className="py-2.5 px-4 text-xs text-slate-700">{order.customer}</td>
                <td className="py-2.5 px-4 text-xs">
                  {getStatusBadge(order.status)}
                </td>
                <td className="py-2.5 px-4 text-xs text-slate-800 font-medium">{order.amount}</td>
                <td className="py-2.5 px-4 text-xs text-slate-600">{order.date}</td>
                <td className="py-2.5 px-4 text-xs">
                  <div className="flex items-center justify-end gap-2">
                    {onView && (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          onView(order);
                        }}
                        title="View"
                        className="text-brand-600 hover:opacity-75 cursor-pointer transition-colors"
                      >
                        <Eye className="h-7 w-7" />
                      </button>
                    )}
                    {onEdit && (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          onEdit(order);
                        }}
                        title="Edit"
                        className="text-blue-600 hover:opacity-75 cursor-pointer transition-colors"
                      >
                        <Edit className="h-7 w-7" />
                      </button>
                    )}
                    {onDelete && (
                      <button
                        onClick={(e) => {
                          e.stopPropagation();
                          onDelete(order);
                        }}
                        title="Delete"
                        className="text-red-600 hover:opacity-75 cursor-pointer transition-colors"
                      >
                        <Trash2 className="h-7 w-7" />
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default OrderListTable;

