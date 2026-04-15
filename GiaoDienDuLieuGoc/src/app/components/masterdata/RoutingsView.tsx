import { useState } from 'react';
import { Plus, GripVertical, Clock, Settings2 } from 'lucide-react';
import { DndProvider, useDrag, useDrop } from 'react-dnd';
import { HTML5Backend } from 'react-dnd-html5-backend';
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
import { Material } from './MaterialsTable';

interface RoutingStep {
  id: string;
  productId: string;
  stepNumber: number;
  stepName: string;
  estimatedTime: number;
}

interface RoutingsViewProps {
  selectedMaterial: Material | null;
}

const mockRoutings: Record<string, RoutingStep[]> = {
  '4': [
    {
      id: 'r1',
      productId: '4',
      stepNumber: 1,
      stepName: 'Cắt thép theo bản vẽ',
      estimatedTime: 120,
    },
    {
      id: 'r2',
      productId: '4',
      stepNumber: 2,
      stepName: 'Hàn khung sườn',
      estimatedTime: 180,
    },
    {
      id: 'r3',
      productId: '4',
      stepNumber: 3,
      stepName: 'Lắp ráp bộ động cơ',
      estimatedTime: 90,
    },
    {
      id: 'r4',
      productId: '4',
      stepNumber: 4,
      stepName: 'Kiểm tra chất lượng',
      estimatedTime: 60,
    },
    {
      id: 'r5',
      productId: '4',
      stepNumber: 5,
      stepName: 'Sơn phủ hoàn thiện',
      estimatedTime: 150,
    },
  ],
  '3': [
    {
      id: 'r6',
      productId: '3',
      stepNumber: 1,
      stepName: 'Cắt thép theo kích thước',
      estimatedTime: 60,
    },
    {
      id: 'r7',
      productId: '3',
      stepNumber: 2,
      stepName: 'Hàn nối các khúc',
      estimatedTime: 120,
    },
    {
      id: 'r8',
      productId: '3',
      stepNumber: 3,
      stepName: 'Kiểm tra độ bền',
      estimatedTime: 30,
    },
  ],
};

const ItemType = 'ROUTING_STEP';

interface DraggableStepProps {
  step: RoutingStep;
  index: number;
  moveStep: (dragIndex: number, hoverIndex: number) => void;
}

function DraggableStep({ step, index, moveStep }: DraggableStepProps) {
  const [{ isDragging }, drag] = useDrag({
    type: ItemType,
    item: { index },
    collect: (monitor) => ({
      isDragging: monitor.isDragging(),
    }),
  });

  const [, drop] = useDrop({
    accept: ItemType,
    hover: (item: { index: number }) => {
      if (item.index !== index) {
        moveStep(item.index, index);
        item.index = index;
      }
    },
  });

  return (
    <div
      ref={(node) => drag(drop(node))}
      className={`flex items-center gap-3 p-4 bg-white border rounded-lg mb-2 cursor-move hover:shadow-md transition-shadow ${
        isDragging ? 'opacity-50' : 'opacity-100'
      }`}
    >
      <GripVertical className="w-5 h-5 text-gray-400" />
      <div className="w-8 h-8 rounded-full bg-[#1e3a5f] text-white flex items-center justify-center text-sm font-medium">
        {step.stepNumber}
      </div>
      <div className="flex-1">
        <div className="font-medium text-sm">{step.stepName}</div>
        <div className="text-xs text-gray-500 flex items-center gap-1 mt-1">
          <Clock className="w-3 h-3" />
          <span>{step.estimatedTime} phút</span>
        </div>
      </div>
    </div>
  );
}

