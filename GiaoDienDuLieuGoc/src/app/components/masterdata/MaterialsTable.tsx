import { useState } from 'react';
import { Plus, Filter, AlertCircle, Package } from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '../ui/table';
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
import { Badge } from '../ui/badge';

export interface Material {
  id: string;
  materialCode: string;
  materialName: string;
  unit: string;
  category: 'Nguyên liệu' | 'Bán thành phẩm' | 'Thành phẩm';
  minStock: number;
  currentStock: number;
}

interface MaterialsTableProps {
  onMaterialSelect: (material: Material) => void;
  selectedMaterialId: string | null;
}

const mockMaterials: Material[] = [
  {
    id: '1',
    materialCode: 'NL-001',
    materialName: 'Thép tấm A36',
    unit: 'kg',
    category: 'Nguyên liệu',
    minStock: 1000,
    currentStock: 1500,
  },
  {
    id: '2',
    materialCode: 'NL-002',
    materialName: 'Nhôm 6061',
    unit: 'kg',
    category: 'Nguyên liệu',
    minStock: 500,
    currentStock: 300,
  },
  {
    id: '3',
    materialCode: 'BTP-001',
    materialName: 'Khung sườn máy',
    unit: 'cái',
    category: 'Bán thành phẩm',
    minStock: 50,
    currentStock: 75,
  },
  {
    id: '4',
    materialCode: 'TP-001',
    materialName: 'Máy ép thủy lực MH-200',
    unit: 'cái',
    category: 'Thành phẩm',
    minStock: 10,
    currentStock: 25,
  },
  {
    id: '5',
    materialCode: 'NL-003',
    materialName: 'Ống thép phi 60',
    unit: 'mét',
    category: 'Nguyên liệu',
    minStock: 200,
    currentStock: 150,
  },
  {
    id: '6',
    materialCode: 'BTP-002',
    materialName: 'Bộ động cơ điện',
    unit: 'bộ',
    category: 'Bán thành phẩm',
    minStock: 20,
    currentStock: 30,
  },
];

