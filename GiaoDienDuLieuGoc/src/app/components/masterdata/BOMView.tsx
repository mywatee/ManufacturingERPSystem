import { useState } from 'react';
import { Plus, ChevronRight, ChevronDown, Package2 } from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '../ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select';
import { Material } from './MaterialsTable';

interface BOMItem {
  id: string;
  parentId: string;
  childId: string;
  childName: string;
  childCode: string;
  quantityPerUnit: number;
  unit: string;
  children?: BOMItem[];
}

interface BOMViewProps {
  selectedMaterial: Material | null;
}

const mockBOMData: Record<string, BOMItem[]> = {
  '4': [
    {
      id: 'bom-1',
      parentId: '4',
      childId: '3',
      childName: 'Khung sườn máy',
      childCode: 'BTP-001',
      quantityPerUnit: 1,
      unit: 'cái',
      children: [
        {
          id: 'bom-1-1',
          parentId: '3',
          childId: '1',
          childName: 'Thép tấm A36',
          childCode: 'NL-001',
          quantityPerUnit: 50,
          unit: 'kg',
        },
        {
          id: 'bom-1-2',
          parentId: '3',
          childId: '5',
          childName: 'Ống thép phi 60',
          childCode: 'NL-003',
          quantityPerUnit: 8,
          unit: 'mét',
        },
      ],
    },
    {
      id: 'bom-2',
      parentId: '4',
      childId: '6',
      childName: 'Bộ động cơ điện',
      childCode: 'BTP-002',
      quantityPerUnit: 1,
      unit: 'bộ',
      children: [
        {
          id: 'bom-2-1',
          parentId: '6',
          childId: '2',
          childName: 'Nhôm 6061',
          childCode: 'NL-002',
          quantityPerUnit: 15,
          unit: 'kg',
        },
      ],
    },
  ],
  '3': [
    {
      id: 'bom-3',
      parentId: '3',
      childId: '1',
      childName: 'Thép tấm A36',
      childCode: 'NL-001',
      quantityPerUnit: 50,
      unit: 'kg',
    },
    {
      id: 'bom-4',
      parentId: '3',
      childId: '5',
      childName: 'Ống thép phi 60',
      childCode: 'NL-003',
      quantityPerUnit: 8,
      unit: 'mét',
    },
  ],
  '6': [
    {
      id: 'bom-5',
      parentId: '6',
      childId: '2',
      childName: 'Nhôm 6061',
      childCode: 'NL-002',
      quantityPerUnit: 15,
      unit: 'kg',
    },
  ],
};

function BOMTreeItem({ item, level = 0 }: { item: BOMItem; level?: number }) {
  const [isExpanded, setIsExpanded] = useState(true);
  const hasChildren = item.children && item.children.length > 0;

  return (
    <div>
      <div
        className={`flex items-center gap-2 py-2 px-3 hover:bg-gray-50 rounded ${
          level > 0 ? 'ml-' + (level * 6) : ''
        }`}
        style={{ marginLeft: level * 24 }}
      >
        <div className="w-5 h-5 flex items-center justify-center">
          {hasChildren ? (
            <button onClick={() => setIsExpanded(!isExpanded)} className="hover:bg-gray-200 rounded p-0.5">
              {isExpanded ? (
                <ChevronDown className="w-4 h-4" />
              ) : (
                <ChevronRight className="w-4 h-4" />
              )}
            </button>
          ) : (
            <div className="w-2 h-2 bg-gray-300 rounded-full" />
          )}
        </div>
        <div className="flex-1 grid grid-cols-4 gap-4 items-center">
          <div className="text-sm text-gray-600">{item.childCode}</div>
          <div className="text-sm col-span-2">{item.childName}</div>
          <div className="text-sm">
            <span className="font-medium">{item.quantityPerUnit}</span>{' '}
            <span className="text-gray-500">{item.unit}</span>
          </div>
        </div>
      </div>
      {hasChildren && isExpanded && (
        <div>
          {item.children!.map((child) => (
            <BOMTreeItem key={child.id} item={child} level={level + 1} />
          ))}
        </div>
      )}
    </div>
  );
}

