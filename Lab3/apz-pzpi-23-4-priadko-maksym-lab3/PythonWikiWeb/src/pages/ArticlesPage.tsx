import React, { FormEvent, useState } from "react";
import { ApiClient } from "../api";
import { ArticleGraphView } from "../components/ArticleGraphView";
import { EmptyState } from "../components/EmptyState";
import { StatGrid } from "../components/StatGrid";
import { Article, ArticleEdge, LocaleCode } from "../types";
import { buildArticleGraph } from "../utils/articleGraph";

interface ArticlesPageProps {
  api: ApiClient;
  articles: Article[];
  edges: ArticleEdge[];
  locale: LocaleCode;
  msg: (key: string) => string;
  onCompleted: () => Promise<void>;
  run: <T>(action: () => Promise<T>, success?: string) => Promise<T | undefined>;
  selected: Article | null;
  setSelected: (article: Article | null) => void;
}

export function ArticlesPage({ api, articles, edges, msg, onCompleted, run, selected, setSelected }: ArticlesPageProps) {
  const [keyword, setKeyword] = useState("");
  const [view, setView] = useState<"graph" | "list">("graph");
  const unlocked = articles.filter((article) => !article.isLocked).length;
  const graph = buildArticleGraph(articles, edges);

  async function search(event: FormEvent) {
    event.preventDefault();
    const article = await run(() => api.searchArticle(keyword));
    if (article) setSelected(article);
  }

  async function openArticle(article: Article) {
    setSelected((await run(() => api.article(article.id))) ?? article);
  }

  return (
    <div className="page-stack">
      <StatGrid
        items={[
          { label: msg("articles"), value: articles.length, tone: "blue" },
          { label: msg("available"), value: unlocked, tone: "green" },
          { label: msg("locked"), value: articles.length - unlocked, tone: "amber" }
        ]}
      />
      <div className="content-grid">
        <section className="panel">
          <div className="panel-header">
            <div>
              <p className="eyebrow">{msg("publicSearch")}</p>
              <h3>{msg("articles")}</h3>
            </div>
            <button className="secondary-button" onClick={() => setView(view === "graph" ? "list" : "graph")}>
              {view === "graph" ? msg("regularView") : msg("graphView")}
            </button>
          </div>
          <form className="search-bar" onSubmit={search}>
            <input value={keyword} onChange={(event) => setKeyword(event.target.value)} placeholder={msg("keyword")} dir="auto" />
            <button>{msg("search")}</button>
          </form>

          {view === "graph" ? (
            <ArticleGraphView
              articles={articles}
              graph={graph}
              selected={selected}
              openArticle={(article) => void openArticle(article)}
              label={msg("articles")}
            />
          ) : (
            <div className="article-list">
              {articles.map((article) => (
                <button
                  key={article.id}
                  className={`article-row ${selected?.id === article.id ? "selected" : ""}`}
                  onClick={() => void openArticle(article)}
                >
                  <span>{article.title}</span>
                  <small>
                    {article.isLocked ? msg("locked") : msg("available")} · {article.xpReward} {msg("xp")}
                  </small>
                </button>
              ))}
            </div>
          )}
        </section>
        <section className="reader-panel">
          {selected ? (
            <article>
              <div className="reader-kicker">
                <span>{selected.xpReward} XP</span>
                <span>
                  {selected.xpRequired} {msg("xpRequired")}
                </span>
              </div>
              <h3>{selected.title}</h3>
              <div className="reader-body" dir="auto">
                {selected.content}
              </div>
              <button
                onClick={async () => {
                  await run(() => api.completeArticle(selected.id));
                  await onCompleted();
                }}
                disabled={selected.isLocked}
              >
                {msg("complete")}
              </button>
            </article>
          ) : (
            <EmptyState title={msg("selectedArticle")} text={msg("search")} />
          )}
        </section>
      </div>
    </div>
  );
}
