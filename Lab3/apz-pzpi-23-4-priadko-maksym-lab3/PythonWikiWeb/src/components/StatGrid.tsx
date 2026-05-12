import React from "react";

export interface StatItem {
  label: string;
  value: string | number;
  tone?: "blue" | "green" | "amber" | "red";
}

export function StatGrid({ items }: { items: StatItem[] }) {
  return (
    <div className="stat-grid">
      {items.map((item) => (
        <article className={`stat-card ${item.tone ?? "blue"}`} key={item.label}>
          <span>{item.label}</span>
          <strong>{item.value}</strong>
        </article>
      ))}
    </div>
  );
}
