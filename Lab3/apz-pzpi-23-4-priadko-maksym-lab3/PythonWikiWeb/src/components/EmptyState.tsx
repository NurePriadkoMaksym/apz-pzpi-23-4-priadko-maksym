import React from "react";

export function EmptyState({ title, text }: { title: string; text?: string }) {
  return (
    <div className="empty-state">
      <div className="empty-mark">PY</div>
      <h3>{title}</h3>
      {text && <p>{text}</p>}
    </div>
  );
}
