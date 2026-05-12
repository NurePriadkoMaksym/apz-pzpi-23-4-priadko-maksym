import React from "react";
import { Article } from "../types";
import { RenderedArticleGraph, getEdgePath } from "../utils/articleGraph";

interface ArticleGraphViewProps {
  articles: Article[];
  graph: RenderedArticleGraph;
  selected: Article | null;
  openArticle: (article: Article) => void;
  label: string;
}

export function ArticleGraphView({ articles, graph, selected, openArticle, label }: ArticleGraphViewProps) {
  return (
    <div className="article-graph" aria-label={label}>
      <svg className="article-edges" viewBox="0 0 100 100" preserveAspectRatio="none" aria-hidden="true">
        <defs>
          <marker id="article-edge-arrow" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
            <path d="M 0 0 L 10 5 L 0 10 z" fill="context-stroke" />
          </marker>
        </defs>
        {graph.edges.map((edge, index) => {
          const from = graph.positions.get(edge.from);
          const to = graph.positions.get(edge.to);
          if (!from || !to) return null;
          return (
            <path
              key={`${edge.from}-${edge.to}-${index}`}
              d={getEdgePath(from, to, index, graph.edges)}
              className={selected?.id === edge.from || selected?.id === edge.to ? "active" : ""}
            />
          );
        })}
      </svg>
      {articles.map((article) => {
        const position = graph.positions.get(article.id) ?? { x: 50, y: 50 };
        return (
          <button
            key={article.id}
            className={`article-node ${article.isLocked ? "locked" : ""} ${selected?.id === article.id ? "selected" : ""}`}
            style={{ left: `${position.x}%`, top: `${position.y}%` }}
            title={article.title}
            onClick={() => openArticle(article)}
          >
            <span>{article.title}</span>
            <small>{article.xpReward} XP</small>
          </button>
        );
      })}
    </div>
  );
}
