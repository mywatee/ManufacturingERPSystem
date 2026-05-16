import re

with open('ManufacturingERP/Views/FinanceView.xaml', 'r', encoding='utf-8') as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if 'Style=' in line:
        count = line.count('Style=')
        if count > 1:
            print(f"ERROR: Multiple Style attributes on line {i+1}: {line.strip()}")
        
    # Check for unclosed Button tags or duplicate attributes across multiple lines
    # This is harder, but we can look for Button start and see all attributes until />