export function BOMView({ selectedMaterial }: BOMViewProps) {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [bomData, setBomData] = useState(mockBOMData);

  const currentBOM = selectedMaterial ? bomData[selectedMaterial.id] || [] : [];

  const handleAddBOM = (childId: string, quantity: number) => {
    if (!selectedMaterial) return;

    const newBOMItem: BOMItem = {
      id: `bom-${Date.now()}`,
      parentId: selectedMaterial.id,
      childId,
      childName: 'Vật tư mới',
      childCode: 'NEW-001',
      quantityPerUnit: quantity,
      unit: 'cái',
    };

    setBomData({
      ...bomData,
      [selectedMaterial.id]: [...currentBOM, newBOMItem],
    });
    setIsDialogOpen(false);
  };

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Package2 className="w-5 h-5 text-[#1e3a5f]" />
          <h3 className="text-lg">Định mức nguyên vật liệu (BOM)</h3>
        </div>
        {selectedMaterial && (
          <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
            <DialogTrigger asChild>
              <Button className="bg-[#1e3a5f] hover:bg-[#2d5080]">
                <Plus className="w-4 h-4 mr-2" />
                Thêm vật tư vào BOM
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-md">
              <DialogHeader>
                <DialogTitle>Thêm vật tư vào BOM</DialogTitle>
                <DialogDescription>
                  Định mức cho: {selectedMaterial.materialName}
                </DialogDescription>
              </DialogHeader>
              <div className="space-y-4 py-4">
                <div className="space-y-2">
                  <Label>Chọn vật tư con</Label>
                  <Select>
                    <SelectTrigger>
                      <SelectValue placeholder="Chọn vật tư" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="1">NL-001 - Thép tấm A36</SelectItem>
                      <SelectItem value="2">NL-002 - Nhôm 6061</SelectItem>
                      <SelectItem value="5">NL-003 - Ống thép phi 60</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label>Số lượng định mức</Label>
                  <Input type="number" placeholder="Nhập số lượng" />
                </div>
              </div>
              <div className="flex justify-end gap-2">
                <Button variant="outline" onClick={() => setIsDialogOpen(false)}>
                  Hủy
                </Button>
                <Button className="bg-[#1e3a5f] hover:bg-[#2d5080]" onClick={() => handleAddBOM('1', 10)}>
                  Thêm vào BOM
                </Button>
              </div>
            </DialogContent>
          </Dialog>
        )}
      </div>

      {!selectedMaterial ? (
        <div className="flex-1 flex items-center justify-center border rounded-lg bg-gray-50">
          <div className="text-center py-12">
            <Package2 className="w-12 h-12 text-gray-400 mx-auto mb-3" />
            <p className="text-gray-500">Chọn một vật tư để xem định mức BOM</p>
          </div>
        </div>
      ) : currentBOM.length === 0 ? (
        <div className="flex-1 flex items-center justify-center border rounded-lg bg-gray-50">
          <div className="text-center py-12">
            <Package2 className="w-12 h-12 text-gray-400 mx-auto mb-3" />
            <p className="text-gray-500 mb-2">Chưa có định mức BOM</p>
            <p className="text-sm text-gray-400">
              Nhấn "Thêm vật tư vào BOM" để thêm cấu trúc sản phẩm
            </p>
          </div>
        </div>
      ) : (
        <div className="border rounded-lg flex-1 overflow-auto">
          <div className="bg-gray-50 px-3 py-2 border-b">
            <div className="grid grid-cols-4 gap-4 text-sm font-medium text-gray-700">
              <div className="pl-7">Mã vật tư</div>
              <div className="col-span-2">Tên vật tư</div>
              <div>Số lượng định mức</div>
            </div>
          </div>
          <div className="p-2">
            {currentBOM.map((item) => (
              <BOMTreeItem key={item.id} item={item} />
            ))}
          </div>
        </div>
      )}

      {selectedMaterial && currentBOM.length > 0 && (
        <div className="mt-4 flex justify-end">
          <Button className="bg-blue-600 hover:bg-blue-700">
            Phát hành định mức
          </Button>
        </div>
      )}
    </div>
  );
}
