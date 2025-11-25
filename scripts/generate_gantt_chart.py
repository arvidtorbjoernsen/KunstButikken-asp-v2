import csv
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
import os

CSV_PATH = 'docs/Gantt.csv'
OUT_PATH = 'docs/Gantt.png'

# Read tasks from CSV
tasks = []
with open(CSV_PATH, newline='') as csvfile:
    reader = csv.DictReader(csvfile)
    for row in reader:
        try:
            start = int(row['StartWeek'])
            end = int(row['EndWeek'])
        except Exception:
            continue
        tasks.append({
            'task': row['Task'],
            'owner': row['Owner'],
            'start': start,
            'end': end,
            'notes': row.get('Notes', '')
        })

if not tasks:
    print('No tasks found in', CSV_PATH)
    raise SystemExit(1)

# Sort tasks for consistent display
# Keep original order so Baseline appears first (top)
# tasks = list(reversed(tasks))

weeks = list(range(39, 49))  # W39..W48 (ascending)

fig, ax = plt.subplots(figsize=(12, max(2, 0.5*len(tasks))))

y_pos = range(len(tasks))
labels = [f"{t['task']} ({t['owner']})" for t in tasks]

for i, t in enumerate(tasks):
    start = t['start']
    end = t['end']
    duration = end - start + 1
    ax.barh(i, duration, left=start, height=0.6, align='center', color='#4C78A8')
    ax.text(start + duration + 0.05, i, f"W{start}-W{end}", va='center', fontsize=9)

ax.set_yticks(list(y_pos))
ax.set_yticklabels(labels, fontsize=10)
ax.set_xticks(weeks)
ax.set_xticklabels([f'W{w}' for w in weeks])
ax.set_xlim(38.5, 48.5)
# Ensure x-axis is ascending left->right (no inversion)
# ax.invert_xaxis()
ax.invert_yaxis()  # show first task at the top
ax.grid(axis='x', linestyle='--', alpha=0.5)

ax.set_title('Gantt ‑ uke 39–48 (W39 → W48)')
plt.tight_layout()

os.makedirs(os.path.dirname(OUT_PATH), exist_ok=True)
plt.savefig(OUT_PATH, dpi=200)
print('Saved', OUT_PATH)
