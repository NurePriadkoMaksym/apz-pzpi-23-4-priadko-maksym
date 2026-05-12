import React, { MouseEvent, useMemo, useState } from "react";
import { Article, ArticleEdge } from "../types";
import { GraphPoint } from "../utils/articleGraph";

const NEW_ARTICLE_NODE_ID = -1;
const NEW_ARTICLE_POSITION = { x: 50, y: 50 };

interface ConnectionBuilderModalProps {
  articles: Article[];
  editedArticleId: number | null;
  edges: ArticleEdge[];
  title: string;
  msg: (key: string) => string;
  onClose: () => void;
  onConnect: (from: number, to: number) => Promise<void> | void;
}

export function ConnectionBuilderModal({
  articles,
  editedArticleId,
  edges,
  title,
  msg,
  onClose,
  onConnect
}: ConnectionBuilderModalProps) {
  const [dragFrom, setDragFrom] = useState<number | null>(null);
  const [pointer, setPointer] = useState<GraphPoint | null>(null);
  const nodePositions = useMemo(() => getNodePositions(articles), [articles]);

  function getBuilderPosition(articleId: number) {
    return articleId === NEW_ARTICLE_NODE_ID ? NEW_ARTICLE_POSITION : nodePositions.get(articleId);
  }

  function updatePointer(event: MouseEvent<HTMLDivElement>) {
    if (dragFrom === null) return;
    const rect = event.currentTarget.getBoundingClientRect();
    setPointer({
      x: ((event.clientX - rect.left) / rect.width) * 100,
      y: ((event.clientY - rect.top) / rect.height) * 100
    });
  }

  async function connectTo(targetId: number) {
    if (dragFrom === null || dragFrom === targetId) {
      setDragFrom(null);
      setPointer(null);
      return;
    }

    const from = dragFrom;
    setDragFrom(null);
    setPointer(null);
    await onConnect(from, targetId);
  }

  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true">
      <section className="connection-modal">
        <div className="panel-header">
          <div>
            <p className="eyebrow">{msg("editorWorkspace")}</p>
            <h3>{msg("visualBuilder")}</h3>
          </div>
          <button className="secondary-button" onClick={onClose}>
            {msg("close")}
          </button>
        </div>
        <p className="helper-text">{editedArticleId ? msg("visualBuilderHint") : msg("visualBuilderCreateHint")}</p>
        <div
          className="connection-canvas"
          onMouseMove={updatePointer}
          onMouseUp={() => {
            setDragFrom(null);
            setPointer(null);
          }}
          onMouseLeave={() => {
            setDragFrom(null);
            setPointer(null);
          }}
        >
          <svg className="connection-lines" viewBox="0 0 100 100" preserveAspectRatio="none">
            {edges.map((edge) => {
              const from = getBuilderPosition(edge.from);
              const to = getBuilderPosition(edge.to);
              if (!from || !to) return null;
              return <line key={`${edge.from}-${edge.to}`} x1={from.x} y1={from.y} x2={to.x} y2={to.y} />;
            })}
            {dragFrom !== null && pointer && getBuilderPosition(dragFrom) && (
              <line
                className="preview"
                x1={getBuilderPosition(dragFrom)!.x}
                y1={getBuilderPosition(dragFrom)!.y}
                x2={pointer.x}
                y2={pointer.y}
              />
            )}
          </svg>
          {!editedArticleId && (
            <button
              type="button"
              className={`builder-node new-article ${dragFrom === NEW_ARTICLE_NODE_ID ? "dragging" : ""}`}
              style={{ left: `${NEW_ARTICLE_POSITION.x}%`, top: `${NEW_ARTICLE_POSITION.y}%` }}
              onMouseDown={(event) => {
                event.preventDefault();
                setDragFrom(NEW_ARTICLE_NODE_ID);
                setPointer(NEW_ARTICLE_POSITION);
              }}
            >
              <span>{title || msg("newArticle")}</span>
              <small>{msg("create")}</small>
            </button>
          )}
          {articles.map((article) => {
            const position = nodePositions.get(article.id) ?? { x: 50, y: 50 };
            return (
              <button
                key={article.id}
                type="button"
                className={`builder-node ${dragFrom === article.id ? "dragging" : ""}`}
                style={{ left: `${position.x}%`, top: `${position.y}%` }}
                onMouseDown={(event) => {
                  event.preventDefault();
                  if (!editedArticleId) return;
                  setDragFrom(article.id);
                  setPointer(position);
                }}
                onMouseUp={(event) => {
                  event.stopPropagation();
                  void connectTo(article.id);
                }}
              >
                <span>{article.title}</span>
                <small>#{article.id}</small>
              </button>
            );
          })}
        </div>
      </section>
    </div>
  );
}

function getNodePositions(articles: Article[]) {
  const positions = new Map<number, GraphPoint>();
  const radiusX = 36;
  const radiusY = 32;

  articles.forEach((article, index) => {
    const angle = articles.length <= 1 ? 0 : (Math.PI * 2 * index) / articles.length - Math.PI / 2;
    positions.set(article.id, {
      x: 50 + Math.cos(angle) * radiusX,
      y: 50 + Math.sin(angle) * radiusY
    });
  });

  return positions;
}

export { NEW_ARTICLE_NODE_ID };
