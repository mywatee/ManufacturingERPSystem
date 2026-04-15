import { useState } from 'react';
import { Shield, Check } from 'lucide-react';

interface Permission {
  module: string;
  view: boolean;
  add: boolean;
  edit: boolean;
  delete: boolean;
  override: boolean;
}

const modules = [
  'Bảng điều khiển',
  'Quản trị hệ thống',
  'Dữ liệu gốc',
  'Sản xuất',
  'Kiểm soát chất lượng',
  'Kho bãi',
  'Nhân sự & Lương',
  'Tài chính',
];

const roles = ['Admin', 'Nhân viên vận hành', 'QC', 'Kế toán'];

export function RolePermissions() {
  const [selectedRole, setSelectedRole] = useState('Nhân viên vận hành');
  const [permissions, setPermissions] = useState<Permission[]>(
    modules.map(module => ({
      module,
      view: module === 'Bảng điều khiển' || module === 'Sản xuất',
      add: module === 'Sản xuất',
      edit: module === 'Sản xuất',
      delete: false,
      override: false,
    }))
  );

  const togglePermission = (moduleIndex: number, permission: keyof Permission) => {
    if (permission === 'module') return;

    setPermissions(prev => {
      const newPermissions = [...prev];
      newPermissions[moduleIndex] = {
        ...newPermissions[moduleIndex],
        [permission]: !newPermissions[moduleIndex][permission]
      };
      return newPermissions;
    });
  };

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-xl mb-1">Phân quyền dựa trên Vai trò (RBAC)</h2>
        <p className="text-sm text-gray-600">Cấu hình quyền truy cập cho từng vai trò trong hệ thống</p>
      </div>

      <div className="bg-white border border-gray-200 rounded-lg p-6 mb-6">
        <label className="block text-sm mb-3">Chọn vai trò để cấu hình</label>
        <div className="flex gap-3">
          {roles.map(role => (
            <button
              key={role}
              onClick={() => setSelectedRole(role)}
              className={`px-4 py-2 rounded-lg border transition-colors flex items-center gap-2 ${
                selectedRole === role
                  ? 'bg-blue-50 text-blue-700 border-blue-300'
                  : 'bg-white text-gray-700 border-gray-200 hover:bg-gray-50'
              }`}
            >
              <Shield className="w-4 h-4" />
              {role}
            </button>
          ))}
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
        <div className="p-4 bg-blue-50 border-b border-blue-200">
          <div className="flex items-center gap-2 text-blue-900">
            <Shield className="w-5 h-5" />
            <span className="text-sm">Ma trận quyền hạn cho: <strong>{selectedRole}</strong></span>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="bg-gray-50 border-b border-gray-200">
                <th className="px-6 py-3 text-left text-xs text-gray-600">Phân hệ</th>
                <th className="px-6 py-3 text-center text-xs text-gray-600">Xem</th>
                <th className="px-6 py-3 text-center text-xs text-gray-600">Thêm</th>
                <th className="px-6 py-3 text-center text-xs text-gray-600">Sửa</th>
                <th className="px-6 py-3 text-center text-xs text-gray-600">Xóa</th>
                <th className="px-6 py-3 text-center text-xs text-gray-600">Ghi đè</th>
              </tr>
            </thead>
            <tbody>
              {permissions.map((perm, index) => (
                <tr key={perm.module} className="border-b border-gray-100 hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4 text-sm">{perm.module}</td>
                  <td className="px-6 py-4">
                    <div className="flex justify-center">
                      <button
                        onClick={() => togglePermission(index, 'view')}
                        className={`w-5 h-5 rounded border-2 flex items-center justify-center transition-colors ${
                          perm.view
                            ? 'bg-green-500 border-green-500'
                            : 'bg-white border-gray-300 hover:border-gray-400'
                        }`}
                      >
                        {perm.view && <Check className="w-3 h-3 text-white" />}
                      </button>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex justify-center">
                      <button
                        onClick={() => togglePermission(index, 'add')}
                        className={`w-5 h-5 rounded border-2 flex items-center justify-center transition-colors ${
                          perm.add
                            ? 'bg-green-500 border-green-500'
                            : 'bg-white border-gray-300 hover:border-gray-400'
                        }`}
                      >
                        {perm.add && <Check className="w-3 h-3 text-white" />}
                      </button>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex justify-center">
                      <button
                        onClick={() => togglePermission(index, 'edit')}
                        className={`w-5 h-5 rounded border-2 flex items-center justify-center transition-colors ${
                          perm.edit
                            ? 'bg-orange-500 border-orange-500'
                            : 'bg-white border-gray-300 hover:border-gray-400'
                        }`}
                      >
                        {perm.edit && <Check className="w-3 h-3 text-white" />}
                      </button>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex justify-center">
                      <button
                        onClick={() => togglePermission(index, 'delete')}
                        className={`w-5 h-5 rounded border-2 flex items-center justify-center transition-colors ${
                          perm.delete
                            ? 'bg-red-500 border-red-500'
                            : 'bg-white border-gray-300 hover:border-gray-400'
                        }`}
                      >
                        {perm.delete && <Check className="w-3 h-3 text-white" />}
                      </button>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex justify-center">
                      <button
                        onClick={() => togglePermission(index, 'override')}
                        className={`w-5 h-5 rounded border-2 flex items-center justify-center transition-colors ${
                          perm.override
                            ? 'bg-purple-500 border-purple-500'
                            : 'bg-white border-gray-300 hover:border-gray-400'
                        }`}
                      >
                        {perm.override && <Check className="w-3 h-3 text-white" />}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="p-4 bg-gray-50 border-t border-gray-200 flex items-center justify-end gap-3">
          <button className="px-4 py-2 text-gray-700 border border-gray-200 rounded-lg hover:bg-white transition-colors">
            Đặt lại
          </button>
          <button className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors">
            Lưu cấu hình
          </button>
        </div>
      </div>

      <div className="mt-4 flex items-start gap-2 p-4 bg-blue-50 border border-blue-200 rounded-lg">
        <div className="text-blue-600 mt-0.5">
          <Shield className="w-5 h-5" />
        </div>
        <div className="text-sm text-blue-900">
          <strong>Lưu ý:</strong> Các thay đổi về phân quyền sẽ có hiệu lực ngay lập tức đối với tất cả người dùng thuộc vai trò này. Vui lòng kiểm tra kỹ trước khi lưu.
        </div>
      </div>
    </div>
  );
}
