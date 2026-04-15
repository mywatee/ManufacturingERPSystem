import { Download } from 'lucide-react';

export function ExportButton() {
  const handleExport = () => {
    // Export all source code as a downloadable file
    const files = {
      'App.tsx': `import { Factory, AlertTriangle, TrendingUp, DollarSign } from 'lucide-react';
import { Sidebar } from './components/Sidebar';
import { TopBar } from './components/TopBar';
import { StatCard } from './components/StatCard';
import { ProductionChart } from './components/ProductionChart';
import { DefectChart } from './components/DefectChart';
import { ActivityTable } from './components/ActivityTable';
import { ProductionOrdersTable } from './components/ProductionOrdersTable';

export default function App() {
  return (
    <div className="h-screen flex bg-gray-50">
      <Sidebar />

      <div className="flex-1 flex flex-col overflow-hidden">
        <TopBar />

        <main className="flex-1 overflow-y-auto">
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
        </main>
      </div>
    </div>
  );
}`,
      'README.md': `# Hệ thống ERP Sản xuất - Dashboard

Giao diện Dashboard chuyên nghiệp cho hệ thống ERP Sản xuất với phong cách WPF hiện đại.

## Tính năng

- Dashboard tổng quan với các chỉ số KPI
- Biểu đồ theo dõi tiến độ sản xuất
- Phân tích nguyên nhân hàng lỗi
- Quản lý lệnh sản xuất
- Theo dõi hoạt động hệ thống

## Cài đặt

\`\`\`bash
npm install
# hoặc
pnpm install
\`\`\`

## Chạy ứng dụng

\`\`\`bash
npm run dev
# hoặc
pnpm dev
\`\`\`

## Công nghệ sử dụng

- React 18
- TypeScript
- Tailwind CSS v4
- Recharts (Biểu đồ)
- Lucide React (Icons)

## Cấu trúc thư mục

\`\`\`
src/
├── app/
│   ├── components/
│   │   ├── Sidebar.tsx
│   │   ├── TopBar.tsx
│   │   ├── StatCard.tsx
│   │   ├── ProductionChart.tsx
│   │   ├── DefectChart.tsx
│   │   ├── ActivityTable.tsx
│   │   └── ProductionOrdersTable.tsx
│   └── App.tsx
└── styles/
    ├── theme.css
    └── fonts.css
\`\`\`
`
    };

    let content = '# Hệ thống ERP Sản xuất - Source Code\n\n';
    content += 'Đã tạo: ' + new Date().toLocaleString('vi-VN') + '\n\n';
    content += '---\n\n';

    for (const [filename, code] of Object.entries(files)) {
      content += `## ${filename}\n\n\`\`\`typescript\n${code}\n\`\`\`\n\n---\n\n`;
    }

    const blob = new Blob([content], { type: 'text/markdown' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'erp-dashboard-source-code.md';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <button
      onClick={handleExport}
      className="fixed bottom-8 right-8 px-6 py-3 bg-blue-600 text-white rounded-lg shadow-lg hover:bg-blue-700 transition-colors flex items-center gap-2 z-50"
    >
      <Download className="w-5 h-5" />
      Tải Source Code
    </button>
  );
}
