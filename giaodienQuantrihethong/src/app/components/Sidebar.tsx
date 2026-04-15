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

interface SidebarProps {
  activeView: string;
  onViewChange: (view: string) => void;
}

const menuItems = [
  { id: 'dashboard', icon: LayoutDashboard, label: 'Bảng điều khiển' },
  { id: 'admin', icon: Settings, label: 'Quản trị hệ thống' },
  { id: 'data', icon: Database, label: 'Dữ liệu gốc' },
  { id: 'production', icon: Factory, label: 'Sản xuất' },
  { id: 'qc', icon: ClipboardCheck, label: 'Kiểm soát chất lượng' },
  { id: 'warehouse', icon: Warehouse, label: 'Kho bãi' },
  { id: 'hr', icon: Users, label: 'Nhân sự & Lương' },
  { id: 'finance', icon: DollarSign, label: 'Tài chính' },
];

export function Sidebar({ activeView, onViewChange }: SidebarProps) {
  return (
    <aside className="w-64 bg-[#1e3a5f] text-white flex flex-col h-full">
      <div className="p-6 border-b border-white/10">
        <h1 className="text-xl tracking-tight">Hệ thống ERP Sản xuất</h1>
      </div>

      <nav className="flex-1 p-4">
        <ul className="space-y-1">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const isActive = activeView === item.id;
            return (
              <li key={item.id}>
                <button
                  onClick={() => onViewChange(item.id)}
                  className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg transition-colors ${
                    isActive
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
