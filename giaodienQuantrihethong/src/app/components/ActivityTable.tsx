import { FileEdit, Plus, Trash2 } from 'lucide-react';

const activities = [
  {
    id: 1,
    action: 'Thêm',
    content: 'Lệnh sản xuất LSX-2026-0413',
    user: 'Huy Hoàng',
    time: '10:30 SA',
    icon: Plus,
    color: 'text-green-600 bg-green-50'
  },
  {
    id: 2,
    action: 'Sửa',
    content: 'Định mức BOM-SP-1025',
    user: 'Mai Anh',
    time: '09:45 SA',
    icon: FileEdit,
    color: 'text-blue-600 bg-blue-50'
  },
  {
    id: 3,
    action: 'Xóa',
    content: 'Phiếu xuất kho PXK-0412',
    user: 'Tuấn Minh',
    time: '08:20 SA',
    icon: Trash2,
    color: 'text-red-600 bg-red-50'
  },
  {
    id: 4,
    action: 'Thêm',
    content: 'Nhà cung cấp NCC-VL-089',
    user: 'Thu Hà',
    time: '07:55 SA',
    icon: Plus,
    color: 'text-green-600 bg-green-50'
  },
  {
    id: 5,
    action: 'Sửa',
    content: 'Hồ sơ nhân viên NV-2024-156',
    user: 'Huy Hoàng',
    time: '07:30 SA',
    icon: FileEdit,
    color: 'text-blue-600 bg-blue-50'
  },
];

export function ActivityTable() {
  return (
    <div className="bg-white border border-gray-200 rounded-lg">
      <div className="p-6 border-b border-gray-200">
        <h3>Hoạt động gần đây</h3>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="bg-gray-50 border-b border-gray-200">
              <th className="px-6 py-3 text-left text-xs text-gray-600">Hành động</th>
              <th className="px-6 py-3 text-left text-xs text-gray-600">Nội dung</th>
              <th className="px-6 py-3 text-left text-xs text-gray-600">Người thực hiện</th>
              <th className="px-6 py-3 text-left text-xs text-gray-600">Thời gian</th>
            </tr>
          </thead>
          <tbody>
            {activities.map((activity) => {
              const Icon = activity.icon;
              return (
                <tr key={activity.id} className="border-b border-gray-100 hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2">
                      <div className={`w-8 h-8 rounded-lg flex items-center justify-center ${activity.color}`}>
                        <Icon className="w-4 h-4" />
                      </div>
                      <span className="text-sm">{activity.action}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4 text-sm">{activity.content}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{activity.user}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{activity.time}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
