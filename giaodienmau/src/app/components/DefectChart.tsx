import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from 'recharts';

const data = [
  { name: 'Lỗi kỹ thuật', value: 35 },
  { name: 'Kích thước sai lệch', value: 28 },
  { name: 'Bề mặt không đạt', value: 22 },
  { name: 'Nguyên liệu kém', value: 15 },
];

const COLORS = ['#ef4444', '#f97316', '#f59e0b', '#eab308'];

export function DefectChart() {
  return (
    <div className="bg-white border border-gray-200 rounded-lg p-6">
      <h3 className="mb-6">Phân tích lý do hàng lỗi</h3>
      <ResponsiveContainer width="100%" height={300}>
        <PieChart id="defect-chart">
          <Pie
            key="defect-pie"
            data={data}
            cx="50%"
            cy="50%"
            labelLine={false}
            label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
            outerRadius={80}
            fill="#8884d8"
            dataKey="value"
          >
            {data.map((entry, index) => (
              <Cell key={`cell-${entry.name}-${index}`} fill={COLORS[index % COLORS.length]} />
            ))}
          </Pie>
          <Tooltip
            key="defect-tooltip"
            contentStyle={{
              backgroundColor: 'white',
              border: '1px solid #e5e7eb',
              borderRadius: '8px',
              fontSize: '12px'
            }}
          />
          <Legend
            key="defect-legend"
            wrapperStyle={{ fontSize: '12px' }}
            iconType="circle"
          />
        </PieChart>
      </ResponsiveContainer>
    </div>
  );
}