function RoutingsContent({ selectedMaterial }: RoutingsViewProps) {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [routings, setRoutings] = useState(mockRoutings);
  const [formData, setFormData] = useState({
    stepName: '',
    estimatedTime: 0,
  });

  const currentRoutings = selectedMaterial ? routings[selectedMaterial.id] || [] : [];

  const moveStep = (dragIndex: number, hoverIndex: number) => {
    if (!selectedMaterial) return;

    const updatedSteps = [...currentRoutings];
    const [movedStep] = updatedSteps.splice(dragIndex, 1);
    updatedSteps.splice(hoverIndex, 0, movedStep);

    const reorderedSteps = updatedSteps.map((step, index) => ({
      ...step,
      stepNumber: index + 1,
    }));

    setRoutings({
      ...routings,
      [selectedMaterial.id]: reorderedSteps,
    });
  };

  const handleAddStep = () => {
    if (!selectedMaterial) return;

    const newStep: RoutingStep = {
      id: `r${Date.now()}`,
      productId: selectedMaterial.id,
      stepNumber: currentRoutings.length + 1,
      stepName: formData.stepName,
      estimatedTime: formData.estimatedTime,
    };

    setRoutings({
      ...routings,
      [selectedMaterial.id]: [...currentRoutings, newStep],
    });

    setIsDialogOpen(false);
    setFormData({ stepName: '', estimatedTime: 0 });
  };

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <Settings2 className="w-5 h-5 text-[#1e3a5f]" />
          <h3 className="text-lg">Quy trình công nghệ</h3>
        </div>
        {selectedMaterial && (
          <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
            <DialogTrigger asChild>
              <Button className="bg-[#1e3a5f] hover:bg-[#2d5080]">
                <Plus className="w-4 h-4 mr-2" />
                Thêm công đoạn
              </Button>
            </DialogTrigger>
            <DialogContent className="max-w-md">
              <DialogHeader>
                <DialogTitle>Thêm công đoạn mới</DialogTitle>
                <DialogDescription>
                  Quy trình cho: {selectedMaterial.materialName}
                </DialogDescription>
              </DialogHeader>
              <div className="space-y-4 py-4">
                <div className="space-y-2">
                  <Label htmlFor="stepName">Tên công đoạn</Label>
                  <Input
                    id="stepName"
                    value={formData.stepName}
                    onChange={(e) => setFormData({ ...formData, stepName: e.target.value })}
                    placeholder="VD: Cắt nguyên liệu"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="estimatedTime">Thời gian dự kiến (phút)</Label>
                  <Input
                    id="estimatedTime"
                    type="number"
                    value={formData.estimatedTime}
                    onChange={(e) => setFormData({ ...formData, estimatedTime: Number(e.target.value) })}
                    placeholder="60"
                  />
                </div>
              </div>
              <div className="flex justify-end gap-2">
                <Button variant="outline" onClick={() => setIsDialogOpen(false)}>
                  Hủy
                </Button>
                <Button className="bg-[#1e3a5f] hover:bg-[#2d5080]" onClick={handleAddStep}>
                  Thêm công đoạn
                </Button>
              </div>
            </DialogContent>
          </Dialog>
        )}
      </div>

      {!selectedMaterial ? (
        <div className="flex-1 flex items-center justify-center border rounded-lg bg-gray-50">
          <div className="text-center py-12">
            <Settings2 className="w-12 h-12 text-gray-400 mx-auto mb-3" />
            <p className="text-gray-500">Chọn một vật tư để xem quy trình công nghệ</p>
          </div>
        </div>
      ) : currentRoutings.length === 0 ? (
        <div className="flex-1 flex items-center justify-center border rounded-lg bg-gray-50">
          <div className="text-center py-12">
            <Settings2 className="w-12 h-12 text-gray-400 mx-auto mb-3" />
            <p className="text-gray-500 mb-2">Chưa có quy trình công nghệ</p>
            <p className="text-sm text-gray-400">
              Nhấn "Thêm công đoạn" để tạo quy trình sản xuất
            </p>
          </div>
        </div>
      ) : (
        <div className="flex-1 overflow-auto">
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-3 mb-3">
            <p className="text-sm text-blue-800">
              💡 Kéo thả các công đoạn để sắp xếp lại thứ tự thực hiện
            </p>
          </div>
          {currentRoutings.map((step, index) => (
            <DraggableStep key={step.id} step={step} index={index} moveStep={moveStep} />
          ))}
        </div>
      )}

      {selectedMaterial && currentRoutings.length > 0 && (
        <div className="mt-4 p-4 bg-gray-50 border rounded-lg">
          <div className="flex items-center justify-between">
            <div className="text-sm text-gray-600">
              Tổng thời gian dự kiến:{' '}
              <span className="font-medium text-gray-900">
                {currentRoutings.reduce((sum, step) => sum + step.estimatedTime, 0)} phút
              </span>
            </div>
            <Button className="bg-blue-600 hover:bg-blue-700">
              Lưu thay đổi
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}

export function RoutingsView(props: RoutingsViewProps) {
  return (
    <DndProvider backend={HTML5Backend}>
      <RoutingsContent {...props} />
    </DndProvider>
  );
}
