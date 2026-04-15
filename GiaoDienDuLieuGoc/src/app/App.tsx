import { useState } from 'react';
import { Factory, AlertTriangle, TrendingUp, DollarSign } from 'lucide-react';
import { Sidebar } from './components/Sidebar';
import { TopBar } from './components/TopBar';
import { StatCard } from './components/StatCard';
import { ProductionChart } from './components/ProductionChart';
import { DefectChart } from './components/DefectChart';
import { ActivityTable } from './components/ActivityTable';
import { ProductionOrdersTable } from './components/ProductionOrdersTable';
import { SystemAdminPage } from './components/SystemAdminPage';
import { MasterDataPage } from './components/MasterDataPage';

export default function App() {
  const [activeView, setActiveView] = useState('dashboard');

  const renderContent = () => {
    if (activeView === 'admin') {
      return <SystemAdminPage />;
    }

    if (activeView === 'data') {
      return <MasterDataPage />;
    }

    if (activeView === 'dashboard') {
      return (
        <div className="p-8">
          <div className="mb-8">
            <h1 className="text-2xl mb-1">Bảng điều khiển</h1>
            <p className="text-sm text-gray-600">Tổng quan hệ thống sản xuất - Cập nhật lúc 10:45 SA, 13/04/2026</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
            <StatCard
              icon={Factory}
              title="Lệnh sản xuất đang chạy"
              value="24"
              color="blue"
              trend={{ value: '+3', positive: true }}
            />
            <StatCard
              icon={AlertTriangle}
              title="Cảnh báo vật tư"
              value="8"
              color="orange"
              trend={{ value: '+2', positive: false }}
            />
            <StatCard
              icon={TrendingUp}
              title="Năng suất hôm nay"
              value="94.2%"
              color="green"
              trend={{ value: '+2.4%', positive: true }}
            />
            <StatCard
              icon={DollarSign}
              title="Doanh thu tháng này"
              value="1.2 tỷ"
              color="purple"
              trend={{ value: '+12%', positive: true }}
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
            <ProductionChart />
            <DefectChart />
          </div>

          <div className="mb-8">
            <ProductionOrdersTable />
          </div>

          <div>
            <ActivityTable />
          </div>
        </div>
      );
    }

    return (
      <div className="p-8">
        <div className="text-center py-20">
          <h2 className="text-xl text-gray-600">Chức năng đang phát triển</h2>
          <p className="text-sm text-gray-500 mt-2">Màn hình này sẽ được cập nhật sớm</p>
        </div>
      </div>
    );
  };

  return (
    <div className="h-screen flex bg-gray-50">
      <Sidebar activeView={activeView} onViewChange={setActiveView} />

      <div className="flex-1 flex flex-col overflow-hidden">
        <TopBar />

        <main className="flex-1 overflow-y-auto">
          {renderContent()}
        </main>
      </div>
    </div>
  );
}