export function MaterialsTable({ onMaterialSelect, selectedMaterialId }: MaterialsTableProps) {
  const [materials, setMaterials] = useState<Material[]>(mockMaterials);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [filterCategory, setFilterCategory] = useState<string>('all');
  const [filterStock, setFilterStock] = useState<string>('all');

  const [formData, setFormData] = useState({
    materialCode: '',
    materialName: '',
    unit: '',
    category: 'Nguyên liệu' as Material['category'],
    minStock: 0,
    currentStock: 0,
  });

  const handleAddMaterial = () => {
    const newMaterial: Material = {
      id: Date.now().toString(),
      ...formData,
    };
    setMaterials([...materials, newMaterial]);
    setIsDialogOpen(false);
    setFormData({
      materialCode: '',
      materialName: '',
      unit: '',
      category: 'Nguyên liệu',
      minStock: 0,
      currentStock: 0,
    });
  };

  const filteredMaterials = materials.filter((material) => {
    const categoryMatch = filterCategory === 'all' || material.category === filterCategory;
    const stockMatch =
      filterStock === 'all' ||
      (filterStock === 'warning' && material.currentStock < material.minStock) ||
      (filterStock === 'normal' && material.currentStock >= material.minStock);
    return categoryMatch && stockMatch;
  });

  const getCategoryColor = (category: Material['category']) => {
    switch (category) {
      case 'Nguyên liệu':
        return 'bg-blue-100 text-blue-800';
      case 'Bán thành phẩm':
        return 'bg-purple-100 text-purple-800';
      case 'Thành phẩm':
        return 'bg-green-100 text-green-800';
    }
  };

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Package className="w-5 h-5 text-[#1e3a5f]" />
          <h3 className="text-lg">Danh mục vật tư</h3>
        </div>
        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogTrigger asChild>
            <Button className="bg-[#1e3a5f] hover:bg-[#2d5080]">
              <Plus className="w-4 h-4 mr-2" />
              Thêm vật tư mới
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-md">
            <DialogHeader>
              <DialogTitle>Thêm vật tư mới</DialogTitle>
              <DialogDescription>
                Nhập thông tin vật tư vào hệ thống
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4 py-4">
              <div className="space-y-2">
                <Label htmlFor="materialCode">Mã vật tư</Label>
                <Input
                  id="materialCode"
                  value={formData.materialCode}
                  onChange={(e) => setFormData({ ...formData, materialCode: e.target.value })}
                  placeholder="VD: NL-004"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="materialName">Tên vật tư</Label>
                <Input
                  id="materialName"
                  value={formData.materialName}
                  onChange={(e) => setFormData({ ...formData, materialName: e.target.value })}
                  placeholder="Nhập tên vật tư"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="unit">Đơn vị tính</Label>
                <Input
                  id="unit"
                  value={formData.unit}
                  onChange={(e) => setFormData({ ...formData, unit: e.target.value })}
                  placeholder="VD: kg, mét, cái"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="category">Phân loại</Label>
                <Select
                  value={formData.category}
                  onValueChange={(value) => setFormData({ ...formData, category: value as Material['category'] })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Nguyên liệu">Nguyên liệu</SelectItem>
                    <SelectItem value="Bán thành phẩm">Bán thành phẩm</SelectItem>
                    <SelectItem value="Thành phẩm">Thành phẩm</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="minStock">Tồn kho tối thiểu</Label>
                  <Input
                    id="minStock"
                    type="number"
                    value={formData.minStock}
                    onChange={(e) => setFormData({ ...formData, minStock: Number(e.target.value) })}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="currentStock">Tồn kho hiện tại</Label>
                  <Input
                    id="currentStock"
                    type="number"
                    value={formData.currentStock}
                    onChange={(e) => setFormData({ ...formData, currentStock: Number(e.target.value) })}
                  />
                </div>
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setIsDialogOpen(false)}>
                Hủy
              </Button>
              <Button className="bg-[#1e3a5f] hover:bg-[#2d5080]" onClick={handleAddMaterial}>
                Lưu vật tư
              </Button>
            </div>
          </DialogContent>
        </Dialog>
      </div>

      <div className="flex gap-2 mb-4">
        <div className="flex items-center gap-2">
          <Filter className="w-4 h-4 text-gray-500" />
          <Select value={filterCategory} onValueChange={setFilterCategory}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Phân loại" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tất cả phân loại</SelectItem>
              <SelectItem value="Nguyên liệu">Nguyên liệu</SelectItem>
              <SelectItem value="Bán thành phẩm">Bán thành phẩm</SelectItem>
              <SelectItem value="Thành phẩm">Thành phẩm</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <Select value={filterStock} onValueChange={setFilterStock}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Trạng thái tồn kho" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tất cả trạng thái</SelectItem>
            <SelectItem value="warning">Dưới ngưỡng</SelectItem>
            <SelectItem value="normal">Đủ tồn kho</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="border rounded-lg flex-1 overflow-auto">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Mã vật tư</TableHead>
              <TableHead>Tên vật tư</TableHead>
              <TableHead>Đơn vị</TableHead>
              <TableHead>Phân loại</TableHead>
              <TableHead>Tồn kho</TableHead>
              <TableHead>Trạng thái</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredMaterials.map((material) => {
              const isLowStock = material.currentStock < material.minStock;
              const isSelected = selectedMaterialId === material.id;

              return (
                <TableRow
                  key={material.id}
                  className={`cursor-pointer ${isSelected ? 'bg-blue-50' : 'hover:bg-gray-50'}`}
                  onClick={() => onMaterialSelect(material)}
                >
                  <TableCell>{material.materialCode}</TableCell>
                  <TableCell>{material.materialName}</TableCell>
                  <TableCell>{material.unit}</TableCell>
                  <TableCell>
                    <Badge className={getCategoryColor(material.category)}>
                      {material.category}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    {material.currentStock} / {material.minStock}
                  </TableCell>
                  <TableCell>
                    {isLowStock ? (
                      <div className="flex items-center gap-1 text-orange-600">
                        <AlertCircle className="w-4 h-4" />
                        <span className="text-sm">Cảnh báo</span>
                      </div>
                    ) : (
                      <Badge className="bg-green-100 text-green-800">Đủ</Badge>
                    )}
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
