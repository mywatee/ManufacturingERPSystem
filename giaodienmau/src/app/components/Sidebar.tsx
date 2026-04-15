import {
  LayoutDashboard,
  Settings,
  Database,
  Factory,
  ClipboardCheck,
  Warehouse,
  Users,
  DollarSign,
} from 'lucide-react';

const menuItems = [
  { icon: LayoutDashboard, label: 'Bảng điều khiển', active: true },
  { icon: Settings, label: 'Quản trị hệ thống' },
  { icon: Database, label: 'Dữ liệu gốc' },
  { icon: Factory, label: 'Sản xuất' },
  { icon: ClipboardCheck, label: 'Kiểm soát chất lượng' },
  { icon: Warehouse, label: 'Kho bãi' },
  { icon: Users, label: 'Nhân sự & Lương' },
  { icon: DollarSign, label: 'Tài chính' },
];

export function Sidebar() {
  return (
    <aside className="w-64 bg-[#1e3a5f] text-white flex flex-col h-full">
      <div className="p-6 border-b border-white/10">
        <h1 className="text-xl tracking-tight">Hệ thống ERP Sản xuất</h1>
      </div>

      <nav className="flex-1 p-4">
        <ul className="space-y-1">
          {menuItems.map((item, index) => {
            const Icon = item.icon;
            return (
              <li key={index}>
                <button
                  className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                    item.active
                      ? 'bg-white/15 text-white'
                      : 'text-white/70 hover:bg-white/10 hover:text-white'
                  }`}
                >
                  <Icon className="w-5 h-5 shrink-0" />
                  <span className="text-sm">{item.label}</span>
                </button>
              </li>
            );
          })}
        </ul>
      </nav>
    </aside>
  );
}
