import { useState } from 'react';
import { Users, Shield, FileText, Settings as SettingsIcon } from 'lucide-react';
import { UserManagement } from './admin/UserManagement';
import { RolePermissions } from './admin/RolePermissions';
import { AuditLogs } from './admin/AuditLogs';
import { SystemSettings } from './admin/SystemSettings';

type TabType = 'users' | 'roles' | 'logs' | 'settings';

interface Tab {
  id: TabType;
  label: string;
  icon: typeof Users;
}

const tabs: Tab[] = [
  { id: 'users', label: 'Quản lý Người dùng', icon: Users },
  { id: 'roles', label: 'Phân quyền', icon: Shield },
  { id: 'logs', label: 'Nhật ký Hệ thống', icon: FileText },
  { id: 'settings', label: 'Thiết lập', icon: SettingsIcon },
];

export function SystemAdminPage() {
  const [activeTab, setActiveTab] = useState<TabType>('users');

  const renderContent = () => {
    switch (activeTab) {
      case 'users':
        return <UserManagement />;
      case 'roles':
        return <RolePermissions />;
      case 'logs':
        return <AuditLogs />;
      case 'settings':
        return <SystemSettings />;
      default:
        return <UserManagement />;
    }
  };

  return (
    <div className="p-8">
      <div className="mb-8">
        <h1 className="text-2xl mb-1">Quản trị Hệ thống</h1>
        <p className="text-sm text-gray-600">Quản lý người dùng, phân quyền và cấu hình hệ thống</p>
      </div>

      <div className="bg-white border border-gray-200 rounded-lg mb-6">
        <div className="flex border-b border-gray-200 overflow-x-auto">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            return (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex items-center gap-2 px-6 py-4 border-b-2 transition-colors whitespace-nowrap ${
                  activeTab === tab.id
                    ? 'border-blue-600 text-blue-600'
                    : 'border-transparent text-gray-600 hover:text-gray-900'
                }`}
              >
                <Icon className="w-5 h-5" />
                <span className="text-sm">{tab.label}</span>
              </button>
            );
          })}
        </div>
      </div>

      <div>{renderContent()}</div>
    </div>
  );
}
