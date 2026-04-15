import { useState } from 'react';
import { Search, Filter, Calendar, FileEdit, Plus, Trash2 } from 'lucide-react';

interface AuditLog {
  id: string;
  timestamp: string;
  user: string;
  action: 'Thêm' | 'Xóa' | 'Sửa';
  tableName: string;
  oldValue: string;
  newValue: string;
}

const mockLogs: AuditLog[] = [
  {
    id: '1',
    timestamp: '13/04/2026 10:30:45',
    user: 'Huy Hoàng',
    action: 'Thêm',
    tableName: 'ProductionOrders',
    oldValue: '-',
    newValue: 'LSX-2026-0413 | Số lượng: 500'
  },
  {
    id: '2',
    timestamp: '13/04/2026 09:45:22',
    user: 'Mai Anh',
    action: 'Sửa',
    tableName: 'BOM',
    oldValue: 'Định mức: 2.5kg',
    newValue: 'Định mức: 2.8kg'
  },
  {
    id: '3',
    timestamp: '13/04/2026 08:20:15',
    user: 'Tuấn Minh',
    action: 'Xóa',
    tableName: 'WarehouseTransactions',
    oldValue: 'PXK-0412 | Số lượng: 100',
    newValue: '-'
  },
  {
    id: '4',
    timestamp: '13/04/2026 07:55:33',
    user: 'Thu Hà',
    action: 'Thêm',
    tableName: 'Suppliers',
    oldValue: '-',
    newValue: 'NCC-VL-089 | Tên: Công ty TNHH Thép Việt'
  },
  {
    id: '5',
    timestamp: '13/04/2026 07:30:10',
    user: 'Huy Hoàng',
    action: 'Sửa',
    tableName: 'Users',
    oldValue: 'Vai trò: Nhân viên',
    newValue: 'Vai trò: QC'
  },
  {
    id: '6',
    timestamp: '12/04/2026 16:45:55',
    user: 'Mai Anh',
    action: 'Thêm',
    tableName: 'QualityControl',
    oldValue: '-',
    newValue: 'QC-20260412-045 | Kết quả: Lỗi'
  },
  {
    id: '7',
    timestamp: '12/04/2026 15:20:30',
    user: 'Văn Hùng',
    action: 'Sửa',
    tableName: 'ProductionOrders',
    oldValue: 'Trạng thái: Chờ',
    newValue: 'Trạng thái: Đang làm'
  },
  {
    id: '8',
    timestamp: '12/04/2026 14:10:18',
    user: 'Thu Hà',
    action: 'Xóa',
    tableName: 'WarehouseTransactions',
    oldValue: 'PNK-0411 | Số lượng: 250',
    newValue: '-'
  },
];

export function AuditLogs() {
  const [logs] = useState(mockLogs);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedAction, setSelectedAction] = useState('Tất cả');
  const [showFilters, setShowFilters] = useState(false);

  const getActionIcon = (action: string) => {
    switch (action) {
      case 'Thêm':
        return <Plus className="w-4 h-4" />;
      case 'Sửa':
        return <FileEdit className="w-4 h-4" />;
      case 'Xóa':
        return <Trash2 className="w-4 h-4" />;
      default:
        return null;
    }
  };

  const getActionColor = (action: string) => {
    switch (action) {
      case 'Thêm':
        return 'text-green-600 bg-green-50';
      case 'Sửa':
        return 'text-orange-600 bg-orange-50';
      case 'Xóa':
        return 'text-red-600 bg-red-50';
      default:
        return 'text-gray-600 bg-gray-50';
    }
  };

  const filteredLogs = logs.filter(log => {
    const matchesSearch = log.user.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         log.tableName.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesAction = selectedAction === 'Tất cả' || log.action === selectedAction;
    return matchesSearch && matchesAction;
  });

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-xl mb-1">Nhật ký Hệ thống (Audit Logs)</h2>
        <p className="text-sm text-gray-600">Theo dõi và truy xuất mọi thay đổi dữ liệu trong hệ thống</p>
      </div>

      <div className="bg-white border border-gray-200 rounded-lg p-4 mb-6">
        <div className="flex flex-wrap gap-4">
          <div className="flex-1 min-w-[300px]">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                placeholder="Tìm kiếm theo người dùng hoặc bảng..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
              />
            </div>
          </div>

          <div className="flex gap-2">
            <button
              onClick={() => setShowFilters(!showFilters)}
              className="px-4 py-2 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors flex items-center gap-2"
            >
              <Filter className="w-4 h-4" />
              Bộ lọc
            </button>
            <button className="px-4 py-2 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors flex items-center gap-2">
              <Calendar className="w-4 h-4" />
              Chọn ngày
            </button>
          </div>
        </div>

        {showFilters && (
          <div className="mt-4 pt-4 border-t border-gray-200">
            <label className="block text-sm mb-2">Lọc theo hành động</label>
            <div className="flex gap-2">
              {['Tất cả', 'Thêm', 'Sửa', 'Xóa'].map(action => (
                <button
                  key={action}
                  onClick={() => setSelectedAction(action)}
                  className={`px-3 py-1.5 text-sm rounded-lg border transition-colors ${
                    selectedAction === action
                      ? 'bg-blue-50 text-blue-700 border-blue-300'
                      : 'bg-white text-gray-700 border-gray-200 hover:bg-gray-50'
                  }`}
                >
                  {action}
                </button>
              ))}
            </div>
          </div>
        )}
      </div>

      <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="bg-gray-50 border-b border-gray-200">
                <th className="px-6 py-3 text-left text-xs text-gray-600">Thời gian</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Người thực hiện</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Hành động</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Bảng dữ liệu</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Giá trị cũ</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Giá trị mới</th>
              </tr>
            </thead>
            <tbody>
              {filteredLogs.map((log) => (
                <tr key={log.id} className="border-b border-gray-100 hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4 text-sm text-gray-600">{log.timestamp}</td>
                  <td className="px-6 py-4 text-sm">{log.user}</td>
                  <td className="px-6 py-4">
                    <div className={`inline-flex items-center gap-1.5 px-2 py-1 text-xs rounded ${getActionColor(log.action)}`}>
                      {getActionIcon(log.action)}
                      {log.action}
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <span className="text-sm font-mono bg-gray-100 px-2 py-1 rounded text-gray-700">
                      {log.tableName}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-600 max-w-xs truncate">
                    {log.oldValue}
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-600 max-w-xs truncate">
                    {log.newValue}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="p-4 bg-gray-50 border-t border-gray-200 flex items-center justify-between">
          <div className="text-sm text-gray-600">
            Hiển thị {filteredLogs.length} / {logs.length} bản ghi
          </div>
          <div className="flex gap-2">
            <button className="px-3 py-1.5 text-sm border border-gray-200 rounded-lg hover:bg-white transition-colors">
              Trước
            </button>
            <button className="px-3 py-1.5 text-sm bg-blue-600 text-white rounded-lg">
              1
            </button>
            <button className="px-3 py-1.5 text-sm border border-gray-200 rounded-lg hover:bg-white transition-colors">
              2
            </button>
            <button className="px-3 py-1.5 text-sm border border-gray-200 rounded-lg hover:bg-white transition-colors">
              Sau
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
