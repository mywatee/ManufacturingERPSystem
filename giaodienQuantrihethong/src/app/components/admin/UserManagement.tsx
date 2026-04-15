import { useState } from 'react';
import { Plus, Edit, Lock, Trash2, Check, X } from 'lucide-react';

interface User {
  id: string;
  maNV: string;
  username: string;
  fullName: string;
  email: string;
  phone: string;
  role: string;
  status: 'Hoạt động' | 'Khóa';
}

const mockUsers: User[] = [
  {
    id: '1',
    maNV: 'NV-001',
    username: 'huyhoang',
    fullName: 'Nguyễn Huy Hoàng',
    email: 'huyhoang@company.vn',
    phone: '0912345678',
    role: 'Admin',
    status: 'Hoạt động'
  },
  {
    id: '2',
    maNV: 'NV-024',
    username: 'maianh',
    fullName: 'Trần Mai Anh',
    email: 'maianh@company.vn',
    phone: '0923456789',
    role: 'Nhân viên vận hành',
    status: 'Hoạt động'
  },
  {
    id: '3',
    maNV: 'NV-045',
    username: 'tuanminh',
    fullName: 'Lê Tuấn Minh',
    email: 'tuanminh@company.vn',
    phone: '0934567890',
    role: 'QC',
    status: 'Hoạt động'
  },
  {
    id: '4',
    maNV: 'NV-089',
    username: 'thuha',
    fullName: 'Phạm Thu Hà',
    email: 'thuha@company.vn',
    phone: '0945678901',
    role: 'Kế toán',
    status: 'Hoạt động'
  },
  {
    id: '5',
    maNV: 'NV-156',
    username: 'vanhung',
    fullName: 'Hoàng Văn Hùng',
    email: 'vanhung@company.vn',
    phone: '0956789012',
    role: 'Nhân viên vận hành',
    status: 'Khóa'
  },
];

export function UserManagement() {
  const [showModal, setShowModal] = useState(false);
  const [users, setUsers] = useState(mockUsers);

  return (
    <div>
      <div className="mb-6 flex items-center justify-between">
        <div>
          <h2 className="text-xl mb-1">Quản lý Tài khoản Người dùng</h2>
          <p className="text-sm text-gray-600">Quản lý danh sách người dùng và phân quyền truy cập hệ thống</p>
        </div>
        <button
          onClick={() => setShowModal(true)}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors flex items-center gap-2"
        >
          <Plus className="w-4 h-4" />
          Thêm người dùng mới
        </button>
      </div>

      <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="bg-gray-50 border-b border-gray-200">
                <th className="px-6 py-3 text-left text-xs text-gray-600">Mã NV</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Tên đăng nhập</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Họ tên</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Email</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Số điện thoại</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Vai trò</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Trạng thái</th>
                <th className="px-6 py-3 text-left text-xs text-gray-600">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {users.map((user) => (
                <tr key={user.id} className="border-b border-gray-100 hover:bg-gray-50 transition-colors">
                  <td className="px-6 py-4 text-sm">{user.maNV}</td>
                  <td className="px-6 py-4 text-sm">{user.username}</td>
                  <td className="px-6 py-4 text-sm">{user.fullName}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{user.email}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{user.phone}</td>
                  <td className="px-6 py-4">
                    <span className="px-2 py-1 text-xs bg-purple-50 text-purple-700 rounded border border-purple-200">
                      {user.role}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <span className={`px-2 py-1 text-xs rounded border flex items-center gap-1 w-fit ${
                      user.status === 'Hoạt động'
                        ? 'bg-green-50 text-green-700 border-green-200'
                        : 'bg-red-50 text-red-700 border-red-200'
                    }`}>
                      {user.status === 'Hoạt động' ? (
                        <Check className="w-3 h-3" />
                      ) : (
                        <X className="w-3 h-3" />
                      )}
                      {user.status}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2">
                      <button
                        className="p-1.5 text-blue-600 hover:bg-blue-50 rounded transition-colors"
                        title="Sửa quyền"
                      >
                        <Edit className="w-4 h-4" />
                      </button>
                      <button
                        className="p-1.5 text-orange-600 hover:bg-orange-50 rounded transition-colors"
                        title="Reset mật khẩu"
                      >
                        <Lock className="w-4 h-4" />
                      </button>
                      <button
                        className="p-1.5 text-red-600 hover:bg-red-50 rounded transition-colors"
                        title="Xóa"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg w-full max-w-2xl mx-4">
            <div className="p-6 border-b border-gray-200">
              <h3 className="text-lg">Thêm người dùng mới</h3>
            </div>

            <div className="p-6">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm mb-2">Mã nhân viên</label>
                  <input
                    type="text"
                    placeholder="NV-XXX"
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm mb-2">Tên đăng nhập</label>
                  <input
                    type="text"
                    placeholder="username"
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                  />
                </div>

                <div className="col-span-2">
                  <label className="block text-sm mb-2">Họ và tên</label>
                  <input
                    type="text"
                    placeholder="Nguyễn Văn A"
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm mb-2">Email</label>
                  <input
                    type="email"
                    placeholder="email@company.vn"
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm mb-2">Số điện thoại</label>
                  <input
                    type="tel"
                    placeholder="0912345678"
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm mb-2">Vai trò</label>
                  <select className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500">
                    <option>Nhân viên vận hành</option>
                    <option>QC</option>
                    <option>Kế toán</option>
                    <option>Admin</option>
                  </select>
                </div>

                <div>
                  <label className="block text-sm mb-2">Mật khẩu</label>
                  <input
                    type="password"
                    placeholder="••••••••"
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                  />
                </div>
              </div>
            </div>

            <div className="p-6 border-t border-gray-200 flex items-center justify-end gap-3">
              <button
                onClick={() => setShowModal(false)}
                className="px-4 py-2 text-gray-700 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
              >
                Hủy
              </button>
              <button
                onClick={() => setShowModal(false)}
                className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
              >
                Lưu
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
