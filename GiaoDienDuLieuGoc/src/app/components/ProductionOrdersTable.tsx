import { FileSpreadsheet, FileText, Plus, Edit } from 'lucide-react';

const orders = [
  {
    id: 'LSX-2026-0413',
    product: 'Bánh răng truyền động BR-45',
    quantity: 500,
    completed: 320,
    status: 'Đang làm',
    priority: 'Khẩn cấp',
    deadline: '15/04/2026'
  },
  {
    id: 'LSX-2026-0412',
    product: 'Trục cam TC-28A',
    quantity: 300,
    completed: 0,
    status: 'Chờ',
    priority: 'Cao',
    deadline: '18/04/2026'
  },
  {
    id: 'LSX-2026-0410',
    product: 'Vỏ máy bơm VB-120',
    quantity: 200,
    completed: 185,
    status: 'Đang làm',
    priority: 'Khẩn cấp',
    deadline: '14/04/2026'
  },
  {
    id: 'LSX-2026-0408',
    product: 'Piston thủy lực PT-65',
    quantity: 400,
    completed: 400,
    status: 'Hoàn thành',
    priority: 'Trung bình',
    deadline: '12/04/2026'
  },
];

const statusColors = {
  'Chờ': 'bg-orange-50 text-orange-700 border-orange-200',
  'Đang làm': 'bg-blue-50 text-blue-700 border-blue-200',
  'Hoàn thành': 'bg-green-50 text-green-700 border-green-200',
};

export function ProductionOrdersTable() {
  return (
    <div className="bg-white border border-gray-200 rounded-lg">
      <div className="p-6 border-b border-gray-200 flex items-center justify-between">
        <h3>Lệnh sản xuất khẩn cấp</h3>
        <div className="flex items-center gap-2">
          <button className="px-3 py-1.5 text-sm text-gray-700 hover:bg-gray-100 rounded-lg border border-gray-200 flex items-center gap-2 transition-colors">
            <FileSpreadsheet className="w-4 h-4" />
            Xuất Excel
          </button>
          <button className="px-3 py-1.5 text-sm text-gray-700 hover:bg-gray-100 rounded-lg border border-gray-200 flex items-center gap-2 transition-colors">
            <FileText className="w-4 h-4" />
            Xuất PDF
          </button>
          <button className="px-3 py-1.5 text-sm text-white bg-blue-600 hover:bg-blue-700 rounded-lg flex items-center gap-2 transition-colors">
            <Plus className="w-4 h-4" />
            Thêm mới
          </button>
        </div>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="bg-gray-50 border-b border-gray-200">
              <th className="px-6 py-3 text-left text-xs text-gray-600">Mã lệnh</th>
              <th className="px-6 py-3 text-left text-xs text-gray-600">Sản phẩm</th>
              <th className="px-6 py-3 text-left text-xs text-gray-600">Số lượng</th>
              <th className="px-6 py-3 text-left text-xs text-gray-600">Tiến độ</th>
              <th className="px-6 py-3 text-left text-xs text-gray-600">Trạng thái</th>
              <th className="px-6 py-3 text-left text-xs text-gray-600">Hạn hoàn thành</th>
              <th className="px-6 py-3 text-left text-xs text-gray-600">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {orders.map((order) => {
              const progress = (order.completed / order.quantity) * 100;
              return (
                <tr key={order.id} className="border-b border-gray-100 hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2">
                      <span className="text-sm">{order.id}</span>
                      {order.priority === 'Khẩn cấp' && (
                        <span className="px-2 py-0.5 text-xs bg-red-100 text-red-700 rounded">
                          {order.priority}
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 text-sm">{order.product}</td>
                  <td className="px-6 py-4 text-sm">{order.quantity}</td>
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2">
                      <div className="flex-1 h-2 bg-gray-200 rounded-full overflow-hidden max-w-[100px]">
                        <div
                          className="h-full bg-blue-600 rounded-full transition-all"
                          style={{ width: `${progress}%` }}
                        ></div>
                      </div>
                      <span className="text-xs text-gray-600">{Math.round(progress)}%</span>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <span className={`px-2 py-1 text-xs rounded border ${statusColors[order.status as keyof typeof statusColors]}`}>
                      {order.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-600">{order.deadline}</td>
                  <td className="px-6 py-4">
                    <button className="p-1.5 text-gray-600 hover:bg-gray-100 rounded transition-colors">
                      <Edit className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
