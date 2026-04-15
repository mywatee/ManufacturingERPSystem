import { Building2, Calendar, Lock, Save } from 'lucide-react';

export function SystemSettings() {
  return (
    <div>
      <div className="mb-6">
        <h2 className="text-xl mb-1">Thiết lập Hệ thống</h2>
        <p className="text-sm text-gray-600">Cấu hình các tham số chung của hệ thống ERP</p>
      </div>

      <div className="space-y-6">
        <div className="bg-white border border-gray-200 rounded-lg">
          <div className="p-4 border-b border-gray-200 flex items-center gap-2">
            <Building2 className="w-5 h-5 text-gray-600" />
            <h3 className="text-base">Thông tin Doanh nghiệp</h3>
          </div>
          <div className="p-6 space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm mb-2">Tên doanh nghiệp</label>
                <input
                  type="text"
                  defaultValue="Công ty TNHH Sản xuất ABC"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm mb-2">Mã số thuế</label>
                <input
                  type="text"
                  defaultValue="0123456789"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                />
              </div>

              <div className="col-span-2">
                <label className="block text-sm mb-2">Địa chỉ</label>
                <input
                  type="text"
                  defaultValue="123 Đường ABC, Quận XYZ, TP. Hồ Chí Minh"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm mb-2">Số điện thoại</label>
                <input
                  type="tel"
                  defaultValue="028 1234 5678"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm mb-2">Email</label>
                <input
                  type="email"
                  defaultValue="info@company.vn"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                />
              </div>

              <div className="col-span-2">
                <label className="block text-sm mb-2">Logo doanh nghiệp</label>
                <div className="flex items-center gap-4">
                  <div className="w-24 h-24 border-2 border-dashed border-gray-300 rounded-lg flex items-center justify-center bg-gray-50">
                    <Building2 className="w-8 h-8 text-gray-400" />
                  </div>
                  <div>
                    <button className="px-4 py-2 text-sm border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
                      Tải lên Logo
                    </button>
                    <p className="text-xs text-gray-500 mt-2">Định dạng: PNG, JPG (Tối đa 2MB)</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white border border-gray-200 rounded-lg">
          <div className="p-4 border-b border-gray-200 flex items-center gap-2">
            <Calendar className="w-5 h-5 text-gray-600" />
            <h3 className="text-base">Cấu hình Định dạng</h3>
          </div>
          <div className="p-6 space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm mb-2">Định dạng ngày tháng</label>
                <select className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500">
                  <option>DD/MM/YYYY</option>
                  <option>MM/DD/YYYY</option>
                  <option>YYYY-MM-DD</option>
                </select>
              </div>

              <div>
                <label className="block text-sm mb-2">Định dạng giờ</label>
                <select className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500">
                  <option>24 giờ (14:30)</option>
                  <option>12 giờ (2:30 PM)</option>
                </select>
              </div>

              <div>
                <label className="block text-sm mb-2">Đơn vị tiền tệ</label>
                <select className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500">
                  <option>VND (₫)</option>
                  <option>USD ($)</option>
                  <option>EUR (€)</option>
                </select>
              </div>

              <div>
                <label className="block text-sm mb-2">Múi giờ</label>
                <select className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500">
                  <option>GMT+7 (Hà Nội, TP.HCM)</option>
                  <option>GMT+8 (Singapore)</option>
                  <option>GMT+9 (Tokyo)</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        <div className="bg-white border border-gray-200 rounded-lg">
          <div className="p-4 border-b border-gray-200 flex items-center gap-2">
            <Lock className="w-5 h-5 text-gray-600" />
            <h3 className="text-base">Bảo mật & Xác thực</h3>
          </div>
          <div className="p-6 space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm mb-2">Độ dài mật khẩu tối thiểu</label>
                <input
                  type="number"
                  defaultValue="8"
                  min="6"
                  max="20"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm mb-2">Thuật toán băm mật khẩu</label>
                <select className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500">
                  <option>bcrypt (Khuyến nghị)</option>
                  <option>SHA-256</option>
                  <option>Argon2</option>
                </select>
              </div>

              <div>
                <label className="block text-sm mb-2">Thời gian hết phiên (phút)</label>
                <input
                  type="number"
                  defaultValue="30"
                  min="5"
                  max="480"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm mb-2">Số lần đăng nhập sai tối đa</label>
                <input
                  type="number"
                  defaultValue="5"
                  min="3"
                  max="10"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                />
              </div>

              <div className="col-span-2">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    defaultChecked
                    className="w-4 h-4 text-blue-600 rounded border-gray-300 focus:ring-2 focus:ring-blue-500/20"
                  />
                  <span className="text-sm">Yêu cầu mật khẩu phức tạp (chữ hoa, chữ thường, số, ký tự đặc biệt)</span>
                </label>
              </div>

              <div className="col-span-2">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    defaultChecked
                    className="w-4 h-4 text-blue-600 rounded border-gray-300 focus:ring-2 focus:ring-blue-500/20"
                  />
                  <span className="text-sm">Bắt buộc đổi mật khẩu mỗi 90 ngày</span>
                </label>
              </div>

              <div className="col-span-2">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="checkbox"
                    className="w-4 h-4 text-blue-600 rounded border-gray-300 focus:ring-2 focus:ring-blue-500/20"
                  />
                  <span className="text-sm">Bật xác thực hai yếu tố (2FA)</span>
                </label>
              </div>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-end gap-3">
          <button className="px-6 py-2 text-gray-700 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
            Đặt lại
          </button>
          <button className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors flex items-center gap-2">
            <Save className="w-4 h-4" />
            Lưu cấu hình
          </button>
        </div>
      </div>
    </div>
  );
}
