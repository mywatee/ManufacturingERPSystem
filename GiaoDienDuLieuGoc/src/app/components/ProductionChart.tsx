import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';

const data = [
  { ngay: 'T2', keHoach: 240, thucTe: 220 },
  { ngay: 'T3', keHoach: 250, thucTe: 245 },
  { ngay: 'T4', keHoach: 260, thucTe: 250 },
  { ngay: 'T5', keHoach: 255, thucTe: 265 },
  { ngay: 'T6', keHoach: 270, thucTe: 260 },
  { ngay: 'T7', keHoach: 240, thucTe: 235 },
  { ngay: 'CN', keHoach: 200, thucTe: 190 },
];

export function ProductionChart() {
  return (
    <div className="bg-white border border-gray-200 rounded-lg p-6">
      <h3 className="mb-6">Tiến độ sản xuất so với Kế hoạch</h3>
      <ResponsiveContainer width="100%" height={300}>
        <LineChart data={data} id="production-chart">
          <CartesianGrid key="grid" strokeDasharray="3 3" stroke="#e5e7eb" />
          <XAxis
            key="xaxis"
            dataKey="ngay"
            tick={{ fontSize: 12, fill: '#6b7280' }}
            axisLine={{ stroke: '#d1d5db' }}
          />
          <YAxis
            key="yaxis"
            tick={{ fontSize: 12, fill: '#6b7280' }}
            axisLine={{ stroke: '#d1d5db' }}
          />
          <Tooltip
            key="tooltip"
            contentStyle={{
              backgroundColor: 'white',
              border: '1px solid #e5e7eb',
              borderRadius: '8px',
              fontSize: '12px'
            }}
          />
          <Legend
            key="legend"
            wrapperStyle={{ fontSize: '12px' }}
            iconType="circle"
          />
          <Line
            key="keHoach"
            type="monotone"
            dataKey="keHoach"
            stroke="#94a3b8"
            strokeWidth={2}
            name="Kế hoạch"
            dot={{ fill: '#94a3b8', r: 4 }}
          />
          <Line
            key="thucTe"
            type="monotone"
            dataKey="thucTe"
            stroke="#2563eb"
            strokeWidth={2}
            name="Thực tế"
            dot={{ fill: '#2563eb', r: 4 }}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
