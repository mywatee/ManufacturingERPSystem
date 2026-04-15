import { useState } from 'react';
import { Database } from 'lucide-react';
import { MaterialsTable, Material } from './masterdata/MaterialsTable';
import { BOMView } from './masterdata/BOMView';
import { RoutingsView } from './masterdata/RoutingsView';
import { Card } from './ui/card';

export function MasterDataPage() {
  const [selectedMaterial, setSelectedMaterial] = useState<Material | null>(null);

  return (
    <div className="p-8 h-full flex flex-col">
      <div className="mb-6">
        <div className="flex items-center gap-2 mb-1">
          <Database className="w-6 h-6 text-[#1e3a5f]" />
          <h1 className="text-2xl">Dữ liệu gốc</h1>
        </div>
        <p className="text-sm text-gray-600">
          Quản lý danh mục vật tư, định mức BOM và quy trình công nghệ
        </p>
      </div>

      <div className="flex-1 grid grid-cols-1 lg:grid-cols-5 gap-6 min-h-0">
        {/* Left Panel - Materials Table */}
        <Card className="lg:col-span-2 p-6 flex flex-col overflow-hidden">
          <MaterialsTable
            onMaterialSelect={setSelectedMaterial}
            selectedMaterialId={selectedMaterial?.id || null}
          />
        </Card>

        {/* Right Panel - BOM and Routings */}
        <div className="lg:col-span-3 flex flex-col gap-6 min-h-0">
          {/* BOM Section */}
          <Card className="p-6 flex flex-col flex-1 min-h-0 overflow-hidden">
            <BOMView selectedMaterial={selectedMaterial} />
          </Card>

          {/* Routings Section */}
          <Card className="p-6 flex flex-col flex-1 min-h-0 overflow-hidden">
            <RoutingsView selectedMaterial={selectedMaterial} />
          </Card>
        </div>
      </div>
    </div>
  );
}
